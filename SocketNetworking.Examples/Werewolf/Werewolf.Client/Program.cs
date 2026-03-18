using System;
using SocketNetworking;
using Werewolf.Shared;

namespace Werewolf.Client
{
    public class Program
    {
        public static string Name { get; private set; }

        public static string IP { get; private set; }

        public static ushort Port { get; private set; }

        static void Main(string[] args)
        {
            Log.Levels = Log.FULL_LOG;
            Log.OnLog += ExampleLogger.HandleNetworkLog;
            Utility.ImportHelper();
            Console.WriteLine("What is your name?");
            Console.Write(": ");
            Name = Console.ReadLine();
            Console.WriteLine("Enter destination port (127.0.0.1):");
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
            WerewolfClient client = new WerewolfClient();
            client.ClientName = Name;
            client.InitLocalClient();
            client.Connect(IP, Port);
        }
    }
}
