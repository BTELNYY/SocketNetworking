using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Client;
using SocketNetworking.Shared;

namespace SocketNetworking.ExampleSharedData
{
    public class TestClient : MixedNetworkClient
    {
        [NetworkInvocable(PacketDirection.Server)]
        private TestResult SomeNetworkMethod(float someFloat, int someInt, ValueTuple<int, int> values)
        {
            Log.GlobalDebug($"{someFloat}, {someInt}, {values.Item1 + values.Item2}");
            return TestResult.Result2;
        }

        public void NetworkInvokeSomeMethod(float someFloat, int someInt)
        {
            TestResult result = NetworkInvoke<TestResult>(this, "SomeNetworkMethod", new object[] { someFloat, someInt, new ValueTuple<int, int> (1, 3) });
            Log.GlobalDebug(result.ToString());
            ExampleCustomPacket exampleCustom = new ExampleCustomPacket();
            exampleCustom.Data = "rah";
            exampleCustom.Flags = exampleCustom.Flags.SetFlag(PacketFlags.Priority, true);
        }
    }

    public enum TestResult
    {
        Result1,
        Result2,
        Result3,
    }
}
