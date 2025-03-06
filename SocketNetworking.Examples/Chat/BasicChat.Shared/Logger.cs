using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking;

namespace BasicChat.Shared
{
    public class Logger
    {
        public static void HandleNetworkLog(LogData data)
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
                case LogSeverity.Success:
                    color = ConsoleColor.Green;
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
