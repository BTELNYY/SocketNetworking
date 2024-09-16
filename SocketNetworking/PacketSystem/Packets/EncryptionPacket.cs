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

        public byte[] AsymExponent { get; set; } = new byte[] { };

        public byte[] AsymModulus { get; set; } = new byte[] { };

        public byte[] SymIV { get; set; } = new byte[] { };

        public byte[] SymKey { get; set; } = new byte[] { };

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteByte((byte)EncryptionFunction);
            switch (EncryptionFunction)
            {
                case EncryptionFunction.None:
                    break;
                case EncryptionFunction.AsymmetricalKeySend:
                    writer.WriteByteArray(AsymModulus);
                    writer.WriteByteArray(AsymExponent);
                    break;
                case EncryptionFunction.SymmetricalKeySend:
                    //Enforce ASYM encryption when sending the SYM key.
                    Flags = Flags.SetFlag(PacketFlags.AsymtreicalEncrypted, true);
                    writer.WriteByteArray(SymIV);
                    writer.WriteByteArray(SymKey);
                    break;
            }
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            EncryptionFunction = (EncryptionFunction)reader.ReadByte();
            switch (EncryptionFunction)
            {
                case EncryptionFunction.AsymmetricalKeySend:
                    AsymModulus = reader.ReadByteArray();
                    AsymExponent = reader.ReadByteArray();
                    break;
                case EncryptionFunction.SymmetricalKeySend:
                    SymIV = reader.ReadByteArray();
                    SymKey = reader.ReadByteArray();
                    break;
                default:
                    break;
            }
            return reader;
        }
    }

    public enum EncryptionFunction : byte
    {
        None,
        AsymmetricalKeySend,
        AsymmetricalKeyRecieve,
        SymmetricalKeySend,
        SymetricalKeyRecieve,
    }
}
