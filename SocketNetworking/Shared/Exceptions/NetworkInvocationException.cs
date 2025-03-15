using System;

namespace SocketNetworking.Shared.Exceptions
{
    public class NetworkInvocationException : Exception
    {
        public NetworkInvocationException() { }

        public NetworkInvocationException(string message) : base(message) { }

        public NetworkInvocationException(string message, Exception innerException) : base(message, innerException) { }

    }
}
