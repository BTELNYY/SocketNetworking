using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem
{
    public class TargetedPacket : Packet
    {
        /// <summary>
        /// The NetworkID of the object which this packet is being sent to. 0 Means only sent to the other clients class.
        /// </summary>
        public int NetworkIDTarget { get; set; } = 0;

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            NetworkIDTarget = reader.ReadInt();
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(NetworkIDTarget);
            return writer;
        }
    }
}
