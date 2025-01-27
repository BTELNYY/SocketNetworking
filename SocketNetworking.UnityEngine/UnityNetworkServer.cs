using SocketNetworking.PacketSystem;
using SocketNetworking.UnityEngine.Components;
using SocketNetworking.UnityEngine.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SocketNetworking.Server;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using System.Data.SqlTypes;

namespace SocketNetworking.UnityEngine
{
    public class UnityNetworkServer : NetworkServer
    {

        public static void NetworkDestroy(NetworkObject identity)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            identity.NetworkDestroy();
        }

        public static void NetworkSpawn(NetworkObject identity)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            identity.NetworkSpawn();
        }
    }
}
