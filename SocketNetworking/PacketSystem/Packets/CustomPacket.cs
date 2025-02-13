using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;

namespace SocketNetworking.PacketSystem.Packets
{
    /// <summary>
    /// Base class for all custom packets, it is the only class accepted by library. Your CustomPacketID value must be unique per class.
    /// </summary>
    public class CustomPacket : Packet
    {
        public override int CustomPacketID => NetworkManager.GetAutoPacketID(this);

        public CustomPacket() { }

        public CustomPacket(INetworkObject sender) 
        {
            NetowrkIDTarget = sender.NetworkID;
        }

        public sealed override PacketType Type => PacketType.CustomPacket;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            return reader;
        }
    }
}
