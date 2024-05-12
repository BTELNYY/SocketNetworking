using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class NetworkInvocationResultPacket : Packet
    {
        public override PacketType Type => PacketType.NetworkInvocationResult;

        public int CallbackID { get; set; } = 0;

        public SerializedData Result { get; set; } = new SerializedData();

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(CallbackID);
            writer.Write<SerializedData>(Result);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            CallbackID = reader.ReadInt();
            Result = reader.Read<SerializedData>();
            return reader;
        }
    }
}
