using System.Runtime.Serialization;

namespace ChromeMessagingServiceHost.Exceptions
{
    [Serializable]
    internal class BufferTryDequeueException : Exception
    {
        public BufferTryDequeueException()
        {
        }

        public BufferTryDequeueException(string? message) : base(message)
        {
        }

        public BufferTryDequeueException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected BufferTryDequeueException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}