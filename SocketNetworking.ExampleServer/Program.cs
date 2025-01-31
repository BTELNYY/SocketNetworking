using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking;
using SocketNetworking.ExampleSharedData;
using SocketNetworking.Misc;
using SocketNetworking.Server;
using SocketNetworking.Client;
using SocketNetworking.Shared;

namespace SocketNetworking.ExampleServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.OnLog += HandleNetworkLog;
            NetworkManager.ImportAssmebly(Utility.GetAssembly());
            MixedNetworkServer server = new MixedNetworkServer();
            NetworkServer.ClientType = typeof(TestClient);
            NetworkServer.ClientAvatar = typeof(NetworkObjectTest);
            NetworkServer.Config.HandshakeTime = 10f;
            NetworkServer.Config.EncryptionMode = ServerEncryptionMode.Required;
            NetworkServer.ClientConnected += OnClientConnected;
            server.StartServer();
            Thread t = new Thread(SpamThread);
            t.Start();
        }

        private static void SpamThread()
        {
            Random r = new Random();
            Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            while (true)
            {
                break;
                Thread.Sleep(1000);
                foreach (NetworkClient c in NetworkServer.ConnectedClients)
                {
                    if (c is TestClient client && c.Ready)
                    {
                        client.NetworkInvokeSomeMethod((float)r.NextDouble(), r.Next());
                        ExampleCustomPacket packet = new ExampleCustomPacket();
                        packet.Data = "test";
                        packet.Flags = packet.Flags.SetFlag(PacketFlags.Priority, false);
                        c.Send(packet);
                    }
                    continue;
                    if (c.IsTransportConnected && c.Ready && c.CurrentConnectionState == ConnectionState.Connected)
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
