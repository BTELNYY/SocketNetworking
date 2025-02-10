using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Example.SharedData
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
            return Assembly.GetExecutingAssembly();
        }
    }
}
