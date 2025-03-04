using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.PacketSystem.Packets
{
    /// <summary>
    /// Base class for all custom packets, it is the only class accepted by library. Your CustomPacketID value must be unique per class.
    /// </summary>
    public class CustomPacket : TargetedPacket
    {
        public CustomPacket() 
        {
            CustomPacketID = NetworkManager.GetAutoPacketID(this);
        }

        public CustomPacket(INetworkObject sender) : this()
        {
            NetworkIDTarget = sender.NetworkID;
        }

        public sealed override PacketType Type => PacketType.Custom;

        /// <summary>
        /// This will only be a value greater then 0 if <see cref="Type"/> is <see cref="PacketType.Custom"/>
        /// </summary>
        public virtual int CustomPacketID { get; protected set; } = -1;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(CustomPacketID);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            CustomPacketID = reader.ReadInt();
            return reader;
        }
    }
}
