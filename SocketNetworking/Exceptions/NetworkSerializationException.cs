using System;

namespace SocketNetworking.Exceptions
{
    public class NetworkSerializationException : Exception
    {
        public NetworkSerializationException() { }

        public NetworkSerializationException(string message) : base(message)
        {

        }
    }
}
