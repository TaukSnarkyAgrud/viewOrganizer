using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;

namespace ChromeMessagingServiceHost
{
    public class MessagingServiceViewOrganizer : BackgroundService
    {
        private readonly Serilog.ILogger _logger;
        private NamedPipeServerStream pipeServer;
        private CancellationTokenSource stoppingToken;

        // Define the pipe names for communication with View Organizer and Chrome extension
        private const string viewOrganizerPipeName = "viewOrganizerPipe"; 

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
            pipeServer = new(
                    viewOrganizerPipeName,
                    PipeDirection.InOut);
            _logger = logger;
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.stoppingToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Information("MessagingServiceViewOrganizer running at: {time}", DateTimeOffset.Now);
                // Start listening for messages from View Organizer and Chrome extension in separate threads
                Task viewOrganizerListener = ListenForMessages(viewOrganizerPipeName, toChromeExtenstionBuffer);
                Task chromeExtensionListener = ListenForMessages(toViewOrgainzerBuffer);

                // Start processing messages in separate threads
                _logger.Information("Starting Message Processors at: {time}", DateTimeOffset.Now);
                Task viewOrganizerProcessor = ProcessMessages(toChromeExtenstionBuffer, SendToChromeExtension, sendToChromeExtensionLock, toChromeExtensionBufferLock);
                Task chromeExtensionProcessor = ProcessMessages(toViewOrgainzerBuffer, SendToViewOrganizer, sendToViewOrganizerLock, toViewOrganizerBufferLock);

                // Wait for both listeners and processors to complete (optional)
                await Task.WhenAll(viewOrganizerListener, chromeExtensionListener, viewOrganizerProcessor, chromeExtensionProcessor);

                // Additional cleanup or termination logic can be added here if needed
            }
            pipeServer.Dispose();
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
                    _logger.Information("Message observed from chrome at: {time}", DateTimeOffset.Now);
                    _logger.Information(message);
                    buffer.Enqueue(message);
                }
            }
        }

        private async Task ListenForMessages(string pipeName, ConcurrentQueue<string> buffer)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Information("Native Messaging Host is waiting for messages from {pipeName}...", pipeName);

               

                try
                {
                    using StreamReader reader = new(pipeServer, Encoding.UTF8);
                    _logger.Information("StreamReader Ready to receive...");

                    if (!pipeServer.IsConnected)
                    {
                        await pipeServer.WaitForConnectionAsync();
                        _logger.Information("Connection (for receive) initiation observed from {pipeName}...", pipeName);
                    }
                    else
                    {
                        _logger.Information("Client still connected");
                    }

                    // Read messages from the pipe and add them to the buffer
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        string? message = reader.ReadToEnd();
                        if (string.IsNullOrEmpty(message))
                        {

                            _logger.Information("This Line was empty signifying the end of a message on the pipe or and empty send(an error)");
                            // End of message or connection closed
                            break;
                        }

                        // Acquire a lock before enqueuing the message to the buffer
                        lock (toChromeExtensionBufferLock)
                        {
                            _logger.Information("Message observed from chrome at: {time}", DateTimeOffset.Now);
                            _logger.Information(message);
                            buffer.Enqueue(message);
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
            while (!stoppingToken.IsCancellationRequested && !buffer.IsEmpty)
            {
                _logger.Information("Messages preparing to be sent: {time}", DateTimeOffset.Now);
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

                // Optional: Add a delay or awaitable task to prevent busy-waiting
                await Task.Delay(100);
            }
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
            _logger.Information("Message sending to chrome at: {time}", DateTimeOffset.Now);
            _logger.Information(message);
            // Send the message to Chrome extension via stdout
            Console.Write(message.Length.ToString("x8")); // Send the message length in hexadecimal format
            Console.Write(message);
            await Console.Out.FlushAsync();
        }

        private async Task SendToViewOrganizer(string message)
        {
            _logger.Information("Message to be sent to ViewOrganizer at: {time}", DateTimeOffset.Now);
            _logger.Information(message);

            if (pipeServer.IsConnected && pipeServer.CanWrite)
            {
                _logger.Information("Pipe Connected. Message sending to ViewOrganizer at: {time}", DateTimeOffset.Now);
                using StreamWriter writer = new(pipeServer);

                await writer.WriteLineAsync(message);
                await writer.FlushAsync();
            }
        }
    }
}