using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking;
using SocketNetworking.ExampleSharedData;

namespace SocketNetworking.ExampleClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.OnLog += HandleNetworkLog;
            NetworkManager.ImportAssmebly(Utility.GetAssembly());
            TestClient client = new TestClient();
            client.InitLocalClient();
            client.Connect("127.0.0.1", 7777, "DefaultPassword");
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
