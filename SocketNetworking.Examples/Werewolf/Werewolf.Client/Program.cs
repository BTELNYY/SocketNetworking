using System;
using System.Threading.Tasks;
using SocketNetworking;
using SocketNetworking.Misc.Console;
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
            client.AvatarChanged += (x) =>
            {
                //it will always be this avatar.
                PlayerAvatar avatar = x as PlayerAvatar;
                avatar.MessageReceived += Avatar_MessageReceived;
                avatar.OnPlayerDeath += Avatar_OnPlayerDeath;
                avatar.OnPlayerTurn += Avatar_OnPlayerTurn;
                avatar.TeamChanged += Avatar_TeamChanged;
                avatar.NameChanged += Avatar_NameChanged;
            };
            _ = Task.Run(async () =>
            {
                client.ClientConfiguration = new SocketNetworking.Shared.Messages.ProtocolConfiguration("Werewolf", "1.0.0");
                client.ClientName = Name;
                client.InitLocalClient();
                await client.ConnectAsync(IP, Port);
            });
            while (true)
            {
                //wait for connection
                while (client.PlayerAvatar == null)
                {

                }
                string input = FancyConsole.ReadLine("(Use / for commands, Otherwise speak): ");
                client.PlayerAvatar.ClientSendMessage(input);
            }
        }

        private static void Avatar_NameChanged(string obj)
        {
            FancyConsole.WriteLine($"Your name has been changed to: {obj.Replace("\n", "")}");
        }

        private static void Avatar_TeamChanged(Team obj)
        {
            FancyConsole.WriteLineAutoColor($"Your team was updated to: {FancyConsole.SpecialMarker}{GameManager.GetTeamColor(obj)}{obj}");
        }

        private static void Avatar_OnPlayerTurn()
        {
            FancyConsole.WriteLineAutoColor($"You have been bitten. You are now a {FancyConsole.SpecialMarker}{GameManager.GetTeamColor(Team.Werewolves)}Werewolf.");
        }

        private static void Avatar_OnPlayerDeath(string obj)
        {
            FancyConsole.WriteLineAutoColor($"You died! Reason: {obj}");
        }

        private static void Avatar_MessageReceived(SocketNetworking.Shared.NetworkHandle arg1, string arg2)
        {
            FancyConsole.WriteLineAutoColor(arg2);
        }
    }
}
