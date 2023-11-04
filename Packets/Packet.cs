using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Packets
{
    public class Packet
    {
        public const int MaxPacketSize = ushort.MaxValue;

        public virtual PacketType Type { get; } = PacketType.None;

        public virtual byte[] Serialize()
        {

        }

        public virtual Packet Deseriallize(byte[] data)
        {

        }
    }

    public enum PacketType 
    {
        None,
        Error,
        ClientData,
        ServerData,
        CustomPacket,
    }
}
