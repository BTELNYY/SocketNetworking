using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.Streams
{
    public abstract class NetStreamBase : Stream
    {
        NetStreamBase(NetworkClient client)
        {
            Client = client;
            Log = new Log($"[Stream {id}, Client {client.ClientID}]");
        }

        public NetworkClient Client { get; }

        public Log Log { get; }

        byte[] buffer;

        public NetStreamBase(NetworkClient client, short id, int bufferSize) : this(client)
        {
            this.id = id;
            this.bufferSize = bufferSize;
            this.buffer = new byte[bufferSize];
            Log.Prefix = $"[Stream {id}, Client {client.ClientID}]";
        }

        ushort id;

        int bufferSize;

        public ushort ID
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
            ByteReader reader = new ByteReader(packet.Data);
            switch(packet.Function)
            {
                case StreamFunction.DataSend:
                    StreamData data = reader.ReadPacketSerialized<StreamData>();
                    buffer = buffer.Push(data.Chunk);
                    break;
                case StreamFunction.Close:
                    _isOpen = false;
                    Close();
                    break;
                case StreamFunction.Accept:
                    StreamMetaData metaData = reader.ReadPacketSerialized<StreamMetaData>();
                    _isOpen = true;
                    buffer = new byte[metaData.MaxBufferSize];
                    _allowRead = metaData.AllowReading;
                    _allowWrite = metaData.AllowWriting;
                    _allowSeek = metaData.AllowSeeking;
                    break;
            }
        }

        protected bool _allowWrite;

        public bool AllowWrite
        {
            get
            {
                return _allowWrite;
            }
        }

        protected bool _allowRead;

        public bool AllowRead
        {
            get
            {
                return _allowRead;
            }
        }

        protected bool _allowSeek;

        public bool AllowSeek
        {
            get
            {
                return _allowSeek;
            }
        }

        protected bool _isOpen;

        public bool IsOpen
        {
            get
            {
                return _isOpen;
            }
        }

        public bool UsePriority { get; set; }

        public virtual void Open()
        {
            Client.Streams.Open(this);
        }

        public override void Close()
        {
            Client.Streams.Close(this);
            buffer = null;
        }

        public virtual void Send(Packet packet)
        {
            Client.Send(packet, UsePriority);
        }

        public virtual bool ValidateAcceptance(StreamMetaData data, StreamPacket packet, ByteReader reader)
        {
            return true;
        }
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
