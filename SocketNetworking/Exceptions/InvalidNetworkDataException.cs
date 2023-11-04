using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
