using System;
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

        NetStreamBase(NetworkClient client)
        {
            Client = client;
            Log = new Log($"[Stream {id}, Client {client.ClientID}]");
        }

        public NetworkClient Client { get; }

        public Log Log { get; }

        byte[] buffer;

        public NetStreamBase(NetworkClient client, ushort id, int bufferSize) : this(client)
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
            set
            {
                if(IsOpen)
                {
                    return;
                }
                id = value;
            }
        }

        public int BufferSize
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

        /// <summary>
        /// Called when the <see cref="NetStreamBase"/> is opened on the remote.
        /// </summary>
        public virtual void OnStreamOpenedRemote()
        {

        }

        /// <summary>
        /// Called when the <see cref="NetStreamBase"/> is opened on the local client.
        /// </summary>
        public virtual void OnStreamOpenedLocal()
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
                        byte[] result = buffer.Skip(requestData.Index).ToArray();
                        streamResponseData.Chunk = result.Take(requestData.Length).ToArray();
                    }
                    else
                    {
                        dataReqResponse.Error = true;
                        dataReqResponse.ErrorMessage = "Seeking is not supported on this stream.";
                    }
                    dataReqResponse.Data = streamResponseData.Serialize();
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

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return base.FlushAsync(cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return base.EndRead(asyncResult);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return base.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return base.BeginWrite(buffer, offset, count, callback, state);
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

    public struct StreamRequestData : IPacketSerializable
    {
        public ushort RequestID;

        public int Index;

        public int Length;

        public int Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            RequestID = reader.ReadUShort();
            Index = reader.ReadInt();
            Length = reader.ReadInt();
            return reader.ReadBytes;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteUShort(RequestID);
            writer.WriteInt(Index);
            writer.WriteInt(Length);
            return writer.Data;
        }
    }

    public struct StreamData : IPacketSerializable
    {
        public ushort RequestID;

        public byte[] Chunk;

        public int Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            RequestID = reader.ReadUShort();
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
            writer.WriteUShort(RequestID);
            writer.WriteByteArray(Chunk);
            return writer.Data;
        }
    }
}
