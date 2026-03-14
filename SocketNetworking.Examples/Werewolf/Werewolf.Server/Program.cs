using System;
using SocketNetworking;
using SocketNetworking.Server;
using Werewolf.Shared;

namespace Werewolf.Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Log.Levels = Log.FULL_LOG;
            Utility.ImportHelper();
            NetworkServer.ClientType = typeof(WerewolfClient);
            NetworkServer.ClientAvatar = typeof(PlayerAvatar);
            Console.WriteLine("Hello, World!");
        }
    }
}
