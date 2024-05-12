using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking;
using SocketNetworking.ExampleSharedData;

namespace SocketNetworking.ExampleServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.OnLog += HandleNetworkLog;
            NetworkManager.ImportCustomPackets(PacketUtils.GetAllPackets());
            NetworkServer.ClientType = typeof(TestClient);
            NetworkServer.StartServer();
            NetworkServer.ClientConnected += OnClientConnected;
            NetworkClient.ClientConnectionStateChanged += NetworkClient_ClientConnectionStateChanged;
            Thread t = new Thread(SpamThread);
            t.Start();
        }

        private static void NetworkClient_ClientConnectionStateChanged(NetworkClient obj)
        {
            if (obj.CurrentConnectionState != ConnectionState.Connected) { return; }
            Random r = new Random();
            TestClient client = (TestClient)obj;
            client.NetworkInvokeSomeMethod((float)r.NextDouble(), r.Next());
        }

        private static void SpamThread()
        {
            Random r = new Random();
            while (true)
            {
                continue;
                foreach (NetworkClient c in NetworkServer.ConnectedClients)
                {
                    if (c.IsConnected && c.Ready && c.CurrentConnectionState == ConnectionState.Connected)
                    {
                        TestClient client2 = (TestClient)c;
                        client2.NetworkInvokeSomeMethod((float)r.NextDouble(), r.Next());
                        SpamPacketTesting packet = new SpamPacketTesting()
                        {
                            ValueOne = (byte)r.Next(255),
                            ValueTwo = r.Next(),
                            ValueThree = (float)r.NextDouble(),
                            ValueFour = Convert.ToBoolean(r.Next(2))
                        };
                        c.Send(packet);
                    }
                }
            }
        }

        private static void OnClientConnected(int id)
        {
            TestClient client = (TestClient)NetworkServer.GetClient(id);
        }

        private static void HandleNetworkLog(LogData data)
        {
            ConsoleColor color = ConsoleColor.White;
            switch (data.Severity)
            {
                case LogSeverity.Debug:
                    color = ConsoleColor.Gray;
                    break;
                case LogSeverity.Info:
                    color = ConsoleColor.White;
                    break;
                case LogSeverity.Warning:
                    color = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Error:
                    color = ConsoleColor.Red;
                    break;
            }
            WriteLineColor(data.Message, color);
        }

        public static void WriteLineColor(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ResetColor();
        }
    }
}
