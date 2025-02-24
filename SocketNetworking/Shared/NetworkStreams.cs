using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.Streams;

namespace SocketNetworking.Shared
{
    public class NetworkStreams
    {
        public NetworkClient Client { get; }

        public NetworkStreams(NetworkClient client)
        {
            Client = client;
        }

        List<NetStreamBase> _streams = new List<NetStreamBase>();

        public void Open(NetStreamBase stream)
        {
            if(_streams.Contains(stream) || _streams.Select(x => x.ID).Contains(stream.ID))
            {
                throw new InvalidOperationException($"Stream {stream.ID} is duplicated!");
            }
            _streams.Add(stream);
            StreamPacket packet = new StreamPacket();
            StreamMetaData metaData = new StreamMetaData()
            {
                MaxBufferSize = stream.BufferSize,
                AllowReading = stream.AllowRead,
                AllowSeeking = stream.AllowSeek,
                AllowWriting = stream.AllowWrite
            };
            packet.Data = metaData.Serialize();
            packet.StreamID = stream.ID;
            Client.Send(packet);
        }

        public void HandlePacket(StreamPacket packet)
        {
            NetStreamBase stream = _streams.FirstOrDefault(x => x.ID == packet.StreamID);
            if(packet.Function == StreamFunction.Open)
            {

            }
            if (stream == default)
            {
                Client.Log.Error($"No such Stream '{packet.StreamID}'");
                return;
            }
        }
    }
}
