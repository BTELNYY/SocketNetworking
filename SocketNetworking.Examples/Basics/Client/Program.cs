using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Transports;
using SocketNetworking.Shared;
using SocketNetworking.Client;
using SocketNetworking.Example.Basics.SharedData;

namespace SocketNetworking.Example.Basics.Client
{
    public class Program
    {
        public static NetworkClient Client;

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
            client.Connect("127.0.0.1", 7777, "DefaultPassword");
        }

        private static void Client_Stopped()
        {
            Client = null;
        }
    }
}
