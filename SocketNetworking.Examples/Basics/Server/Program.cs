using System;
using System.Diagnostics;
using System.Threading;
using SocketNetworking.Client;
using SocketNetworking.Example.Basics.SharedData;
using SocketNetworking.Server;
using SocketNetworking.Shared;

namespace SocketNetworking.Example.Basics.Server
{
    public class Program
    {

        static string Title = "Clients: {count}";

        public static void Main(string[] args)
        {
            Log.OnLog += ExampleLogger.HandleNetworkLog;
            AppDomain.CurrentDomain.ProcessExit += (sender, evtArgs) =>
            {
                NetworkServer.ServerInstance.StopServer();
            };
            Console.CancelKeyPress += (sender, e) =>
            {
                NetworkServer.ServerInstance.StopServer();
            };
            NetworkManager.ImportAssmebly(Utility.GetAssembly());
            MixedNetworkServer server = new MixedNetworkServer();
            NetworkServer.ClientType = typeof(TestClient);
            NetworkServer.ClientAvatar = typeof(NetworkObjectTest);
            NetworkServer.Config.HandshakeTime = 10f;
            NetworkServer.Config.EncryptionMode = ServerEncryptionMode.Required;
            //speeeling
            NetworkServer.Config.CertificatePath = "./example.cert";
            NetworkServer.ClientConnected += (x) =>
            {
                Console.Title = Title.Replace("{count}", NetworkServer.Clients.Count.ToString());
            };
            NetworkServer.ClientDisconnected += (x) =>
            {
                Console.Title = Title.Replace("{count}", NetworkServer.Clients.Count.ToString());
            };
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
                        //client.NetworkInvokeSomeMethod((float)r.NextDouble(), r.Next());
                        ExampleCustomPacket packet = new ExampleCustomPacket();
                        packet.Data = "test";
                        packet.Flags = packet.Flags.SetFlag(PacketFlags.Priority, true);
                        client.Send(packet);
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
    }
}
