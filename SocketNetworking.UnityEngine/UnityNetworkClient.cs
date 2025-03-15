using SocketNetworking.Client;
using SocketNetworking.UnityEngine.Components;
using System.Collections;
using UnityEngine;

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
