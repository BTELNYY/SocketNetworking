using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking
{
    public interface INetworkClient
    {
        void Connect(string host, int port, string password);

        void Send(byte[] data);

        byte[] Recieve();

        bool IsConnected { get; }
    }
}
