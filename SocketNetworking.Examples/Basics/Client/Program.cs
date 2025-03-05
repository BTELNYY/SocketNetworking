using System;
using System.Diagnostics;
using SocketNetworking.Client;
using SocketNetworking.Example.Basics.SharedData;
using SocketNetworking.Shared;

namespace SocketNetworking.Example.Basics.Client
{
    public class Program
    {
        public static NetworkClient Client;

        static string Title = "ClientID: {id}, Latency: {ms}";

        public static void Main(string[] args)
        {
            Log.OnLog += ExampleLogger.HandleNetworkLog;
            NetworkManager.ImportAssmebly(Utility.GetAssembly());
            TestClient client = new TestClient();
            Client = client;
            client.InitLocalClient();
            client.ClientStopped += Client_Stopped;
            //This allows untrusted root certificates if you are using it.
            client.AllowUntrustedRootCertificates = true;
            //Accept all streams
            client.Streams.StreamOpenRequest += (sender, @event) =>
            {
                @event.Accept();
            };
            client.ClientIdUpdated += () =>
            {
                Console.Title = Title.Replace("{id}", client.ClientID.ToString()).Replace("{ms}", client.Latency.ToString());
            };
            client.LatencyChanged += (latency) =>
            {
                Console.Title = Title.Replace("{id}", client.ClientID.ToString()).Replace("{ms}", client.Latency.ToString());
            };
            client.Connect("127.0.0.1", 7777);
        }

        private static void Client_Stopped()
        {
            Client = null;
            Console.WriteLine("Client stopped, press any key to continue.....");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
