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
using System.Security.Policy;
using System.Collections;

namespace SocketNetworking.UnityEngine
{
    public class UnityNetworkClient : MixedNetworkClient
    {
        public UnityNetworkClient()
        {
            UnityNetworkManager.Init();
            _clientObject = new GameObject($"{ClientID}");
            NetworkClientReference reference = _clientObject.AddComponent<NetworkClientReference>();
            reference.NetworkClient = this;
            GameObject.DontDestroyOnLoad(_clientObject);
            UnityNetworkManager.Dispatcher.Enqueue(PacketHandle());
            ManualPacketHandle = true;
            ClientIdUpdated += UnityNetworkClient_ClientIdUpdated;
        }

        private void UnityNetworkClient_ClientIdUpdated()
        {
            _clientObject.name = ClientID.ToString();
        }

        IEnumerator PacketHandle()
        {
            HandleNextPacket();
            UnityNetworkManager.Dispatcher.Enqueue(PacketHandle());
            yield return null;
        }


        private GameObject _clientObject;

        public GameObject ClientObject
        {
            get
            {
                return _clientObject;
            }
        }
    }
}
