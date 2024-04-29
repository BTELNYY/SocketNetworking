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
            Thread t = new Thread(SpamThread);
            t.Start();
        }

        private static void SpamThread()
        {
            while (true)
            {
                Random r = new Random();
                foreach (NetworkClient client in NetworkServer.ConnectedClients)
                {
                    if (client.IsConnected && client.Ready && client.CurrentConnectionState == ConnectionState.Connected)
                    {
                        SpamPacketTesting packet = new SpamPacketTesting()
                        {
                            ValueOne = (byte)r.Next(255),
                            ValueTwo = r.Next(),
                            ValueThree = (float)r.NextDouble(),
                            ValueFour = Convert.ToBoolean(r.Next(2))
                        };
                        client.Send(packet);
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
