using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking;

namespace SocketNetworking.ExampleServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(args.ToString());
            Log.OnLog += HandleNetworkLog;
            NetworkServer.ClientType = typeof(TestClient);
            NetworkServer.StartServer();
            NetworkServer.ClientConnected += OnClientConnected;
        }

        private static void OnClientConnected(int id)
        {
            TestClient client = (TestClient)NetworkServer.GetClient(id);
            Log.Info(client.Value.ToString());
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
