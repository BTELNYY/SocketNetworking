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
    public class SyncedStream : Stream
    {
        public event Action<int> NewDataRecieved;

        /// <summary>
        /// A maximum of 63,488 bytes can be sent every packet, you can write more per write operation, but they will be broken up into many packets.
        /// </summary>
        public const int MAX_BYTES_PER_SEND = 62 * 1024;

        SyncedStream(NetworkClient client)
        {
            Client = client;
            Log = new Log($"[Stream {id}, Client {client.ClientID}]");
        }

        public NetworkClient Client { get; }

        public Log Log { get; }

        byte[] buffer;

        public int BufferSize
        {
            get
            {
                return buffer.Length;
            }
        }

        /// <summary>
        /// The Length of the <see cref="SyncedStream"/>. Same as <see cref="Available"/>
        /// </summary>
        public override long Length => Available;

        long _available;

        /// <summary>
        /// Amount of bytes which are yet to be read
        /// </summary>
        public long Available
        {
            get
            {
                return _available;
            }
            protected set
            {
                _available = Math.Min(bufferSize, value);
            }
        }

        public SyncedStream(NetworkClient client, ushort id, int bufferSize) : this(client)
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
                while(bufferPart.Length > 0)
                {
                    byte[] slice = new byte[Math.Min(bufferPart.Length, MAX_BYTES_PER_SEND)];
                    for(int i = 0; i < count && i < MAX_BYTES_PER_SEND; i++)
                    {
                        slice[i] = bufferPart[i];
                    }
                    StreamData data = new StreamData()
                    {
                        Chunk = slice,
                        RequestID = 0
                    };
                    StreamPacket packet = new StreamPacket()
                    {
                        StreamID = id,
                        Function = StreamFunction.DataSend,
                        Data = data.Serialize().Data,
                    };
                    Send(packet, false);
                    bufferPart = bufferPart.RemoveFromStart(slice.Length);
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(offset > Available)
            {
                throw new ArgumentException("Offset out of range.");
            }
            int readCount = 0;
            for(int i = offset; i < count && i < Available; i++)
            {
                buffer[i - offset] = this.buffer[i];
                readCount = i;
            }
            this.buffer = this.buffer.RemoveFromStart(readCount);
            Available -= readCount;
            return readCount;
        }

        /// <summary>
        /// Called when the <see cref="SyncedStream"/> is opened on the remote.
        /// </summary>
        public virtual void OnStreamOpenedRemote()
        {
            Log.Info($"Stream has been accepted and opened with buffer size {bufferSize}.");
        }

        private ConcurrentDictionary<ushort, byte[]> _requests = new ConcurrentDictionary<ushort, byte[]>();

        public virtual void RecieveNetworkData(StreamPacket packet)
        {
            ByteReader reader = new ByteReader(packet.Data);
            if(packet.Error)
            {
                Log.Error($"Remote error: {packet.ErrorMessage}");
            }
            switch(packet.Function)
            {
                case StreamFunction.DataSend:
                    StreamData data = reader.ReadPacketSerialized<StreamData>();
                    if(data.RequestID == 0)
                    {
                        if(Available + data.Chunk.Length > BufferSize)
                        {
                            buffer = buffer.RemoveFromStart(data.Chunk.Length);
                        }
                        buffer = buffer.AppendAll(data.Chunk);
                        Available += data.Chunk.LongLength;
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

        protected bool _allowWrite = true;

        public bool AllowWrite
        {
            get
            {
                return _allowWrite;
            }
        }

        protected bool _allowRead = true;

        public bool AllowRead
        {
            get
            {
                return _allowRead;
            }
        }

        protected bool _allowSeek = false;

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

        /// <summary>
        /// The position in a stream does nothing within the <see cref="SyncedStream"/> class as you cannot rewind or fast forward the internet.
        /// </summary>
        public override long Position { get; set; }

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
            Log.Info($"Stream open requested with a buffer size of {bufferSize}");
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

        /// <summary>
        /// Since all changes are written to the network as they happen, this method does nothing.
        /// </summary>
        public override void Flush()
        {
            
        }

        /// <summary>
        /// This method does nothing as you cannot rewind the internet.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0L;
        }

        /// <summary>
        /// This method does nothing as the <see cref="BufferSize"/> is set in the constructor.
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            
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
