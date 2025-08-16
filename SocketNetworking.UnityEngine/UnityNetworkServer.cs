using System;
using SocketNetworking.Server;
using SocketNetworking.UnityEngine.Components;

namespace SocketNetworking.UnityEngine
{
    public class UnityNetworkServer : MixedNetworkServer
    {
        public override void StopServer()
        {
            base.StopServer();
            UnityNetworkManager.Init();
        }

        public override void StartServer()
        {
            base.StartServer();
            UnityNetworkManager.Init();
        }

        public static void NetworkDestroy(NetworkBehavior identity)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            identity.NetworkDestroy();
        }

        public static void NetworkSpawn(NetworkBehavior identity)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            if(!identity.Spawnable)
            {
                return;
            }
            identity.NetworkSpawn();
        }
    }
}
