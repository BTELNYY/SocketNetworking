using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BasicChat.Shared;
using SocketNetworking;
using SocketNetworking.Client;
using SocketNetworking.Shared;

namespace BasicChat.Client
{
    public class Program
    {
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
            client.ClientConnected += () =>
            {
                Console.Clear();
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
                    chatAvatar.ClientSetName(Name);
                }
            };
            client.InitLocalClient();
            client.Connect(IP, Port);
        }

        static void HandleInput()
        {
            while(NetworkClient.LocalClient.IsConnected)
            {
                string input = Console.ReadLine();
                if (NetworkClient.LocalClient is ChatClient chatClient)
                {
                    chatClient.ClientSendMessage(input);
                }
            }
        }
    }
}
