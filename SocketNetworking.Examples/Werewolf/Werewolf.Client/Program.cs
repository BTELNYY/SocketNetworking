using System;
using SocketNetworking;
using Werewolf.Shared;

namespace Werewolf.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Log.Levels = Log.FULL_LOG;
            Utility.ImportHelper();
            Console.WriteLine("Hello, World!");
        }
    }
}
