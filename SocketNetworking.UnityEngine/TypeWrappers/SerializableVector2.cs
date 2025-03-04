﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.TypeWrappers
{
    public class SerializableVector2 : IPacketSerializable
    {
        public Vector2 Vector;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Vector = reader.ReadVector2();
            return reader;
        }

        public int GetLength()
        {
            return sizeof(float) * 2;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteVector2(Vector);
            return writer;
        }
    }
}
