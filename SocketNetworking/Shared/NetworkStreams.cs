using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.Streams;

namespace SocketNetworking.Shared
{
    public class NetworkStreams
    {
        public event EventHandler<StreamOpenRequestEvent> StreamOpenRequest;

        public NetworkClient Client { get; }

        public NetworkStreams(NetworkClient client)
        {
            Client = client;
        }

        List<NetStreamBase> _streams = new List<NetStreamBase>();

        public ushort NextID
        {
            get
            {
                List<ushort> ids = _streams.Select(x => x.ID).ToList();
                if (ids.Count == 0)
                {
                    return 1;
                }
                ushort id = ids.GetFirstEmptySlot();
                return id;
            }
        }

        public void Open(NetStreamBase stream)
        {
            OpenInternal(stream);
            StreamPacket packet = new StreamPacket();
            ByteWriter writer = new ByteWriter();
            writer.WritePacketSerialized<StreamMetaData>(stream.GetMetaData());
            writer.Write(stream.GetOpenData().Data);
            packet.Data = writer.Data;
            packet.StreamID = stream.ID;
            packet.Function = StreamFunction.Open;
            Client.Send(packet);
        }

        void OpenInternal(NetStreamBase stream)
        {
            if (_streams.Contains(stream) || _streams.Select(x => x.ID).Contains(stream.ID))
            {
                throw new InvalidOperationException($"Stream {stream.ID} is duplicated!");
            }
            stream.ID = NextID;
            _streams.Add(stream);
        }

        public void Close(NetStreamBase stream)
        {
            _streams.Remove(stream);
        }

        public void HandlePacket(StreamPacket packet)
        {
            NetStreamBase stream = _streams.FirstOrDefault(x => x.ID == packet.StreamID);
            if (packet.Function == StreamFunction.Open)
            {
                StreamOpenRequestEvent @event = new StreamOpenRequestEvent(packet);
                StreamOpenRequest?.Invoke(this, @event);
                StreamPacket result = new StreamPacket();
                result.StreamID = packet.StreamID;
                if (@event.Accept)
                {
                    Type streamType = packet.StreamType;
                    if (streamType == null)
                    {
                        Client.Log.Error("Cannot find the type of stream.");
                    }
                    NetStreamBase streamBase = (NetStreamBase)Activator.CreateInstance(streamType);
                    streamBase.ID = packet.StreamID;
                    OpenInternal(streamBase);
                    ByteReader reader = new ByteReader(packet.Data);
                    StreamMetaData meta = reader.ReadPacketSerialized<StreamMetaData>();
                    streamBase.SetOpenData(reader);
                    result.Function = StreamFunction.Accept;
                    result.StreamID = packet.StreamID;
                    result.Data = streamBase.GetMetaData().Serialize();
                    Client.Send(result);
                }
                else
                {
                    result.Function = StreamFunction.Reject;
                    Client.Send(result);
                }
            }
            if (stream == default)
            {
                Client.Log.Error($"No such Stream '{packet.StreamID}'");
                return;
            }
            stream.RecieveNetworkData(packet);
        }
    }

    public class StreamOpenRequestEvent : EventArgs
    {
        public StreamOpenRequestEvent(StreamPacket packet) : base()
        {
            Packet = packet;
        }

        public StreamPacket Packet { get; }

        public bool Accept { get; set; } = true;

    }
}
