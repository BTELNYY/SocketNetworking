using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking;
using SocketNetworking.Example.SharedData;
using SocketNetworking.Transports;
using SocketNetworking.Shared;
using SocketNetworking.Client;

namespace SocketNetworking.Example.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.OnLog += ExampleLogger.HandleNetworkLog;
            NetworkManager.ImportAssmebly(Utility.GetAssembly());
            TestClient client = new TestClient();
            client.InitLocalClient();
            client.Connect("127.0.0.1", 7777, "DefaultPassword");
        }
    }
}
