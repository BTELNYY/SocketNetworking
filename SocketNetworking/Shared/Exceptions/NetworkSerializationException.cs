using System;

namespace SocketNetworking.Shared.Exceptions
{
    public class NetworkSerializationException : Exception
    {
        public NetworkSerializationException() { }

        public NetworkSerializationException(string message) : base(message)
        {

        }
    }
}
