using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem;
using SocketNetworking.UnityEngine.Components;
using SocketNetworking.UnityEngine.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using SocketNetworking.Server;
using SocketNetworking.Client;
using SocketNetworking.Shared;

namespace SocketNetworking.UnityEngine
{
    public class UnityNetworkClient : NetworkClient
    {
        private UnityMainThreadDispatcher m_Dispatcher;

        public UnityMainThreadDispatcher Dispatcher
        {
            get
            {
                return m_Dispatcher;
            }
        }
    }
}
