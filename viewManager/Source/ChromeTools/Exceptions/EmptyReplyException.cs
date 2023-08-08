using System.Runtime.Serialization;

namespace ChromeTools.Exceptions
{
    [Serializable]
    public class EmptyReplyException : Exception
    {
        public EmptyReplyException()
        {
        }

        public EmptyReplyException(string? message) : base(message)
        {
        }

        public EmptyReplyException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected EmptyReplyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}