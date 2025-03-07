using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem.Packets;

namespace SocketNetworking.Example.Basics.SharedData
{
    public class Utility
    {
        public static List<Type> GetAllPackets()
        {
            List<Type> packets = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(CustomPacket)) && x.GetCustomAttribute<PacketDefinition>() != null).ToList();
            return packets;
        }

        public static Assembly GetAssembly()
        {
            Log.Levels = Log.FULL_LOG;
            return Assembly.GetExecutingAssembly();
        }
    }
}
