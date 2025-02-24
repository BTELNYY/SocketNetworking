using SocketNetworking.Shared;
using SocketNetworking.Shared.Messages;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class ObjectManagePacket : Packet
    {
        public override PacketType Type => PacketType.ObjectManage;

        public ObjectManageAction Action { get; set; }

        public string ObjectClassName { get; set; } = "";

        public string AssmeblyName { get; set; } = "";

        public OwnershipMode OwnershipMode { get; set; }

        public int OwnerID { get; set; }

        public ObjectVisibilityMode ObjectVisibilityMode { get; set; }

        public int NewNetworkID { get; set; }

        public bool Active { get; set; }

        public byte[] ExtraData { get; set; } = new byte[0];

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteByte((byte)Action);
            writer.WriteString(ObjectClassName);
            writer.WriteString(AssmeblyName);
            writer.WriteByte((byte)OwnershipMode);
            writer.WriteInt(OwnerID);
            writer.WriteByte((byte)ObjectVisibilityMode);
            writer.WriteInt(NewNetworkID);
            writer.WriteBool(Active);
            writer.WriteByteArray(ExtraData);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Action = (ObjectManageAction)reader.ReadByte();
            ObjectClassName = reader.ReadString();
            AssmeblyName = reader.ReadString();
            OwnershipMode = (OwnershipMode)reader.ReadByte();
            OwnerID = reader.ReadInt();
            ObjectVisibilityMode = (ObjectVisibilityMode)reader.ReadByte();
            NewNetworkID = reader.ReadInt();
            Active = reader.ReadBool();
            ExtraData = reader.ReadByteArray();
            return reader;
        }

        public enum ObjectManageAction : byte
        {
            Create,
            ConfirmCreate,
            Destroy,
            ConfirmDestroy,
            Modify,
            ConfirmModify,
        }
    }
}
