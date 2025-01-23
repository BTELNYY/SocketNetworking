using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class NetworkInvocationResultPacket : Packet
    {
        public override PacketType Type => PacketType.NetworkInvocationResult;

        public int CallbackID { get; set; } = 0;

        public SerializedData Result { get; set; } = new SerializedData();

        public bool Success { get; set; } = true;

        public string ErrorMessage { get; set; } = string.Empty;

        public bool IgnoreResult { get; set; } = false;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(CallbackID);
            writer.WritePacketSerialized<SerializedData>(Result);
            writer.WriteBool(Success);
            writer.WriteString(ErrorMessage);
            writer.WriteBool(IgnoreResult);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            CallbackID = reader.ReadInt();
            Result = reader.ReadPacketSerialized<SerializedData>();
            Success = reader.ReadBool();
            ErrorMessage = reader.ReadString();
            IgnoreResult = reader.ReadBool();
            return reader;
        }
    }
}
