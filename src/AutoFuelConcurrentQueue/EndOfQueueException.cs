using System;
using System.Runtime.Serialization;

namespace AutoFuelConcurrentQueue
{
    public class EndOfQueueException : Exception
    {
        public EndOfQueueException()
        {
        }

        protected EndOfQueueException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public EndOfQueueException(string message) : base(message)
        {
        }

        public EndOfQueueException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}