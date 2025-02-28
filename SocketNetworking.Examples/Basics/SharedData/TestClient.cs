using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Streams;

namespace SocketNetworking.Example.Basics.SharedData
{
    public class TestClient : MixedNetworkClient
    {
        [NetworkInvokable(NetworkDirection.Server)]
        private TestResult SomeNetworkMethod(NetworkHandle handle, float someFloat, int someInt, ValueTuple<int, int> values)
        {
            Log.GlobalDebug($"{someFloat}, {someInt}, {values.Item1 + values.Item2}");
            return TestResult.Result2;
        }

        public void NetworkInvokeSomeMethod(float someFloat, int someInt)
        {
            TestResult result = NetworkInvoke<TestResult>(this, "SomeNetworkMethod", new object[] { someFloat, someInt, new ValueTuple<int, int>(1, 3) }, priority: true);
            Log.GlobalDebug(result.ToString());
        }

        [PacketListener(typeof(ExampleCustomPacket), NetworkDirection.Any)]
        public void Listener(ExampleCustomPacket packet, NetworkHandle handle)
        {
            Log.GlobalInfo($"Got a packet! Data: {packet.Data}, Flags: {packet.Flags.GetActiveFlagsString()}, Encrypted?: {handle.WasEncrypted}");
            Log.GlobalInfo($"Packet currently: {packet.Data}");
            Log.GlobalInfo($"Packet struct: {packet.Struct.ToString()}");
        }

        public override void Init()
        {
            base.Init();
            ReadyStateChanged += TestClient_ReadyStateChanged;
        }

        private void TestClient_ReadyStateChanged(bool arg1, bool arg2)
        {
            if(Ready && CurrentClientLocation == ClientLocation.Remote)
            {
                SyncedStream stream = new SyncedStream(this, 1, 16);
                stream.StreamOpened += () =>
                {
                    stream.Write(new byte[] { 10, 63, 44, 39 }, 0, 4);
                    stream.Write(new byte[] { 12, 65, 42, 4 }, 0, 4);
                    stream.Write(new byte[] { 1, 6, 43, 3 }, 0, 4);
                    stream.Write(new byte[] { 11, 60, 40, 30 }, 0, 4);
                    stream.Write(new byte[] { 15, 67, 0, 38 }, 0, 4);
                    stream.Write(new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, }, 0, 16);
                };
                stream.Open();
            }
        }
    }

    public enum TestResult
    {
        Result1,
        Result2,
        Result3,
    }
}
