using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;
using ChromeMessagingServiceHost.Exceptions;
using ChromeMessagingServiceHost.Types;
using ChromeTools;
using ChromeTools.Exceptions;
using Newtonsoft.Json;

namespace ChromeMessagingServiceHost
{
    public class MessagingServiceViewOrganizer : BackgroundService
    {
        private readonly Serilog.ILogger _logger;
        private NamedPipeServerStream? fromViewOrganizerPipeServer;
        private NamedPipeClientStream? toViewOrganizerPipeClient;
        private CancellationTokenSource stoppingToken;
        private readonly IHostApplicationLifetime _appLifetime;

        private readonly int messgengerReadyToRecieveChromeDelay = 2000; // 10 seconds

        private readonly int heartbeatTimeout = 80000; // 80 seconds
        private readonly int heartbeatInterval = 15000; // 15 seconds
        private bool keepAlive = false;
        private readonly int BetweenMessageDelay = 1500;
        private readonly double sendTimeout = 5000;
        private readonly int messageTTL = 3000;

        // Define the pipe names for communication with View Organizer and Chrome extension
        private const string viewOrganizerPipeNameFrom = "fromViewOrganizerPipe";
        private const string viewOrganizerPipeNameTo = "toViewOrganizerPipe";

        // Create buffers to store messages from View Organizer and Chrome extension
        private static readonly ConcurrentQueue<MessageObject> toChromeExtenstionBuffer = new();
        private static readonly ConcurrentQueue<MessageObject> toViewOrgainzerBuffer = new();

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
            LogLock(sendToChromeExtensionLock, nameof(sendToChromeExtensionLock));
            LogLock(sendToViewOrganizerLock, nameof(sendToViewOrganizerLock));
            LogLock(toChromeExtensionBufferLock, nameof(toChromeExtensionBufferLock));
            LogLock(toViewOrganizerBufferLock, nameof(toViewOrganizerBufferLock));
        }

        private void LogLock(object theLock, string name)
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
            //viewOrganizerListener = Task.Run(async () => { await ListenForMessagesFromViewOrganizer(toChromeExtenstionBuffer); }, stoppingToken);
            chromeExtensionListener = Task.Run(async () => { await ListenForMessages(); }, stoppingToken);
            //_ = ReadFromStdinAndWriteToFileAsync("C:\\Program Files\\ChromeNativeMessagingHost\\Logging\\ChromeMessagingHost_stdin.log");

            //Task logAllStdIn = logAllStdin();

            // Start processing messages in separate threads
            _logger.Information("Starting Message Processors.");
            viewOrganizerProcessor = ProcessMessages("chrome", toChromeExtenstionBuffer, SendToChromeExtension, toChromeExtensionBufferLock, sendToChromeExtensionLock);
            chromeExtensionProcessor = Task.Run(async () => {
                await ProcessMessages("viewOrganizer", toViewOrgainzerBuffer, SendToViewOrganizer, toViewOrganizerBufferLock, sendToViewOrganizerLock );
            }, stoppingToken);

            // Tell Chrome ready to receive
            var sendReady = SendOneReadyToChromeAsync();

            // Start Heartbeat service
            this.hearbeatService = HeartbeatService(sendReady);

