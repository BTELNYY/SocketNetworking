using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
