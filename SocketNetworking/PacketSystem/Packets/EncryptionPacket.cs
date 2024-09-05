using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem.Packets
{
    /// <summary>
    /// <see cref="Packet"/> used for communicating encryption data, like public keys and encrypted symmetrical keys.
    /// </summary>
    public sealed class EncryptionPacket : Packet
    {
        public override PacketType Type => PacketType.EncryptionPacket;

        public EncryptionFunction EncryptionFunction { get; set; } = EncryptionFunction.None;

        public byte[] Key { get; set; } = new byte[] { };

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteByte((byte)EncryptionFunction);
            writer.WriteByteArray(Key);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            EncryptionFunction = (EncryptionFunction)reader.ReadByte();
            Key = reader.ReadByteArray();
            return reader;
        }
    }

    public enum EncryptionFunction : byte
    {
        None,
        PublicKeySend,
        SymetricalKeySend,
        EncryptionRequest,
    }
}
