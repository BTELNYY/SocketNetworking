﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.Streams;

namespace SocketNetworking.Shared
{
    public class NetworkStreams
    {
        public event EventHandler<StreamOpenRequestEvent> StreamOpenRequest;

        public event Action<SyncedStream> StreamOpened;

        public event Action<SyncedStream> StreamClosed;

        public NetworkClient Client { get; }

        public NetworkStreams(NetworkClient client)
        {
            Client = client;
        }

        List<SyncedStream> _streams = new List<SyncedStream>();

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

        public void Open(SyncedStream stream)
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

        void OpenInternal(SyncedStream stream)
        {
            if (_streams.Contains(stream) || _streams.Select(x => x.ID).Contains(stream.ID))
            {
                throw new InvalidOperationException($"Stream {stream.ID} is duplicated!");
            }
            stream.ID = NextID;
            _streams.Add(stream);
        }

        public void Close(SyncedStream stream)
        {
            _streams.Remove(stream);
            StreamClosed?.Invoke(stream);
        }

        public void HandlePacket(StreamPacket packet)
        {
            SyncedStream stream = _streams.FirstOrDefault(x => x.ID == packet.StreamID);
            if (packet.Function == StreamFunction.Open)
            {
                StreamOpenRequestEvent @event = new StreamOpenRequestEvent(packet);
                StreamOpenRequest?.Invoke(this, @event);
                Type streamType = packet.StreamType;
                if (streamType == null)
                {
                    Client.Log.Error("Cannot find the type of stream.");
                    return;
                }
                Client.Log.Info($"New stream request.");
                StreamPacket result = new StreamPacket();
                result.StreamID = packet.StreamID;
                if (@event.Accepted)
                {
                    try
                    {
                        ByteReader reader = new ByteReader(packet.Data);
                        StreamMetaData meta = reader.ReadPacketSerialized<StreamMetaData>();
                        Client.Log.Info($"Stream open request accepted. ID: {packet.StreamID}, Buffer Size: {meta.MaxBufferSize}");
                        SyncedStream streamBase = (SyncedStream)Activator.CreateInstance(streamType, (NetworkClient)Client, packet.StreamID, (int)meta.MaxBufferSize);
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
                stream.RecieveNetworkData(packet);
            }
        }
    }

    public class StreamOpenRequestEvent : ChoiceEvent
    {
        public StreamOpenRequestEvent(StreamPacket packet) : base()
        {
            Packet = packet;
        }

        public StreamPacket Packet { get; }

    }
}
