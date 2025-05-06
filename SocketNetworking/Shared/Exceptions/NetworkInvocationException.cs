using System;

namespace SocketNetworking.Shared.Exceptions
{
    /// <summary>
    /// The <see cref="NetworkInvocationException"/> is thrown when a Network Invoke call has failed.
    /// </summary>
    public class NetworkInvocationException : Exception
    {
        public NetworkInvocationException() { }

        public NetworkInvocationException(string message) : base(message) { }

        public NetworkInvocationException(string message, Exception innerException) : base(message, innerException) { }

    }
}
