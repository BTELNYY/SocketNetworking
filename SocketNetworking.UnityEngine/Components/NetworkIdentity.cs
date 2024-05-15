using SocketNetworking;
using SocketNetworking.Attributes;
using SocketNetworking.UnityEngine.Components;
using SocketNetworking.UnityEngine.Packets;
using SocketNetworking.UnityEngine.Packets.NetworkTransform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkIdentity : NetworkObject
    {
        void Awake()
        {
            UnityNetworkManager.Register(this);
        }

        void OnDestroy()
        {
            UnityNetworkManager.Unregister(this);
        }

        private List<NetworkObject> NetworkObjects = new List<NetworkObject>();

        public void SyncOwnershipMode()
        {
            foreach (NetworkObject obj in NetworkObjects)
            {
                obj.UpdateOwnershipMode(OwnershipMode);
            }
        }

        public void SyncOwnerID()
        {
            foreach(NetworkObject obj in NetworkObjects)
            {
                obj.UpdateOwnerClientId(OwnerClientID);
            }
        }

        public int RegisteredObjectCount
        {
            get
            {
                return NetworkObjects.Count;
            }
        }
        
        public void RegisterObject(NetworkObject obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (NetworkObjects.Contains(obj)) throw new ArgumentException("obj");
            NetworkObjects.Add(obj);
        }

        public void UnregisterObject(NetworkObject obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (!NetworkObjects.Contains(obj)) throw new ArgumentException("obj");
            NetworkObjects.Remove(obj);
        }

        public override void OnObjectUpdateNetworkIDLocal(int newNetID)
        {
            base.OnObjectUpdateNetworkIDLocal(newNetID);
            foreach (var obj in NetworkObjects)
            {
                if (obj == null)
                {
                    continue;
                }
                obj.SetNetworkID(newNetID);
            }
        }

        public bool NetworkEnabled
        {
            get
            {
                return gameObject.activeSelf;
            }
            set
            {
                if (NetworkManager.WhereAmI != SyncOwner)
                {
                    Logger.Warning($"Tried to set property of {gameObject.name} from illegal client side.");
                    return;
                }
                NetworkObjectEnabledStatusPacket status = new NetworkObjectEnabledStatusPacket();
                status.IsEnabled = value;
                SendPacket(status);
                gameObject.SetActive(value);
            }
        }

        [PacketListener(typeof(NetworkObjectEnabledStatusPacket), PacketDirection.Server)]
        private void OnNetworkEnabledStateUpdate(NetworkObjectEnabledStatusPacket packet, NetworkClient client)
        {
            if (client.CurrnetClientLocation == SyncOwner && DoStrictMode)
            {
                return;
            }
            gameObject.SetActive(packet.IsEnabled);
        }
    }
}
