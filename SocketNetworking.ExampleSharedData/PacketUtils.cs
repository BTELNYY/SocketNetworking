using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.ExampleSharedData
{
    public class PacketUtils
    {
        public static List<Type> GetAllPackets()
        {
            List<Type> packets = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(CustomPacket)) && x.GetCustomAttribute<PacketDefinition>() != null).ToList();
            return packets;
        }
    }
}
