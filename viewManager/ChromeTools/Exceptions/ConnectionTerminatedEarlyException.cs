using System.Runtime.Serialization;

namespace ChromeTools.Exceptions
{
    [Serializable]
    internal class ConnectionTerminatedEarlyException : Exception
    {
        public ConnectionTerminatedEarlyException()
        {
        }

        public ConnectionTerminatedEarlyException(string? message) : base(message)
        {
        }

        public ConnectionTerminatedEarlyException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ConnectionTerminatedEarlyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}