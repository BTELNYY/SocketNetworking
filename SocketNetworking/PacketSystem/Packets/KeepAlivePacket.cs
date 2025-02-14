using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class KeepAlivePacket : Packet
    {
        public override PacketType Type => PacketType.KeepAlive;
    }
}
