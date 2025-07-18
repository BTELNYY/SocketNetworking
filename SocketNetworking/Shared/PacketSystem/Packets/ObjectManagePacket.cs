using System;
using System.Collections.Generic;
using System.Linq;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem.TypeWrappers;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.PacketSystem.Packets
{
    public sealed class ObjectManagePacket : TargetedPacket
    {
        public ObjectManagePacket()
        {
        }

        public ObjectManagePacket(INetworkObject obj)
        {
            ObjectType = obj.GetType();
            OwnerID = obj.OwnerClientID;
            PrivilegedIDs = obj.PrivilegedIDs.ToList();
            NewNetworkID = obj.NetworkID;
            NetworkIDTarget = obj.NetworkID;
            OwnershipMode = obj.OwnershipMode;
            ObjectVisibilityMode = obj.ObjectVisibilityMode;
            Active = obj.Active;
            ExtraData = obj.SendExtraData().Data;
        }

        public override PacketType Type => PacketType.ObjectManage;

        public ObjectManageAction Action { get; set; }

        public Type ObjectType { get; set; }

        public OwnershipMode OwnershipMode { get; set; }

        public int OwnerID { get; set; }

        public List<int> PrivilegedIDs { get; set; } = new List<int>();

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
            writer.WritePacketSerialized<SerializableList<int>>(new SerializableList<int>(PrivilegedIDs));
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
            PrivilegedIDs = reader.ReadPacketSerialized<SerializableList<int>>().ContainedList;
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
