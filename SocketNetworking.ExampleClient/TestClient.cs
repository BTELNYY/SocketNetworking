using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.ExampleSharedData;

namespace SocketNetworking.ExampleClient
{
    public class TestClient : NetworkClient
    {
        public int Value = 0;

        public TestClient()
        {
            ClientConnectionStateChanged += TestClient_ClientConnectionStateChanged;
        }

        private void TestClient_ClientConnectionStateChanged(NetworkClient obj)
        {
            if(obj.CurrentConnectionState == ConnectionState.Connected)
            {
                ExampleCustomPacket exampleCustomPacket = new ExampleCustomPacket();
                exampleCustomPacket.NetowrkIDTarget = 0;
                exampleCustomPacket.Data = "lah (sent from client)";
                Send(exampleCustomPacket);
            }
        }

        [PacketListener(typeof(ExampleCustomPacket), PacketDirection.Server)]
        public void OnExamplePacketGottenClient(ExampleCustomPacket packet, NetworkClient client)
        {
            Log.Info("Gotten on SERVER listener! " + packet.Data);
        }

        [PacketListener(typeof(ExampleCustomPacket), PacketDirection.Server)]
        public void OnExamplePacketGotten(ExampleCustomPacket packet, NetworkClient client)
        {
            Log.Info("Gotten on CLIENT listener! " + packet.Data);
        }
    }
}
