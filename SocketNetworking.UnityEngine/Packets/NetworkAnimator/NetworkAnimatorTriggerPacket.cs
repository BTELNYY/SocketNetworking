using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.UnityEngine.Packets.NetworkAnimator
{
    [PacketDefinition]
    public class NetworkAnimatorTriggerPacket : CustomPacket
    {
        public int Hash { get; set; } = 0;

        public void WriteName(string name)
        {
            Hash = Animator.StringToHash(name);
        }

        public bool SetTrigger { get; set; } = false;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(Hash);
            writer.WriteBool(SetTrigger);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            Hash = reader.ReadInt();
            SetTrigger = reader.ReadBool();
            return reader;
        }
    }
}
