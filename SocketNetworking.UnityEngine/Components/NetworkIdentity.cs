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
using SocketNetworking.Server;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.PacketSystem.Packets;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkIdentity : NetworkObject
    {
        public sealed override int NetworkID
        {
            get
            {
                return base.NetworkID;
            }
            set
            {
                base.NetworkID = value;
            }
        }

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
                obj.OwnershipMode = OwnershipMode;
            }
        }

        public void SyncOwnerID()
        {
            foreach(NetworkObject obj in NetworkObjects)
            {
                obj.OwnerClientID = OwnerClientID;
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
            if(NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                obj.NetworkSetID(NetworkID);
            }
            if(NetworkManager.WhereAmI == ClientLocation.Local)
            {
                //obj.ClientSetNetworkID(NetworkID);
            }
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

        public override void OnModify(ObjectManagePacket modifier, NetworkClient client)
        {
            base.OnModify(modifier, client);
            if(modifier.NewNetworkID != NetworkID)
            {
                foreach (var obj in NetworkObjects)
                {
                    if (obj == null)
                    {
                        continue;
                    }
                    obj.NetworkID = NetworkID;
                }
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
                if (!IsOwner)
                {
                    Logger.Warning($"Tried to set property of {gameObject.name} from illegal client side.");
                    return;
                }
                NetworkInvoke(nameof(GetNetworkStatus), new object[] { value });
                gameObject.SetActive(value);
            }
        }

        [NetworkInvocable]
        private void GetNetworkStatus(NetworkClient client, bool enabled)
        {
            gameObject.SetActive(enabled);
        }
    }
}
