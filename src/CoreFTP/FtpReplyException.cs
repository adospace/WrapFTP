using System;
using System.Runtime.Serialization;

namespace CoreFTP
{
    [Serializable]
    internal class FtpReplyException : Exception
    {
        public FtpReplyException()
        {
        }

        public FtpReplyException(string message) : base(message)
        {
        }

        public FtpReplyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FtpReplyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}