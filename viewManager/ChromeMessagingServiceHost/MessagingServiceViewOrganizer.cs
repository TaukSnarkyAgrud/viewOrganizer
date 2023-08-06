using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;
using ChromeMessagingServiceHost.Exceptions;
using ChromeTools.Exceptions;
using Newtonsoft.Json;
using Serilog;

namespace ChromeMessagingServiceHost
{
    public class MessagingServiceViewOrganizer : BackgroundService
    {
        private readonly Serilog.ILogger _logger;
        private NamedPipeServerStream? fromViewOrganizerPipeServer;
        private NamedPipeClientStream? toViewOrganizerPipeClient;
        private CancellationTokenSource stoppingToken;
        private IHostApplicationLifetime _appLifetime;

        private readonly int messgengerReadyToRecieveChromeDelay = 5000; // 10 seconds

        private readonly int heartbeatTimeout = 80000; // 80 seconds
        private readonly int heartbeatInterval = 15000; // 15 seconds
        private bool keepAlive = false;
        private int BetweenMessageDelay = 3000;

        // Define the pipe names for communication with View Organizer and Chrome extension
        private const string viewOrganizerPipeNameFrom = "fromViewOrganizerPipe";
        private const string viewOrganizerPipeNameTo = "toViewOrganizerPipe";

        // Create buffers to store messages from View Organizer and Chrome extension
        private static readonly ConcurrentQueue<string> toChromeExtenstionBuffer = new();
        private static readonly ConcurrentQueue<string> toViewOrgainzerBuffer = new();

        // Lock objects for synchronization
        private static readonly object sendToChromeExtensionLock = new();
        private static readonly object sendToViewOrganizerLock = new();
        private static readonly object toChromeExtensionBufferLock = new();
        private static readonly object toViewOrganizerBufferLock = new();

        private Task? viewOrganizerListener;
        private Task? chromeExtensionListener;
        private Task? viewOrganizerProcessor;
        private Task? chromeExtensionProcessor;
        private Task? hearbeatService;

        public MessagingServiceViewOrganizer(Serilog.ILogger logger, IHostApplicationLifetime appLifetime)
        {
            this.stoppingToken = new();
            _logger = logger;
            _appLifetime = appLifetime;
            logLock(sendToChromeExtensionLock, nameof(sendToChromeExtensionLock));
            logLock(sendToViewOrganizerLock, nameof(sendToViewOrganizerLock));
            logLock(toChromeExtensionBufferLock, nameof(toChromeExtensionBufferLock));
            logLock(toViewOrganizerBufferLock, nameof(toViewOrganizerBufferLock));
        }

        private void logLock(object theLock, string name)
        {
            _logger.Information($"Lock: {name} => {theLock.GetHashCode()}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Listen for the cancellation event and perform cleanup tasks
            _appLifetime.ApplicationStopping.Register(OnApplicationStopping);
            this.stoppingToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            _logger.Information("MessagingServiceViewOrganizer running.");
            // Start listening for messages from View Organizer and Chrome extension in separate threads
            //viewOrganizerListener = Task.Run(async () => { await ListenForMessagesFromViewOrganizer(toChromeExtenstionBuffer); });
            chromeExtensionListener = Task.Run(async () => { await ListenForMessages(toViewOrgainzerBuffer); });

            //Task logAllStdIn = logAllStdin();

            // Start processing messages in separate threads
            _logger.Information("Starting Message Processors.");
            viewOrganizerProcessor = ProcessMessages("chrome", toChromeExtenstionBuffer, SendToChromeExtension, toChromeExtensionBufferLock, sendToChromeExtensionLock);
            chromeExtensionProcessor = ProcessMessages("viewOrganizer", toViewOrgainzerBuffer, SendToViewOrganizer, toViewOrganizerBufferLock, sendToViewOrganizerLock );

            // Tell Chrome ready to receive
            var sendReady = sendOneReadyToChromeAsync();

            // Start Heartbeat service
            this.hearbeatService = HeartbeatService(sendReady);

            //// Wait for both listeners and processors to complete (optional)
            await Task.WhenAll( chromeExtensionListener, viewOrganizerProcessor, chromeExtensionProcessor);
        }

        private async Task logAllStdin()
        {
            _logger.Information("Native Messaging Host is logging ALL from Chrome NMAPI...");

            // Read messages from stdin
            using StreamReader reader = new(Console.OpenStandardInput(), Encoding.UTF8);
            _logger.Information("Chrome StreamReader Ready to receive...");

            while (true)
            {
                // Read a block of text from the stream
                var buffer = new char[1024];
                var bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);

                // If the stream is empty, log "empty" and break out of the loop
                if (bytesRead == 0)
                {
                    _logger.Information("empty");
                    break;
                }

                // Log the block of text
                var text = new string(buffer, 0, bytesRead);
                _logger.Information(text);

                // Wait for 1 second before reading the next block
                await Task.Delay(1000);
            }
        }

