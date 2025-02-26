﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.Streams
{
    public abstract class NetStreamBase : Stream
    {
        public event Action<int> NewDataRecieved;

        /// <summary>
        /// A maximum of 63,488 bytes can be sent every packet, you can write more per write operation, but they will be broken up into many packets.
        /// </summary>
        public const int MAX_BYTES_PER_SEND = 62 * 1024;

        NetStreamBase(NetworkClient client)
        {
            Client = client;
            Log = new Log($"[Stream {id}, Client {client.ClientID}]");
        }

        public NetworkClient Client { get; }

        public Log Log { get; }

        byte[] buffer;

        long _position = 0;

        public override long Length => throw new NotImplementedException();


        public NetStreamBase(NetworkClient client, ushort id, int bufferSize) : this(client)
        {
            this.id = id;
            this.bufferSize = bufferSize;
            this.buffer = new byte[bufferSize];
            Log.Prefix = $"[Stream {id}, Client {client.ClientID}]";
        }

        ushort id;

        long bufferSize;

        public ushort ID
        {
            get
            {
                return id;
            }
            set
            {
                if(IsOpen)
                {
                    return;
                }
                id = value;
            }
        }

        public long BufferSize
        {
            get 
            { 
                return bufferSize; 
            }
        }

        public virtual StreamMetaData GetMetaData()
        {
            return new StreamMetaData()
            {
                AllowReading = _allowRead,
                AllowWriting = _allowWrite,
                AllowSeeking = _allowSeek,
                MaxBufferSize = bufferSize,
            };
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if(count < MAX_BYTES_PER_SEND)
            {
                StreamData data = new StreamData()
                {
                    Chunk = buffer.Skip(offset).Take(count).ToArray(),
                    RequestID = 0
                };
                StreamPacket packet = new StreamPacket()
                {
                    StreamID = id,
                    Function = StreamFunction.DataSend,
                    Data = data.Serialize().Data,
                };
                Send(packet, false);
            }
            else
            {
                byte[] bufferPart = buffer.Skip(offset).Take(count).ToArray();
                int written = 0;
                while (written <= bufferPart.Length)
                {
                    StreamData data = new StreamData()
                    {
                        Chunk = buffer.Skip(offset).Take(count).ToArray(),
                        RequestID = 0
                    };
                    StreamPacket packet = new StreamPacket()
                    {
                        StreamID = id,
                        Function = StreamFunction.DataSend,
                        Data = data.Serialize().Data,
                    };
                    Send(packet, false);
                }
            }
            _position += count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when the <see cref="NetStreamBase"/> is opened on the remote.
        /// </summary>
        public virtual void OnStreamOpenedRemote()
        {

        }

        private ConcurrentDictionary<ushort, byte[]> _requests = new ConcurrentDictionary<ushort, byte[]>();

        public virtual void RecieveNetworkData(StreamPacket packet)
        {
            ByteReader reader = new ByteReader(packet.Data);
            switch(packet.Function)
            {
                case StreamFunction.DataSend:
                    StreamData data = reader.ReadPacketSerialized<StreamData>();
                    if(data.RequestID == 0)
                    {
                        buffer = buffer.Push(data.Chunk);
                        NewDataRecieved?.Invoke(data.Chunk.Length);
                    }
                    else
                    {
                        if(_requests.ContainsKey(data.RequestID))
                        {
                            Log.Error($"Request ID {data.RequestID} has already been recieved! There is no way you have {ushort.MaxValue} pending data requests right?");
                        }
                        else
                        {
                            _requests.TryAdd(data.RequestID, data.Chunk);
                        }
                    }
                    break;
                case StreamFunction.Close:
                    _isOpen = false;
                    Log.Debug($"Stream was closed.");
                    Close();
                    break;
                case StreamFunction.Accept:
                case StreamFunction.MetaData:
                    StreamMetaData metaData = reader.ReadPacketSerialized<StreamMetaData>();
                    if(packet.Function == StreamFunction.Accept)
                    {
                        _isOpen = true;
                        OnStreamOpenedRemote();
                    }
                    buffer = new byte[metaData.MaxBufferSize];
                    _allowRead = metaData.AllowReading;
                    _allowWrite = metaData.AllowWriting;
                    _allowSeek = metaData.AllowSeeking;
                    break;
                case StreamFunction.Reject:
                    _isOpen = false;
                    Close();
                    Log.Error($"Stream {id} was rejected by remote.");
                    break;
                case StreamFunction.DataRequest:
                    StreamRequestData requestData = reader.ReadPacketSerialized<StreamRequestData>();
                    StreamPacket dataReqResponse = new StreamPacket()
                    {
                        Function = StreamFunction.DataSend,
                        StreamID = ID,
                    };
                    StreamData streamResponseData = new StreamData()
                    {
                        Chunk = new byte[0],
                        RequestID = requestData.RequestID,
                    };
                    if (CanSeek)
                    {
                        byte[] result = buffer.SkipLong(requestData.Index).ToArray();
                        streamResponseData.Chunk = result.Take(requestData.Length).ToArray();
                    }
                    else
                    {
                        dataReqResponse.Error = true;
                        dataReqResponse.ErrorMessage = "Seeking is not supported on this stream.";
                    }
                    dataReqResponse.Data = streamResponseData.Serialize().Data;
                    Send(dataReqResponse, false);
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

        public override bool CanWrite => IsOpen && AllowWrite;

        public override bool CanSeek => IsOpen && AllowSeek;

        public override bool CanRead => IsOpen && AllowRead;

        public bool UsePriority { get; set; }

        public virtual ByteWriter GetOpenData()
        {
            return new ByteWriter();
        }

        public virtual ByteReader SetOpenData(ByteReader reader)
        {
            return reader;
        }

        public virtual void Open()
        {
            Client.Streams.Open(this);
        }

        public virtual void OpenBlocking()
        {
            Open();
            while(!IsOpen)
            {

            }
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

        public virtual void Send(Packet packet, bool usePriority)
        {
            Client.Send(packet, usePriority);
        }
    }


    public struct StreamResponseData : IPacketSerializable
    {
        public StreamError Error;

        public string Message;

        public bool Continue;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Error = (StreamError)reader.ReadByte();
            Message = reader.ReadString();
            Continue = reader.ReadBool();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().DataLength;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteByte((byte)Error);
            writer.WriteString(Message);
            writer.WriteBool(Continue);
            return writer;
        }

        public enum StreamError : byte
        {
            None,
            GeneralError,
        }
    }

    public struct StreamMetaData : IPacketSerializable
    {
        public long MaxBufferSize;

        public bool AllowSeeking;

        public bool AllowReading;

        public bool AllowWriting;

        public int GetLength()
        {
            return Serialize().DataLength;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteLong(MaxBufferSize);
            writer.WriteBool(AllowSeeking);
            writer.WriteBool(AllowReading);
            writer.WriteBool(AllowWriting);
            return writer;
        }

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            MaxBufferSize = reader.ReadLong();
            AllowSeeking = reader.ReadBool();
            AllowReading = reader.ReadBool();
            AllowWriting = reader.ReadBool();
            return reader;
        }
    }

    public struct StreamRequestData : IPacketSerializable
    {
        public ushort RequestID;

        public long Index;

        public int Length;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            RequestID = reader.ReadUShort();
            Index = reader.ReadLong();
            Length = reader.ReadInt();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().DataLength;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteUShort(RequestID);
            writer.WriteLong(Index);
            writer.WriteInt(Length);
            return writer;
        }
    }

    public struct StreamData : IPacketSerializable
    {
        public ushort RequestID;

        public byte[] Chunk;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            RequestID = reader.ReadUShort();
            Chunk = reader.ReadByteArray();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().Data.Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteUShort(RequestID);
            writer.WriteByteArray(Chunk);
            return writer;
        }
    }
}
