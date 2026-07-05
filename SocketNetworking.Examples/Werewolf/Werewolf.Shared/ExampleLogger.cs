using System;
using SocketNetworking;
using SocketNetworking.Misc.Console;

namespace Werewolf.Shared
{
    public class ExampleLogger
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
            FancyConsole.WriteLine(data.Message, color);
        }
    }
}
