using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BasicChat.Shared;
using SocketNetworking;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;

namespace BasicChat.Client
{
    public class Program
    {
        static string Title = "ClientID: {id}, Latency: {ms}";

        static string IP = "127.0.0.1";

        static ushort Port = 7777; 

        public static string Name = "???";

        static Thread reader;

        public static void Main(string[] args)
        {
            Log.OnLog += Logger.HandleNetworkLog;
            Log.Levels = Log.FULL_LOG;

            NetworkManager.ImportAssmebly(Utility.GetAssembly());
            reader = new Thread(HandleInput);

            Console.WriteLine("Enter your name");
            Name = Console.ReadLine();

            Console.WriteLine("Enter destination IP address (127.0.0.1):");
            IP = Console.ReadLine();
            if(IP == "")
            {
                IP = "127.0.0.1";
            }

            Console.WriteLine("Enter destination port (7777):");
            string portStr = Console.ReadLine();
            if(portStr == "")
            {
                portStr = "7777";
            }
            Port = ushort.Parse(portStr);

            ChatClient client = new ChatClient();
            client.AuthenticationStateChanged += () =>
            {
                if(client.Authenticated)
                {
                    //Console.Clear();
                }
            };
            client.ClientDisconnected += () =>
            {
                reader.Abort();
                Console.ReadKey();
                Environment.Exit(0);
            };
            client.ReadyStateChanged += (old, @new) => 
            {
                if(@new)
                {
                    Program.reader.Start();
                }
            };
            client.AvatarChanged += (avatar) =>
            {
                if (avatar is ChatAvatar chatAvatar)
                {
                    //chatAvatar.ClientSetName(Name);
                }
            };
            client.MessageReceived += (handle, message) =>
            {
                INetworkObject obj = NetworkManager.GetNetworkObjectByID(message.Sender).Item1;
                if (obj == null)
                {
                    return;
                }
                if (!(obj is ChatAvatar avatar))
                {
                    return;
                }
                lock (locker)
                {
                    Console.Write(new string('\b', buffer.Count));
                    var msg = $"{avatar.Name}: {message.Content}";
                    var excess = buffer.Count - msg.Length;
                    if (excess > 0) msg += new string(' ', excess);
                    Logger.WriteLineColor(msg, message.Color);
                    Console.Write(new string(buffer.ToArray()));
                }
            };
            client.ClientIdUpdated += () =>
            {
                Console.Title = Title.Replace("{id}", client.ClientID.ToString()).Replace("{ms}", client.Latency.ToString());
            };
            client.LatencyChanged += (latency) =>
            {
                Console.Title = Title.Replace("{id}", client.ClientID.ToString()).Replace("{ms}", client.Latency.ToString());
            };
            client.InitLocalClient();
            client.RequestedName = Name;
            client.Connect(IP, Port);
        }

        static object locker = new object();
        static List<char> buffer = new List<char>();

        static void HandleInput()
        {
            while (NetworkClient.LocalClient.IsConnected)
            {
                var k = Console.ReadKey();
                if (k.Key == ConsoleKey.Enter && buffer.Count > 0)
                {
                    lock (locker)
                    {
                        if (NetworkClient.LocalClient is ChatClient chatClient)
                        {
                            if (buffer[0] == '>')
                            {
                                buffer.RemoveRange(0, Math.Min(2, buffer.Count));
                            }
                            chatClient.ClientSendMessage(string.Join("", buffer));
                        }
                        Console.WriteLine();
                        buffer.Clear();
                        buffer.AddRange("> ");
                        Console.Write(buffer.ToArray());
                    }
                }
                else
                {
                    if(k.Key == ConsoleKey.Backspace)
                    {
                        buffer.RemoveAt(buffer.Count - 1);
                    }
                    buffer.Add(k.KeyChar);
                }
            }
        }
    }
}
