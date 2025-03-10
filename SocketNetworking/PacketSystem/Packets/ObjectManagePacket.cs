using SocketNetworking.PacketSystem.TypeWrappers;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;
using System;

namespace SocketNetworking.PacketSystem.Packets
{
    public sealed class ObjectManagePacket : TargetedPacket
    {
        public override PacketType Type => PacketType.ObjectManage;

        public ObjectManageAction Action { get; set; }

        public Type ObjectType { get; set; }

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
            writer.WriteWrapper<SerializableType, Type>(new SerializableType(ObjectType));
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
            ObjectType = reader.ReadWrapper<SerializableType, Type>();
            OwnershipMode = (OwnershipMode)reader.ReadByte();
            OwnerID = reader.ReadInt();
            ObjectVisibilityMode = (ObjectVisibilityMode)reader.ReadByte();
            NewNetworkID = reader.ReadInt();
            Active = reader.ReadBool();
            ExtraData = reader.ReadByteArray();
            return reader;
        }

        public override string ToString()
        {
            return base.ToString() + $" Action: {Action}";
        }

        public enum ObjectManageAction : byte
        {
            Create,
            ConfirmCreate,
            AlreadyExists,
            Destroy,
            ConfirmDestroy,
            Modify,
            ConfirmModify,
        }
    }
}
