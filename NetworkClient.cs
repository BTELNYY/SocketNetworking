using SocketNetworking.PacketSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketNetworking
{
    public class NetworkClient
    {
        private int _clientId = 0;

        public int ClientID 
        { 
            get 
            {
                return _clientId;
            }
        }

        private TcpClient _tcpClient;

        public TcpClient TcpClient
        {
            get
            {
                return _tcpClient;
            }
        }

        public NetworkStream NetworkStream
        {
            get
            {
                return TcpClient.GetStream();
            }
        }

        private Thread _clientThread;

        public Thread ClientThread
        {
            get
            {
                return _clientThread;
            }
        }

        private bool _shuttingDown = false;

        public NetworkClient(int clientId, TcpClient socket)
        {
            _clientId = clientId;
            _tcpClient = socket;
            _clientThread = new Thread(ClientStartThread);
            _clientThread.Start();
        }

        private void ClientStartThread()
        {
            Log.Info($"Client thread started, ID {ClientID}");
            while (true)
            {
                byte[] buffer = new byte[Packet.MaxPacketSize];
                if (_shuttingDown)
                {
                    break;
                }
                int count = NetworkStream.Read(buffer, 0, TcpClient.ReceiveBufferSize);
                Log.Debug($"Reading Incoming Packet. Size: {count}");
            }
            _tcpClient.Close();
        }
    }
}
