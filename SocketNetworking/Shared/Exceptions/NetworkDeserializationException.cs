using System;

namespace SocketNetworking.Shared.Exceptions
{
    public class NetworkDeserializationException : Exception
    {
        public NetworkDeserializationException() { }

        public NetworkDeserializationException(string message) : base(message)
        {

        }
    }
}
