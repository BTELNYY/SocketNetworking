using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;

namespace SocketNetworking.Shared.Streams
{
    public abstract class NetStreamBase : Stream
    {
        byte[] buffer;

        public NetStreamBase(short id, int bufferSize)
        {
            this.id = id;
            this.bufferSize = bufferSize;
            this.buffer = new byte[bufferSize];
        }

        short id;

        int bufferSize;

        public short ID
        {
            get
            {
                return id;
            }
        }

        public int BufferSize
        {
            get 
            { 
                return bufferSize; 
            }
        }

        public virtual void RecieveNetworkData(StreamPacket packet)
        {

        }

        public virtual void Open()
        {

        }

        public override void Close()
        {
            buffer = null;
        }

        public abstract void Send(Packet packet);
    }



    public struct StreamResponseData : IPacketSerializable
    {
        public StreamError Error;

        public string Message;

        public bool Continue;

        public int Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Error = (StreamError)reader.ReadByte();
            Message = reader.ReadString();
            Continue = reader.ReadBool();
            return reader.ReadBytes;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteByte((byte)Error);
            writer.WriteString(Message);
            writer.WriteBool(Continue);
            return writer.Data;
        }

        public enum StreamError : byte
        {
            None,
            GeneralError,
        }
    }

    public struct StreamMetaData : IPacketSerializable
    {
        public int MaxBufferSize;

        public bool AllowSeeking;

        public bool AllowReading;

        public bool AllowWriting;

        public int GetLength()
        {
            return Serialize().Length;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteInt(MaxBufferSize);
            writer.WriteBool(AllowSeeking);
            writer.WriteBool(AllowReading);
            writer.WriteBool(AllowWriting);
            return writer.Data;
        }

        public int Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            MaxBufferSize = reader.ReadInt();
            AllowSeeking = reader.ReadBool();
            AllowReading = reader.ReadBool();
            AllowWriting = reader.ReadBool();
            return reader.ReadBytes;
        }
    }

    public struct StreamData : IPacketSerializable
    {
        public byte[] Chunk;

        public int Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Chunk = reader.ReadByteArray();
            return reader.ReadBytes;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteByteArray(Chunk);
            return writer.Data;
        }
    }
}
