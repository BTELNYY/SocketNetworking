using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.PacketSystem.Packets
{
    /// <summary>
    /// <see cref="Packet"/> used for communicating encryption data, like public keys and encrypted symmetrical keys.
    /// </summary>
    public sealed class EncryptionPacket : Packet
    {
        public override PacketType Type => PacketType.Encryption;

        public EncryptionFunction EncryptionFunction { get; set; } = EncryptionFunction.None;

        public string PublicKey { get; set; } = string.Empty;

        public byte[] SymIV { get; set; } = new byte[] { };

        public byte[] SymKey { get; set; } = new byte[] { };

        public EncryptionState State { get; set; }

        public override ByteWriter Serialize()
        {
            if(EncryptionFunction == EncryptionFunction.SymmetricalKeySend)
            {
                Flags = Flags.SetFlag(PacketFlags.AsymetricalEncrypted, true);
            }
            else
            {
                Flags = Flags.SetFlag(PacketFlags.AsymetricalEncrypted, false);
                Flags = Flags.SetFlag(PacketFlags.SymetricalEncrypted, false);
            }
            ByteWriter writer = base.Serialize();
            writer.WriteByte((byte)EncryptionFunction);
            switch (EncryptionFunction)
            {
                case EncryptionFunction.AsymmetricalKeySend:
                    writer.WriteString(PublicKey);
                    //Log.GlobalDebug("Wrote Key: " + PublicKey);
                    break;
                case EncryptionFunction.SymmetricalKeySend:
                    writer.WriteByteArray(SymIV);
                    writer.WriteByteArray(SymKey);
                    break;
                case EncryptionFunction.UpdateEncryptionStatus:
                    writer.WriteByte((byte)State);
                    break;
                default:
                    break;
            }
            //Log.GlobalDebug("Wrote: " + string.Join("-", writer.Data));
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            EncryptionFunction = (EncryptionFunction)reader.ReadByte();
            switch (EncryptionFunction)
            {
                case EncryptionFunction.AsymmetricalKeySend:
                    PublicKey = reader.ReadString();
                    //Log.GlobalDebug("Read Key: " + PublicKey);
                    break;
                case EncryptionFunction.SymmetricalKeySend:
                    SymIV = reader.ReadByteArray();
                    SymKey = reader.ReadByteArray();
                    break;
                case EncryptionFunction.UpdateEncryptionStatus:
                    State = (EncryptionState)reader.ReadByte();
                    break;
                default:
                    break;
            }
            //Log.GlobalDebug("Read: " + string.Join("-", reader.RawData));
            return reader;
        }
    }

    public enum EncryptionFunction : byte
    {
        None,
        AsymmetricalKeySend,
        AsymmetricalKeyReceive,
        SymmetricalKeySend,
        SymetricalKeyReceive,
        UpdateEncryptionStatus,
    }
}
