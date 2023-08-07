using ChromeTools.Exceptions;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;

namespace ChromeTools
{
    public class ChromeApiHelper
    {
        // Pipe name for communication with Native Messaging Host
        private const string toNativeMessagingHostPipeName = "fromViewOrganizerPipe";
        private const string fromMessagingHostPipeName = "toViewOrganizerPipe";

        private NamedPipeServerStream? fromNativeMessagingHostPipeServer;
        private NamedPipeClientStream? toNativeMessagingHostPipeClient;

        // Timeout for listening to replies from the Native Messaging Host (5 seconds)
        private static readonly TimeSpan replyTimeout = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan connectionTimeout = TimeSpan.FromSeconds(20);

        private ConcurrentQueue<MessageObject> messageBuffer = new ConcurrentQueue<MessageObject>();
        private CancellationTokenSource stoppingToken = new CancellationTokenSource();

        private static readonly object messageBufferLock = new();

        public async Task ListenForUpdates()
        {
            fromNativeMessagingHostPipeServer = new NamedPipeServerStream(fromMessagingHostPipeName, PipeDirection.In); 
            if (fromNativeMessagingHostPipeServer == null)
            {
                throw new NullReferenceException("PipeObject was null when it souldn't be.");
            }
            using StreamReader reader = new(fromNativeMessagingHostPipeServer, Encoding.UTF8);
            await fromNativeMessagingHostPipeServer.WaitForConnectionAsync();
            while (!stoppingToken.IsCancellationRequested)
            {
                // Read replies from Native Messaging Host until the timeout is reached
                var message = await reader.ReadLineAsync();
                //messageTask.Wait(replyTimeout.Milliseconds);
                //if (!messageTask.IsCompleted) throw new TimeoutException("The Native Message Host listener timed out waiting for a reply.");

                if (string.IsNullOrEmpty(message))
                {
                    throw new EmptyReplyException("The messsage that we received was empty.");
                }

                // Acquire a lock before enqueuing the message to the buffer
                bool lockAcquired = Monitor.TryEnter(messageBufferLock, TimeSpan.FromSeconds(5));
                if (!lockAcquired)
                {
                    Log.Error("Lock acquisition timed out for lock {theErrorLock}", messageBufferLock.GetHashCode());
                    throw new TimeoutException($"Lock acquisition timed out for lock {messageBufferLock.GetHashCode()}");
                }
                try
                {
                    Log.Information("Message observed from messaging host.");
                    Log.Information(message);
                    //var incoming = JsonConvert.DeserializeObject<GenericChromeMessage>(message);
                    //_logger.Information(incoming.Action + " " + incoming.Data);
                    MessageObject newMessage = new(message);
                    messageBuffer.Enqueue(newMessage);
                    Log.Information("Message Enqueued to ViewOrganizer");
                }
                finally
                {
                    Monitor.Exit(messageBufferLock);
                }
            }

            fromNativeMessagingHostPipeServer.Dispose();
            fromNativeMessagingHostPipeServer = null;
        }

        public bool PopMessage(out string message)
        {
            message = "";
            // Acquire a lock before enqueuing the message to the buffer
            bool lockAcquired = Monitor.TryEnter(messageBufferLock, TimeSpan.FromSeconds(5));
            if (!lockAcquired)
            {
                Log.Error("Lock acquisition timed out for lock {theErrorLock}", messageBufferLock.GetHashCode());
                throw new TimeoutException($"Lock acquisition timed out for lock {messageBufferLock.GetHashCode()}");
            }
            try
            {
                if (!messageBuffer.IsEmpty)
                {

                    messageBuffer.TryDequeue(out MessageObject? queuedMessage);
                    if (queuedMessage != null)
                    {
                        message = queuedMessage.message;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                Monitor.Exit(messageBufferLock);
            }
        }

        public async Task<string> SendMessage(string action)
        {
            try
            {
                // Open the pipe to Native Messaging Host
                toNativeMessagingHostPipeClient = new NamedPipeClientStream(".", toNativeMessagingHostPipeName, PipeDirection.Out);
                if (!toNativeMessagingHostPipeClient.IsConnected)
                {
                    // Connect to Native Messaging Host
                    await toNativeMessagingHostPipeClient.ConnectAsync();
                }

                // Prepare the message to send to Native Messaging Host
                string messageToSend = CreateMessage(action);

                // Write the message to the pipe
                byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
                if (toNativeMessagingHostPipeClient.IsConnected)
                {
                    await toNativeMessagingHostPipeClient.WriteAsync(messageBytes, 0, messageBytes.Length);
                }
                else
                {
                    throw new ConnectionTerminatedEarlyException("The Connection was terminated before the message could be sent.");
                }

                // Flush the message to ensure it is sent immediately
                await toNativeMessagingHostPipeClient.FlushAsync();
                toNativeMessagingHostPipeClient.WaitForPipeDrain();

                if (toNativeMessagingHostPipeClient.IsConnected)
                {
                    // Close the pipe (optional, but recommended)
                    toNativeMessagingHostPipeClient.Dispose();
                    toNativeMessagingHostPipeClient = null;
                }

                // Read the replies from Native Messaging Host
                string reply = await ReadReplyFromNMH();

                return reply;
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the communication
                return $"Error: {ex.Message}";
            }
        }

        private async Task<string> ReadReplyFromNMH()
        {
            fromNativeMessagingHostPipeServer = new NamedPipeServerStream(fromMessagingHostPipeName, PipeDirection.In);
            if (fromNativeMessagingHostPipeServer == null)
            {
                throw new NullReferenceException("PipeObject was null when it souldn't be.");
            }
            using StreamReader reader = new(fromNativeMessagingHostPipeServer, Encoding.UTF8);
            await fromNativeMessagingHostPipeServer.WaitForConnectionAsync();

            // Read replies from Native Messaging Host until the timeout is reached
            var messageTask = reader.ReadLineAsync();
            messageTask.Wait(replyTimeout.Milliseconds);
            if (!messageTask.IsCompleted) throw new TimeoutException("The Native Message Host listener timed out waiting for a reply.");

            if (string.IsNullOrEmpty(messageTask.Result))
            {
                throw new EmptyReplyException("The messsage that we received was empty.");
            }

            fromNativeMessagingHostPipeServer.Dispose();
            fromNativeMessagingHostPipeServer = null;

            return messageTask.Result;
        }

        private string CreateMessage(string action)
        {
            // Prepare the message in the required format with the specified action
            // For example, you can use JSON format to represent the message
            // The following is just a simple example; you can adjust it as needed
            return $"{{\"action\": \"{action}\"}}\n";
        }
    }
}