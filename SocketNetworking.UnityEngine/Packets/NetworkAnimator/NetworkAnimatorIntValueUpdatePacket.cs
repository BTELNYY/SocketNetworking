﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Packets.NetworkAnimator
{
    [PacketDefinition]
    public class NetworkAnimatorIntValueUpdatePacket : CustomPacket
    {
        public NetworkAnimatorIntValueUpdatePacket() { }

        public NetworkAnimatorIntValueUpdatePacket(int id, int value) 
        {
            ValueHash = id; 
            Value = value;
        }

        public NetworkAnimatorIntValueUpdatePacket(string name, int value) 
        {
            ValueHash = Animator.StringToHash(name);
            Value = value;
        }

        public int ValueHash { get; set; } = 0;

        public int Value { get; set; } = 0;

        public override ByteWriter Serialize()
        {
            ByteWriter writer = base.Serialize();
            writer.WriteInt(ValueHash);
            writer.WriteInt(Value);
            return writer;
        }

        public override ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = base.Deserialize(data);
            ValueHash = reader.ReadInt();
            Value = reader.ReadInt();
            return reader;
        }
    }
}