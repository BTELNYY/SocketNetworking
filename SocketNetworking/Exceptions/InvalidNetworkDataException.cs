using System;

namespace SocketNetworking.Exceptions
{
    public class InvalidNetworkDataException : Exception
    {
        public override string Message => _message;

        private string _message;

        public InvalidNetworkDataException(string message)
        {
            _message = message;
        }
    }
}
