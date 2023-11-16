using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem.Packets
{
    /// <summary>
    /// This generic class is meant to help with setting up <see cref="INetworkObject"/>, you can fully ignore this and never send it when creating your own, it will never be sent by the server by default.
    /// </summary>
    public sealed class NetworkObjectUpdatePacket : Packet
    {
        public override PacketType Type => PacketType.NetworkObjectUpdate;

        public NetworkObjectUpdateState UpdateState = NetworkObjectUpdateState.Nothing;

        /// <summary>
        /// This is only used for <see cref="NetworkObjectUpdateState.ObjectNetIDUpdated"/>, this contains the new value the server is listening for.
        /// </summary>
        public int NewNetworkID = 0;

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            UpdateState = (NetworkObjectUpdateState)reader.ReadByte();
            switch (UpdateState)
            {
                case NetworkObjectUpdateState.ObjectNetIDUpdated:
                    NewNetworkID = reader.ReadInt();
                    break;
            }
            return reader;
        }

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteByte((byte)UpdateState);
            switch (UpdateState)
            {
                case NetworkObjectUpdateState.ObjectNetIDUpdated:
                    writer.WriteInt(NewNetworkID);
                    break;
            }
            return writer;
        }
    }

    /// <summary>
    /// States for the object. You will need to manually create code to handle Object Creation, it is nearly impossible for me to handle all use cases.
    /// </summary>
    public enum NetworkObjectUpdateState
    {
        Nothing,
        ClientObjectCreated,
        ObjectNetIDUpdated,
        ObjectDestroyed,
    }
}
