using System.Collections;
using SocketNetworking.Client;
using SocketNetworking.UnityEngine.Components;
using UnityEngine;

namespace SocketNetworking.UnityEngine
{
    public class UnityNetworkClient : MixedNetworkClient
    {
        public UnityNetworkClient() : base()
        {
            UnityNetworkManager.Init();
            _clientObject = new GameObject($"{ClientID}");
            NetworkClientReference reference = _clientObject.AddComponent<NetworkClientReference>();
            reference.NetworkClient = this;
            GameObject.DontDestroyOnLoad(_clientObject);
            //ManualPacketHandle = true;
            ClientIdUpdated += UnityNetworkClient_ClientIdUpdated;
            //PacketReadyToHandle += UnityNetworkClient_PacketReadyToHandle;
        }

        private void UnityNetworkClient_PacketReadyToHandle(Shared.PacketSystem.PacketHeader arg1, byte[] arg2)
        {
            Log.Debug("Packet Ready to handle!");
            UnityNetworkManager.Dispatcher.Enqueue(() => 
            {
                Log.Debug("Next packet handle!");
                HandleNextPacket();
            });
        }

        private void UnityNetworkClient_ClientIdUpdated()
        {
            _clientObject.name = ClientID.ToString();
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
