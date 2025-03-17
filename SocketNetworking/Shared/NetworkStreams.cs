using System;
using System.Collections.Generic;
using System.Linq;
using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.Streams;

namespace SocketNetworking.Shared
{
    public class NetworkStreams
    {
        public event EventHandler<StreamOpenRequestEvent> StreamOpenRequest;

        public event Action<NetworkSyncedStream> StreamOpened;

        public event Action<NetworkSyncedStream> StreamClosed;

        public NetworkClient Client { get; }

        public NetworkStreams(NetworkClient client)
        {
            Client = client;
        }

        List<NetworkSyncedStream> _streams = new List<NetworkSyncedStream>();

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

        public void Open(NetworkSyncedStream stream)
        {
            OpenInternal(stream);
            StreamPacket packet = new StreamPacket();
            ByteWriter writer = new ByteWriter();
            writer.WritePacketSerialized<StreamMetaData>(stream.GetMetaData());
            writer.Write(stream.GetOpenData().Data);
            packet.Data = writer.Data;
            packet.StreamID = stream.ID;
            packet.Function = StreamFunction.Open;
            packet.StreamType = stream.GetType();
            Client.Send(packet);
        }

        void OpenInternal(NetworkSyncedStream stream)
        {
            if (_streams.Contains(stream) || _streams.Select(x => x.ID).Contains(stream.ID))
            {
                throw new InvalidOperationException($"Stream {stream.ID} is duplicated!");
            }
            stream.ID = NextID;
            _streams.Add(stream);
        }

        public void Close(NetworkSyncedStream stream)
        {
            _streams.Remove(stream);
            StreamClosed?.Invoke(stream);
        }

        public void HandlePacket(StreamPacket packet)
        {
            NetworkSyncedStream stream = _streams.FirstOrDefault(x => x.ID == packet.StreamID);
            if (packet.Function == StreamFunction.Open)
            {
                Type streamType = packet.StreamType;
                if (streamType == null)
                {
                    Client.Log.Error("Cannot find the type of stream.");
                    return;
                }
                StreamOpenRequestEvent @event = new StreamOpenRequestEvent(packet, false);
                StreamOpenRequest?.Invoke(this, @event);
                StreamPacket result = new StreamPacket();
                result.StreamID = packet.StreamID;
                if (@event.Accepted)
                {
                    try
                    {
                        ByteReader reader = new ByteReader(packet.Data);
                        StreamMetaData meta = reader.ReadPacketSerialized<StreamMetaData>();
                        Client.Log.Info($"Stream open request accepted. ID: {packet.StreamID}, Buffer Size: {meta.MaxBufferSize}");
                        NetworkSyncedStream streamBase = (NetworkSyncedStream)Activator.CreateInstance(streamType, Client, packet.StreamID, (int)meta.MaxBufferSize);
                        streamBase.ID = packet.StreamID;
                        OpenInternal(streamBase);
                        streamBase.SetOpenData(reader);
                        result.Function = StreamFunction.Accept;
                        result.StreamID = packet.StreamID;
                        result.Data = streamBase.GetMetaData().Serialize().Data;
                        Client.Send(result);
                        StreamOpened?.Invoke(streamBase);
                    }
                    catch (Exception ex)
                    {
                        result.Function = StreamFunction.Reject;
                        result.Error = true;
                        result.ErrorMessage = ex.Message;
                        Client.Send(result);
                        return;
                    }
                }
                else
                {
                    result.Function = StreamFunction.Reject;
                    Client.Send(result);
                    Client.Log.Info($"Stream {packet.StreamID} with type {packet.StreamType} was rejected.");
                    return;
                }
            }
            else
            {
                if (stream == default)
                {
                    Client.Log.Error($"No such Stream '{packet.StreamID}'");
                    return;
                }
                stream.ReceiveNetworkData(packet);
            }
        }
    }

    public class StreamOpenRequestEvent : ChoiceEvent
    {
        public StreamOpenRequestEvent(StreamPacket packet) : base()
        {
            Packet = packet;
            StreamType = packet.StreamType;
        }

        public StreamOpenRequestEvent(StreamPacket packet, bool defaultState) : base(defaultState)
        {
            Packet = packet;
            StreamType = packet.StreamType;
        }

        public Type StreamType { get; }

        public StreamPacket Packet { get; }

    }
}
