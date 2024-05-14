using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem.Packets;

namespace SocketNetworking.ExampleSharedData
{
    public class TestClient : NetworkClient
    {
        [NetworkInvocable(PacketDirection.Server)]
        private TestResult SomeNetworkMethod(float someFloat, int someInt)
        {
            Log.GlobalDebug($"{someFloat}, {someInt}");
            return TestResult.Result3;
        }

        public void NetworkInvokeSomeMethod(float someFloat, int someInt)
        {
            TestResult result = NetworkManager.NetworkInvoke<TestResult>(this, this, "SomeNetworkMethod", new object[] { someFloat, someInt });
            Log.GlobalDebug(result.ToString());
        }
    }

    public enum TestResult
    {
        Result1,
        Result2,
        Result3,
    }
}