        private async Task HeartbeatService(Task chromeReady)
        {
            _logger.Information("Starting heartbeat service");
            while (true)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    _logger.Information("StoppingToken invoked(canceled)");
                    HaltApplication();
                }
                await Task.WhenAny(chromeReady);

                await Task.WhenAny(Task.Delay(TimeSpan.FromMilliseconds(heartbeatTimeout)), SendHeartBeat());
                if (keepAlive)
                {
                    _logger.Information("Restarting Pulse timeout; There must still be an active connection with chrome.");
                    keepAlive = false;
                    await Task.Delay(heartbeatInterval);
                }
                else
                {
                    _logger.Fatal("Heartbeat timeout reached waiting for return");
                    HaltApplication();
                    break;
                }
            }
        }

        private async Task SendHeartBeat()
        {
            _logger.Information("Sending heartbeat");
            bool lockAcquired = Monitor.TryEnter(toChromeExtensionBufferLock, TimeSpan.FromSeconds(5));
            if (!lockAcquired)
            {
                _logger.Error("Lock acquisition timed out for lock {theErrorLock}", toChromeExtensionBufferLock.GetHashCode());
                throw new TimeoutException($"Lock acquisition timed out for lock {toChromeExtensionBufferLock.GetHashCode()}");
            }
            try
            {
                toChromeExtenstionBuffer.Enqueue("{\"type\":\"heartbeat\"}");
            }
            finally
            {
                Monitor.Exit(toChromeExtensionBufferLock);
            }

            _logger.Information("Waiting for heartbeat return");
            await CheckForHeartbeatResponse();
            _logger.Information("KeepAlive has been observed to be reset to true");
        }

        private async Task CheckForHeartbeatResponse()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                if (keepAlive)
                {
                    break;
                }
            }
            return;
        }

        private async Task sendOneReadyToChromeAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(messgengerReadyToRecieveChromeDelay));
            _logger.Information("Sending Ready Message.");

            bool lockAcquired = Monitor.TryEnter(toChromeExtensionBufferLock, TimeSpan.FromSeconds(5));
            if (!lockAcquired)
            {
                _logger.Error("Lock acquisition timed out for lock {theErrorLock}", toChromeExtensionBufferLock.GetHashCode());
                throw new TimeoutException($"Lock acquisition timed out for lock {toChromeExtensionBufferLock.GetHashCode()}");
            }
            try
            {
                string readyMessage = "{\"type\":\"ready\"}";
                toChromeExtenstionBuffer.Enqueue(readyMessage);
            }
            finally
            {
                Monitor.Exit(toChromeExtensionBufferLock);
            }
            await Task.Delay(TimeSpan.FromMilliseconds(messgengerReadyToRecieveChromeDelay));
        }

        private async Task sendTestStackToChrome()
        {
            _logger.Information("Sending Test Stack");

            bool lockAcquired = Monitor.TryEnter(toChromeExtensionBufferLock, TimeSpan.FromSeconds(5));
            if (!lockAcquired)
            {
                _logger.Error("Lock acquisition timed out for lock {theErrorLock}", toChromeExtensionBufferLock.GetHashCode());
                throw new TimeoutException($"Lock acquisition timed out for lock {toChromeExtensionBufferLock.GetHashCode()}");
            }
            try
            {
                string aMess = "{\"type\":\"chicken\"}";
                toChromeExtenstionBuffer.Enqueue(aMess);
                aMess = "{\"type\":\"spannered\"}";
                toChromeExtenstionBuffer.Enqueue(aMess);
                aMess = "{\"type\":\"pilton\"}";
                toChromeExtenstionBuffer.Enqueue(aMess);
            }
            finally
            {
                Monitor.Exit(toChromeExtensionBufferLock);
            }
        }

        private void OnApplicationStopping()
        {
            //viewOrganizerListener?.Dispose();
            //chromeExtensionListener?.Dispose();
            //viewOrganizerProcessor?.Dispose();
            //chromeExtensionProcessor?.Dispose();
            //hearbeatService?.Dispose();
        }

        private void HaltApplication()
        {
            _logger.Information("Halt Application Initiated.");
            _appLifetime.StopApplication();
        }

        protected void StopAsync()
        {
            _logger.Information("Cancellation Requested. Stopping Worker Service.");
            base.StopAsync(this.stoppingToken.Token);
            stoppingToken.Cancel();
        }

        private async Task ListenForMessages(ConcurrentQueue<string> buffer)
        {
            _logger.Information("Native Messaging Host is waiting for messages from Chrome NMAPI...");

            // Read messages from stdin
            using StreamReader reader = new(Console.OpenStandardInput(), Encoding.UTF8);
            _logger.Information("Chrome StreamReader Ready to receive...");

            var headerBuffer = new char[4]; // 4-byte header for message length
            var messageLength = 0;

            while (true)
            {
                // Read the message length header
                var numberOfBytesRead = await reader.ReadAsync(headerBuffer, 0, headerBuffer.Length);

                // Check if the header is read correctly
                if (!BytesAreValidMessageHeader(numberOfBytesRead, headerBuffer, out messageLength))
                {
                    _logger.Error("Error reading message length header. Flushing the stream.");
                    reader.DiscardBufferedData();
                    continue;
                }

                // Validate the message length (adjust as per your message format)
                if (messageLength <= 0 || messageLength > 1000000)
                {
                    _logger.Error($"Invalid message length: {messageLength}. Flushing the stream.");
                    reader.DiscardBufferedData();
                    continue;
                }
                else
                {
                    _logger.Information($"Message length: {messageLength}.");
                }

                _logger.Information("Reading Message...");
                // Read the entire message based on the message length
                var messageBuffer = new char[messageLength];
                numberOfBytesRead = await reader.ReadAsync(messageBuffer, 0, messageLength);

                // Check if the message is read correctly
                if (numberOfBytesRead < messageLength)
                {
                    _logger.Error("Error reading the entire message. Flushing the stream.");
                    reader.DiscardBufferedData();
                    continue;
                }

                // Enqueue the message
                var message = new string(messageBuffer);

                // Acquire a lock before enqueuing the message to the buffer
                bool lockAcquired = Monitor.TryEnter(toViewOrganizerBufferLock, TimeSpan.FromSeconds(5));
                if (!lockAcquired)
                {
                    _logger.Error("Lock acquisition timed out for lock {theErrorLock}", toViewOrganizerBufferLock.GetHashCode());
                    throw new TimeoutException($"Lock acquisition timed out for lock {toViewOrganizerBufferLock.GetHashCode()}");
                }
                try
                {
                    _logger.Information("Message observed from chrome.");
                    _logger.Information(message);
                    //var incoming = JsonConvert.DeserializeObject<GenericChromeMessage>(message);
                    //_logger.Information(incoming.Action + " " + incoming.Data);
                    buffer.Enqueue(message);
                    _logger.Information("Message Enqueued to ViewOrganizer");
                }
                finally
                {
                    Monitor.Exit(toViewOrganizerBufferLock);
                }
            }
        }

        private bool BytesAreValidMessageHeader(int numberOfBytesRead, char[] bytesRead, out int messageLength)
        {
            messageLength = 0;
            if (numberOfBytesRead != 4)
            {
                return false;
            }
            try
            {
                messageLength = BitConverter.ToInt32(Encoding.UTF8.GetBytes(bytesRead), 0);
            }
            catch (Exception)
            {
                _logger.Error("Unable to convert byte array to message length");
                return false;
            }
            return true;
        }

        private async Task ListenForMessagesFromViewOrganizer(ConcurrentQueue<string> buffer)
        {
            while (true)
            {
                fromViewOrganizerPipeServer ??= new(
                    viewOrganizerPipeNameFrom,
                    PipeDirection.In);
                _logger.Information("Native Messaging Host is waiting for messages from View Organizer...");

                try
                {
                    using StreamReader reader = new(fromViewOrganizerPipeServer, Encoding.UTF8);
                    _logger.Information("NMH StreamReader Ready to receive...");

                    if (fromViewOrganizerPipeServer.IsConnected)
                    {
                        _logger.Information("Client still connected, when expected to not be connected.");
                    }
                    _logger.Information("Client not connected, listener waiting(infinitly) to connect...");
                    await fromViewOrganizerPipeServer.WaitForConnectionAsync();
                    _logger.Information("Connection (for receive) initiation observed from View Organizer...");

                    // Read messages from the pipe and add them to the buffer
                    _logger.Information("Attempting to read message from pipe...");
                    string? message = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(message))
                    {
                        _logger.Information("This Line was empty signifying the end of a message on the pipe or an empty send(an error)");
                        fromViewOrganizerPipeServer?.Dispose();
                        fromViewOrganizerPipeServer = null;
                        break;
                    }

                    // Acquire a lock before enqueuing the message to the buffer
                    bool lockAcquired = Monitor.TryEnter(toChromeExtensionBufferLock, TimeSpan.FromSeconds(5));
                    if (!lockAcquired)
                    {
                        _logger.Error("Lock acquisition timed out for lock {theErrorLock}", toChromeExtensionBufferLock.GetHashCode());
                        throw new TimeoutException($"Lock acquisition timed out for lock {toChromeExtensionBufferLock.GetHashCode()}");
                    }
                    try
                    {
                        _logger.Information("Message observed and enqued from ViewOrganizer.");
                        _logger.Information(message);
                        buffer.Enqueue(message);
                        fromViewOrganizerPipeServer?.Dispose();
                        fromViewOrganizerPipeServer = null;
                        break;
                    }
                    finally
                    {
                        Monitor.Exit(toChromeExtensionBufferLock);
                    }
                }
                catch (ObjectDisposedException odEx)
                {
                    _logger.Fatal("PipeServer was disposed early.");
                    _logger.Fatal(odEx.ToString());
                    StopAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.ToString());
                }
            }
        }

        private async Task ProcessMessages(string bufferName, ConcurrentQueue<string> buffer, Func<string, Task> sendMessage, object bufferLock, object processLock)
        {
            while (true)
            {
                while (!buffer.IsEmpty)
                {
                    _logger.Information("Buffer not empty");
                    string? message = null;

                    // Dequeue the message from the buffer
                    bool lockAcquired = Monitor.TryEnter(bufferLock, TimeSpan.FromSeconds(5));
                    if (!lockAcquired)
                    {
                        _logger.Error("Lock acquisition timed out for lock {theErrorLock}", bufferLock.GetHashCode());
                        throw new TimeoutException($"Lock acquisition timed out for lock {bufferLock.GetHashCode()}");
                    }
                    try
                    {
                        if (buffer.TryDequeue(out message))
                        {
                            if (bufferName == "viewOrganizer")
                            {
                                var messageDeseriJson = JsonConvert.DeserializeObject<HeartbeatCheck>(message);
                                if (messageDeseriJson.Action == "heartbeat")
                                {
                                    _logger.Information("Heartbeat KeepAlive noticed in transit");
                                    keepAlive = true;
                                    break;
                                }

                                _logger.Information("NON-Heartbeat KeepAlive triggered");
                                keepAlive = true;
                            }
                        }
                        else
                        {
                            _logger.Error("Try Dequeue failed to pull from the buffer.");
                            throw new BufferTryDequeueException("Try Dequeue failed to pull from the buffer.");
                        }
                    }
                    finally
                    {
                        Monitor.Exit(bufferLock);
                    }

                    if (message != null)
                    {
                        // Send the processed message to the respective recipient asynchronously
                        await SendWithLock(sendMessage, message, processLock);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(BetweenMessageDelay));
                }


                // Optional: Add a delay or awaitable task to prevent busy-waiting
                await Task.Delay(100);
            }
            _logger.Information("Processors stopped due to Cancelation");
        }

        private async Task SendWithLock(Func<string, Task> sendMessage, string message, object lockObject)
        {
            bool lockAcquired = Monitor.TryEnter(lockObject, TimeSpan.FromSeconds(5));
            if (!lockAcquired)
            {
                _logger.Error("Lock acquisition timed out for lock {theErrorLock}", lockObject.GetHashCode());
                throw new TimeoutException($"Lock acquisition timed out for lock {lockObject.GetHashCode()}");
            }

            try
            {
                await sendMessage(message);
            }
            finally
            {
                Monitor.Exit(lockObject);
            }
        }

        private async Task SendToChromeExtension(string message)
        {
            _logger.Information("Message to be sent to stdout(chrome ):");
            _logger.Information("       " + message);

            _logger.Information("Converting message to JSON...");
            var convertedMessage = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message + "\n");
            byte[] messageLengthBytes = BitConverter.GetBytes(convertedMessage.Length);
            _logger.Information("       " + Encoding.UTF8.GetString(messageLengthBytes) + Encoding.UTF8.GetString(convertedMessage));

            // Send the message to Chrome extension via stdout
            _logger.Information("Message sending to stdout(chrome )...");
            Console.OpenStandardOutput().Write(messageLengthBytes);
            Console.OpenStandardOutput().Write(convertedMessage);
            //Console.Write("\n");
            await Console.Out.FlushAsync();
            _logger.Information("Message sent to stdout(chrome )...");
        }

        private async Task SendToViewOrganizer(string message)
        {
            _logger.Information("Message to be sent to ViewOrganizer.");
            _logger.Information(message);

            // Prepare the message to send to Native Messaging Host
            string messageToSend = message;

            // Write the message to the pipe
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);

            toViewOrganizerPipeClient = new NamedPipeClientStream(".", viewOrganizerPipeNameTo, PipeDirection.Out);

            if (!toViewOrganizerPipeClient.IsConnected)
            {
                _logger.Information("Opening connection with ViewOrgaizer for sending messages.");
                await toViewOrganizerPipeClient.ConnectAsync();
            }

            if (toViewOrganizerPipeClient.IsConnected)
            {
                await toViewOrganizerPipeClient.WriteAsync(messageBytes, 0, messageBytes.Length);
                await toViewOrganizerPipeClient.FlushAsync();
                toViewOrganizerPipeClient.WaitForPipeDrain();
                _logger.Information("Message sent to ViewOrganizer."); 
                toViewOrganizerPipeClient?.Dispose();
                toViewOrganizerPipeClient = null;
            }
            else
            {
                throw new ConnectionTerminatedEarlyException("The Connection was terminated before the message could be sent.");
            }
        }
    }
}