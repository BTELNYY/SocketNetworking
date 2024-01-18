using SocketNetworking.Attributes;
using SocketNetworking.ExampleSharedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.ExampleServer
{
    public class TestClient : NetworkClient
    {
        public int Value = 1002034;

        [PacketListener(typeof(ExampleCustomPacket), PacketDirection.Server)]
        public void OnExamplePacketGottenClient(ExampleCustomPacket packet, NetworkClient client)
        {
            Log.Info("Gotten on what should be the CLIENT! " + packet.Data);
        }

        [PacketListener(typeof(ExampleCustomPacket), PacketDirection.Client)]
        public void OnExamplePacketGotten(ExampleCustomPacket packet, NetworkClient client)
        {
            Log.Info("Gotten on what should be the SERVER! " + packet.Data);
        }
    }
}
