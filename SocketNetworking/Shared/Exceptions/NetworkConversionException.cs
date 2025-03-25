using System;

namespace SocketNetworking.Shared.Exceptions
{
    public class NetworkConversionException : Exception
    {
        public NetworkConversionException() { }

        public NetworkConversionException(string message) : base(message)
        {

        }
    }
}
