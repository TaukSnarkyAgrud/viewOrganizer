using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;
using ChromeTools.Exceptions;

namespace ChromeMessagingServiceHost
{
    public class MessagingServiceViewOrganizer : BackgroundService
    {
        private readonly Serilog.ILogger _logger;
        private NamedPipeServerStream? fromViewOrganizerPipeServer;
        private NamedPipeClientStream? toViewOrganizerPipeClient;
        private CancellationTokenSource stoppingToken;

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

        public MessagingServiceViewOrganizer(Serilog.ILogger logger)
        {
            this.stoppingToken = new();
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.stoppingToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Information("MessagingServiceViewOrganizer running at: {time}");
                // Start listening for messages from View Organizer and Chrome extension in separate threads
                Task viewOrganizerListener = ListenForMessages(viewOrganizerPipeNameFrom, toChromeExtenstionBuffer);
                Task chromeExtensionListener = ListenForMessages(toViewOrgainzerBuffer);

                // Start processing messages in separate threads
                _logger.Information("Starting Message Processors at: {time}");
                Task viewOrganizerProcessor = ProcessMessages(toChromeExtenstionBuffer, SendToChromeExtension, sendToChromeExtensionLock, toChromeExtensionBufferLock);
                Task chromeExtensionProcessor = ProcessMessages(toViewOrgainzerBuffer, SendToViewOrganizer, sendToViewOrganizerLock, toViewOrganizerBufferLock);

                //// Wait for both listeners and processors to complete (optional)
                await Task.WhenAll(viewOrganizerListener, chromeExtensionListener, viewOrganizerProcessor, chromeExtensionProcessor);

                // Additional cleanup or termination logic can be added here if needed
            }
        }
        protected void StopAsync()
        {
            _logger.Information("Cancellation Requested. Stopping Worker Service.");
            base.StopAsync(this.stoppingToken.Token);
            stoppingToken.Cancel();
        }

        private async Task ListenForMessages(ConcurrentQueue<string> buffer)
        {
            // Read messages from stdin (The Customer - Chrome extension)
            using StreamReader reader = new(Console.OpenStandardInput());

            while (!stoppingToken.IsCancellationRequested)
            {
                string? message = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(message))
                {
                    // End of message or connection closed
                    break;
                }

                // Acquire a lock before enqueuing the message to the buffer
                lock (toViewOrganizerBufferLock)
                {
                    _logger.Information("Message observed from chrome at: {time}");
                    _logger.Information(message);
                    buffer.Enqueue(message);
                }
            }
        }

        private async Task ListenForMessages(string pipeName, ConcurrentQueue<string> buffer)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                fromViewOrganizerPipeServer ??= new(
                    viewOrganizerPipeNameFrom,
                    PipeDirection.In);
                _logger.Information("Native Messaging Host is waiting for messages from {pipeName}...", pipeName);

                try
                {
                    using StreamReader reader = new(fromViewOrganizerPipeServer, Encoding.UTF8);
                    _logger.Information("StreamReader Ready to receive...");

                    if (fromViewOrganizerPipeServer.IsConnected)
                    {
                        _logger.Information("Client still connected, when expected to not be connected.");
                    }
                    _logger.Information("Client not connected, listener waiting(infinitly) to connect...");
                    await fromViewOrganizerPipeServer.WaitForConnectionAsync();
                    _logger.Information("Connection (for receive) initiation observed from {pipeName}...", pipeName);

                    // Read messages from the pipe and add them to the buffer
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.Information("Attempting to read message...");
                        string? message = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(message))
                        {

                            _logger.Information("This Line was empty signifying the end of a message on the pipe or and empty send(an error)");
                            // Acquire a lock before enqueuing the message to the buffer
                            lock (toViewOrganizerBufferLock)
                            {
                                buffer.Enqueue("Error: Last received message was empty.");
                            }
                            fromViewOrganizerPipeServer?.Dispose();
                            fromViewOrganizerPipeServer = null;
                            break;
                        }

                        // Acquire a lock before enqueuing the message to the buffer
                        lock (toChromeExtensionBufferLock)
                        {
                            _logger.Information("Message observed and enqued from chrome at: {time}");
                            _logger.Information(message);
                            buffer.Enqueue(message);
                            fromViewOrganizerPipeServer?.Dispose();
                            fromViewOrganizerPipeServer = null;
                            break;
                        }
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

        private async Task ProcessMessages(ConcurrentQueue<string> buffer, Func<string, Task> sendMessage, object bufferLock, object processLock)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (!buffer.IsEmpty)
                {
                    _logger.Information("Messages preparing to be sent: {time}");
                    string? message = null;

                    // Dequeue the message from the buffer and process it under a separate lock
                    lock (bufferLock)
                    {
                        if (buffer.TryDequeue(out message))
                        {
                            // Process the message as needed
                            // (You can add custom logic here to handle the messages)
                        }
                        else
                        {
                            _logger.Error("The buffer was empty when it was expected to have contents.");
                            throw new EmptyBufferException("The buffer was empty when it was expected to have contents.");
                        }
                    }

                    if (message != null)
                    {
                        // Send the processed message to the respective recipient asynchronously
                        await SendWithLock(sendMessage, message, processLock);
                    }
                }


                // Optional: Add a delay or awaitable task to prevent busy-waiting
                await Task.Delay(100);
            }
            _logger.Information("Processors stopped due to Cancelation at: {time}");
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
            _logger.Information("Message sending to chrome at: {time}");
            _logger.Information(message);
            // Send the message to Chrome extension via stdout
            Console.Write(message.Length.ToString("x8")); // Send the message length in hexadecimal format
            Console.Write(message);
            await Console.Out.FlushAsync();
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