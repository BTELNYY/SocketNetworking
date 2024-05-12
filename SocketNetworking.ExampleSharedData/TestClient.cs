using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;

namespace SocketNetworking.ExampleSharedData
{
    public class TestClient : NetworkClient
    {
        [NetworkInvocable]
        private void SomeNetworkMethod(float someFloat, int someInt)
        {
            Log.GlobalDebug($"{someFloat}, {someInt}");
        }

        public void NetworkInvokeSomeMethod(float someFloat, int someInt)
        {
            NetworkManager.NetworkInvoke(this, this, "SomeNetworkMethod", new object[] { someFloat, someInt });
        }
    }
}
