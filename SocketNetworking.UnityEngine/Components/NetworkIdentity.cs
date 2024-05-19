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

        private readonly List<NetworkObject> NetworkObjects = new List<NetworkObject>();

        private static readonly Dictionary<Type, uint> ObjectLimits = new Dictionary<Type, uint>() 
        {
            [typeof(NetworkTransform)] = 1,
            [typeof(NetworkAnimator)] = 1,
            [typeof(NetworkPrefab)] = 1,
        };

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
            if (obj == null) throw new ArgumentNullException("obj cannot be null!", new NullReferenceException("obj is not set to an instance of an object"));
            if (NetworkObjects.Contains(obj)) throw new ArgumentException("Object is already registered", "obj");
            if (ObjectLimits.Keys.Any(x => obj.GetType().IsSubclassOf(x)))
            {
                Type limitingType = ObjectLimits.Keys.Where(x => obj.GetType().IsSubclassOf(x)).FirstOrDefault();
                if(limitingType != default)
                {
                    List<NetworkObject> registered = NetworkObjects.Where(x => x.GetType().IsSubclassOf(limitingType)).ToList();
                    if(registered.Count >= ObjectLimits[limitingType])
                    {
                        throw new ArgumentException("Cannot register any more of that object type to this identity!", "obj");
                    }
                }
            }
            NetworkObjects.Add(obj);
        }

        public void UnregisterObject(NetworkObject obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (!NetworkObjects.Contains(obj)) throw new ArgumentException("obj");
            NetworkObjects.Remove(obj);
        }

        public bool IsRegistered(NetworkObject obj)
        {
            return NetworkObjects.Contains(obj);
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
