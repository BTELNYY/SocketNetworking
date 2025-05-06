using System;

namespace SocketNetworking.Shared.Exceptions
{
    /// <summary>
    /// The <see cref="NetworkConversionException"/> is thrown when converting data fails.
    /// </summary>
    public class NetworkConversionException : Exception
    {
        public NetworkConversionException() { }

        public NetworkConversionException(string message) : base(message)
        {

        }
    }
}
