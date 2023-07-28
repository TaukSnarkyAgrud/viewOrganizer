using System.Runtime.Serialization;

namespace ChromeMessagingServiceHost
{
    [Serializable]
    internal class EmptyBufferException : Exception
    {
        public EmptyBufferException()
        {
        }

        public EmptyBufferException(string message) : base(message)
        {
        }

        public EmptyBufferException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected EmptyBufferException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}