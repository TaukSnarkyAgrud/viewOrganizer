using ChromeTools.Exceptions;
using System.IO.Pipes;
using System.Text;

namespace ChromeTools
{
    public class ChromeApiHelper
    {
        // Pipe name for communication with Native Messaging Host
        private const string pipeName = "viewOrganizerPipe";

        // Timeout for listening to replies from the Native Messaging Host (5 seconds)
        private static readonly TimeSpan replyTimeout = TimeSpan.FromSeconds(5);

        public async Task<string> SendMessage(string action)
        {
            try
            {
                // Open the pipe to Native Messaging Host
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut))
                {
                    if (!pipeClient.IsConnected) 
                    {
                        // Connect to Native Messaging Host
                        await pipeClient.ConnectAsync();
                    }
                    
                    // Prepare the message to send to Native Messaging Host
                    string messageToSend = CreateMessage(action);

                    // Write the message to the pipe
                    byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
                    if (pipeClient.IsConnected)
                    {
                        await pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length);
                    }
                    else
                    {
                        throw new ConnectionTerminatedEarlyException("The Connection was terminated before the message could be sent.");
                    }

                    // Flush the message to ensure it is sent immediately
                    await pipeClient.FlushAsync();

                    // Read the replies from Native Messaging Host
                    string reply = await ReadRepliesFromNMH(pipeClient);

                    if (pipeClient.IsConnected)
                    {
                        // Close the pipe (optional, but recommended)
                        pipeClient.Close();
                    }

                    return reply;
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the communication
                return $"Error: {ex.Message}";
            }
        }

        private async Task<string> ReadRepliesFromNMH(NamedPipeClientStream pipeClient)
        {
            // Prepare a StringBuilder to accumulate all replies
            StringBuilder allReplies = new StringBuilder();

            // Prepare a cancellation token for the timeout
            using (var cancellationTokenSource = new CancellationTokenSource(replyTimeout))
            {
                try
                {
                    // Read replies from Native Messaging Host until the timeout is reached
                    byte[] buffer = new byte[4096];
                    while (true)
                    {
                        int bytesRead = await pipeClient.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                        if (bytesRead == 0)
                        {
                            // End of stream, no more replies
                            break;
                        }

                        // Convert the received bytes to a string
                        string reply = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        allReplies.Append(reply);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Timeout reached, stop reading replies
                }
            }

            return allReplies.ToString();
        }

        private string CreateMessage(string action)
        {
            // Prepare the message in the required format with the specified action
            // For example, you can use JSON format to represent the message
            // The following is just a simple example; you can adjust it as needed
            return $"{{\"action\": \"{action}\"}}";
        }
    }
}