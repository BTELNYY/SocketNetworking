using System;
using System.Threading;
using BasicChat.Shared;
using SocketNetworking;
using SocketNetworking.Client;
using SocketNetworking.Misc.Console;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;

namespace BasicChat.Client
{
    public class Program
    {
        static string Title = "ClientID: {id}, Latency: {ms}ms";

        static string IP = "127.0.0.1";

        static ushort Port = 7777;

        public static string Name = "???";

        static Thread reader;

        public static void Main(string[] args)
        {
            Log.OnLog += Logger.HandleNetworkLog;
            Log.Levels = Log.FULL_LOG;

            Thread.Sleep(500);

            NetworkManager.ImportAssembly(Utility.GetAssembly());
            reader = new Thread(HandleInput);

            Console.WriteLine("Enter your name");
            Name = Console.ReadLine();

            Console.WriteLine("Enter destination IP address (127.0.0.1):");
            IP = Console.ReadLine();
            if (IP == "")
            {
                IP = "127.0.0.1";
            }

            Console.WriteLine("Enter destination port (7777):");
            string portStr = Console.ReadLine();
            if (portStr == "")
            {
                portStr = "7777";
            }
            Port = ushort.Parse(portStr);

            ChatClient client = new ChatClient();
            client.AuthenticationStateChanged += () =>
            {
                if (client.Authenticated)
                {
                    Console.Clear();
                }
            };
            client.ClientDisconnected += () =>
            {
                reader.Abort();
                Console.ReadKey();
                Environment.Exit(0);
            };
            client.AvatarChanged += (avatar) =>
            {
                if (avatar is ChatAvatar chatAvatar)
                {
                    reader.Start();
                }
            };
            client.MessageReceived += (handle, message) =>
            {
                if (message.Sender == 0)
                {
                    FancyConsole.WriteLine(message.Content, message.Color);
                    return;
                }
                INetworkObject obj = NetworkManager.GetNetworkObjectByID(message.Sender).Item1;
                if (obj == null)
                {
                    return;
                }
                if (!(obj is ChatAvatar avatar))
                {
                    return;
                }
                FancyConsole.WriteLine($"{avatar.Name}: {message.Content}", message.Color);
            };
            client.ClientIdUpdated += () =>
            {
                Console.Title = Title.Replace("{id}", client.ClientID.ToString()).Replace("{ms}", client.Latency.ToString());
            };
            client.LatencyChanged += (latency) =>
            {
                Console.Title = Title.Replace("{id}", client.ClientID.ToString()).Replace("{ms}", client.Latency.ToString());
            };

            Console.CancelKeyPress += (sender, @event) =>
            {
                NetworkClient.LocalClient.Disconnect();
            };
            //client.TcpTransport.Socket.NoDelay = true;
            client.InitLocalClient();
            client.RequestedName = Name;
            client.Connect(IP, Port);
        }

        static void HandleInput()
        {
            string cursor = "> ";
            while (NetworkClient.LocalClient.IsConnected && NetworkClient.LocalClient.Avatar != null)
            {
                ChatAvatar avatar = NetworkClient.LocalClient.Avatar as ChatAvatar;
                if (avatar.Name != default)
                {
                    cursor = $"{avatar.Name}@{NetworkClient.LocalClient.ConnectedHostname}> ";
                }
                string input = FancyConsole.ReadLine(cursor);
                ChatClient client = NetworkClient.LocalClient as ChatClient;
                client.ClientSendMessage(input);
            }
        }
    }
}
