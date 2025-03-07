using System;

namespace SocketNetworking.Exceptions
{
    public class NetworkDeserializationException : Exception
    {
        public NetworkDeserializationException() { }

        public NetworkDeserializationException(string message) : base(message)
        {

        }
    }
}
