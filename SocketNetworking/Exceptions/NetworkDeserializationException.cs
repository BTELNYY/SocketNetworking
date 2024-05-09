using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