            //// Wait for both listeners and processors to complete (optional)
            await Task.WhenAll( chromeExtensionListener, viewOrganizerProcessor, chromeExtensionProcessor);
            HaltApplication();
        }

        private async Task HeartbeatService(Task chromeReady)
        {
            _logger.Information("Starting heartbeat service");
            while (!stoppingToken.IsCancellationRequested)
            {
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
                    stoppingToken.Cancel();
                    break;
                }
            }
            _logger.Information("StoppingToken invoked(canceled)");
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
                MessageObject newMessage = new("{\"type\":\"heartbeat\"}");
                toChromeExtenstionBuffer.Enqueue(newMessage);
            }
            finally
            {
                Monitor.Exit(toChromeExtensionBufferLock);
            }

            _logger.Information("Waiting for heartbeat return");
            await CheckForHeartbeatResponse();
            if(stoppingToken.IsCancellationRequested)
            {
                return;
            }
            _logger.Information("KeepAlive has been observed to be reset to true");
        }

        private async Task CheckForHeartbeatResponse()
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                if (keepAlive)
                {
                    break;
                }
            }
            return;
        }

        private async Task SendOneReadyToChromeAsync()
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
                MessageObject newMessage = new(readyMessage);
                toChromeExtenstionBuffer.Enqueue(newMessage);
            }
            finally
            {
                Monitor.Exit(toChromeExtensionBufferLock);
            }
            await Task.Delay(TimeSpan.FromMilliseconds(messgengerReadyToRecieveChromeDelay));
        }

        private void OnApplicationStopping()
        {
            viewOrganizerListener?.Dispose();
            chromeExtensionListener?.Dispose();
            viewOrganizerProcessor?.Dispose();
            chromeExtensionProcessor?.Dispose();
            hearbeatService?.Dispose();
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

        private async Task ListenForMessages()
        {
            _logger.Information("Native Messaging Host is waiting for messages from Chrome NMAPI...");

            // Read messages from stdin
            using StreamReader reader = new(Console.OpenStandardInput(), Encoding.UTF8);
            _logger.Information("Chrome StreamReader Ready to receive...");

            byte[] headerBuffer = new byte[4];

            var messageLength = 0;
            _ = CleanupReader(reader);
            while (!stoppingToken.IsCancellationRequested)
            {
                // Generate a unique ID to group log messages in an aggregate log file for readability
                string guid = GenerateContrastGuid();

                // Read the message length header
                var numberOfHeaderBytesRead = reader.BaseStream.Read(headerBuffer, 0, headerBuffer.Length);

                // Check if the header is read correctly
                if (!BytesAreValidMessageHeader(numberOfHeaderBytesRead, headerBuffer, out messageLength))
                {
                    _logger.Error($"{guid}Error reading message length header. Flushing the stream.");
                    reader.DiscardBufferedData();
                    await Task.Delay(TimeSpan.FromMilliseconds(BetweenMessageDelay));
                    continue;
                }

                // Validate the message length (adjust as per your message format)
                if (messageLength <= 0 || messageLength > 1000000)
                {
                    _logger.Error($"{guid}Invalid message length: {messageLength}. Flushing the stream.");
                    reader.DiscardBufferedData();
                    continue;
                }
                else
                {
                    _logger.Information($"{guid}Message length: {messageLength}.");
                }

                _logger.Information($"{guid}Reading Message...");
                // Read the entire message based on the message length
                var messageBuffer = new char[messageLength];
                var numberOfBytesRead = await reader.ReadAsync(messageBuffer, 0, messageLength);

                // Check if the message is read correctly
                if (numberOfBytesRead < messageLength)
                {
                    _logger.Error($"{guid}Error reading the entire message. Flushing the stream.");
                    reader.DiscardBufferedData();
                    continue;
                }

                await IngestMessage(messageBuffer.ToArray<char>(), guid);

                await Task.Delay(TimeSpan.FromMilliseconds(BetweenMessageDelay));
            }
        }

        private static string GenerateContrastGuid()
        {
            Guid uniqueId = Guid.NewGuid();

            // You can convert the Guid to a short string if needed
            string shortUniqueId = Convert.ToBase64String(uniqueId.ToByteArray());
            return string.Concat("[", shortUniqueId.Replace("/", "_").Replace("+", "-").AsSpan(0, 8), "]");
        }

        private Task IngestMessage(char[] messageBuffer, string guid)
        {
            // Enqueue the message
            var message = new string(messageBuffer);

            // Acquire a lock before enqueuing the message to the buffer
            bool lockAcquired = Monitor.TryEnter(toViewOrganizerBufferLock, TimeSpan.FromSeconds(5));
            if (!lockAcquired)
            {
                var errorMessage = $"{guid}Lock acquisition timed out for lock {nameof(toViewOrganizerBufferLock)}";
                _logger.Error(errorMessage);
                throw new TimeoutException(errorMessage);
            }
            try
            {
                _logger.Information($"{guid}Message observed from chrome ----> {message}");
                MessageObject newMessage = new(message);
                toViewOrgainzerBuffer.Enqueue(newMessage);
                _logger.Information($"{guid}Message Enqueued to ViewOrganizerBuffer");
            }
            finally
            {
                Monitor.Exit(toViewOrganizerBufferLock);
            }
            return Task.CompletedTask;
        }

        private async Task ReadFromStdinAndWriteToFileAsync(string outputFilePath)
        {
            using StreamReader reader = new(Console.OpenStandardInput(), Encoding.UTF8);
            using var writer = new StreamWriter(File.Create(outputFilePath), Encoding.UTF8);
            while (!stoppingToken.IsCancellationRequested)
            {
                byte[] headerBuffer = new byte[4];

                var numberOfHeaderBytesRead = reader.BaseStream.Read(headerBuffer, 0, headerBuffer.Length);
                BytesAreValidMessageHeader(numberOfHeaderBytesRead, headerBuffer, out int messageLength);
                var messageBuffer = new char[messageLength];
                var numberOfBytesRead = await reader.ReadAsync(messageBuffer, 0, messageLength);

            
                await writer.WriteAsync(messageBuffer, 0, numberOfBytesRead);
            }
        }

        private bool BytesAreValidMessageHeader(int numberOfBytesRead, byte[] bytesRead, out int messageLength)
        {
            messageLength = 0;
            if (numberOfBytesRead != 4)
            {
                return false;
            }
            try
            {
                messageLength = BitConverter.ToInt32(bytesRead, 0);
            }
            catch (Exception)
            {
                _logger.Error("Unable to convert byte array to message length");
                return false;
            }
            return true;
        }

        private async Task ListenForMessagesFromViewOrganizer(ConcurrentQueue<MessageObject> buffer)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                fromViewOrganizerPipeServer ??= new(
                    viewOrganizerPipeNameFrom,
                    PipeDirection.In);
                _logger.Information("Native Messaging Host is waiting for messages from View Organizer...");

                try
                {
                    using StreamReader reader = new(fromViewOrganizerPipeServer, Encoding.UTF8);

                    _ = CleanupReader(reader);
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
                        MessageObject newMessage = new(message);
                        buffer.Enqueue(newMessage);
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

        private async Task CleanupReader(StreamReader reader)
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                if (stoppingToken.IsCancellationRequested)
                {
                    _logger.Information("Reader Disposed due to cancellation token");
                    reader.Dispose();
                    break;
                }
            }
        }

        private async Task ProcessMessages(string bufferName, ConcurrentQueue<MessageObject> buffer, Func<string, Task> sendMessage, object bufferLock, object processLock)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (!buffer.IsEmpty)
                {
                    // Generate a unique ID to group log messages in an aggregate log file for readability
                    string guid = GenerateContrastGuid();

                    _logger.Information($"{guid}Buffer not empty");
                    string? message = null;

                    // Dequeue the message from the buffer
                    bool lockAcquired = Monitor.TryEnter(bufferLock, TimeSpan.FromSeconds(5));
                    if (!lockAcquired)
                    {
                        var errorMessage = $"{guid}Lock acquisition timed out for lock {bufferLock.GetHashCode()}";
                        _logger.Error(errorMessage);
                        throw new TimeoutException(errorMessage);
                    }
                    try
                    {
                        if (buffer.TryDequeue(out MessageObject? messageObj))
                        {
                            if(DateTime.Now.Millisecond - messageObj.arrival.Millisecond > messageTTL)
                            {
                                _logger.Error($"{guid}Message was outside its TTL and will be discarded.");
                                break;
                            }
                            message = messageObj.message;
                            if (bufferName == "viewOrganizer")
                            {
                                var messageDeseriJson = JsonConvert.DeserializeObject<HeartbeatCheck>(message);
                                if (messageDeseriJson.Action == "heartbeat")
                                {
                                    _logger.Information($"{guid}Heartbeat KeepAlive noticed in transit");
                                    keepAlive = true;
                                    break;
                                }
                                else
                                {
                                    _logger.Information($"{guid}NON-Heartbeat KeepAlive triggered");
                                    keepAlive = true;
                                }
                            }
                        }
                        else
                        {
                            var errorMessage = $"{guid}Try Dequeue failed to pull from the buffer.";
                            _logger.Error(errorMessage);
                            throw new BufferTryDequeueException(errorMessage);
                        }
                    }
                    finally
                    {
                        Monitor.Exit(bufferLock);
                    }

                    if (message != null)
                    {
                        var sendTask = SendWithLock(sendMessage, message, processLock);
                        await Task.WhenAny(Task.Delay(TimeSpan.FromMilliseconds(sendTimeout)), sendTask);
                        if (!sendTask.IsCompleted)
                        {
                            _logger.Error($"{guid}Message send from {bufferName} timed out during send.");
                        }
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
            _logger.Information($"Message to be sent to ViewOrganizer ---> {message}");

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