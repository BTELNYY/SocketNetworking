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

        public static void NetworkDestroy(NetworkIdentity identity)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            int netId = identity.NetworkID;
            NetworkObjectDestroyPacket packet = new NetworkObjectDestroyPacket();
            packet.DestroyID = identity.NetworkID;
            SendToAll(packet);
            GameObject.Destroy(identity.gameObject);
        }

        public static void NetworkDestroy(GameObject gameObject)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            NetworkIdentity identity = gameObject.GetComponent<NetworkIdentity>();
            if(identity == null)
            {
                return;
            }
            NetworkDestroy(identity);
        }

        public static GameObject NetworkSpawn(NetworkPrefab prefab, bool createNew)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            if (createNew)
            {
                return NetworkSpawn(prefab.PrefabID);
            }
            else
            {
                return NetworkSpawn(prefab.gameObject);
            }
        }

        public static GameObject NetworkSpawn(GameObject gameObject)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            NetworkPrefab prefab = gameObject.GetComponent<NetworkPrefab>();
            NetworkIdentity identity = gameObject.GetComponent<NetworkIdentity>();
            if(identity == null || prefab == null)
            {
                Log.GlobalError("In order to network spawn existing gameobjects, the gameobjects must have a NetworkPrefab and NetworkIdentity component.");
                return null;
            }
            NetworkObjectSpawnPacket packet = new NetworkObjectSpawnPacket();
            packet.PrefabID = prefab.PrefabID;
            packet.NewNetworkID = identity.NetworkID;
            SendToAll(packet);
            return gameObject;
        }

        public static GameObject NetworkSpawn(int prefabId)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            GameObject prefab = UnityNetworkManager.GetPrefabByID(prefabId);
            if(prefab == null)
            {
                Log.GlobalError("Can't find Prefab with ID: " + prefabId);
                return null;
            }
            GameObject clone = GameObject.Instantiate(prefab);
            NetworkIdentity identity = clone.GetComponent<NetworkIdentity>();
            if(identity == null)
            {
                identity = clone.AddComponent<NetworkIdentity>();
            }
            int newNetId = UnityNetworkManager.GetNextNetworkObjectID();
            identity.NetworkSpawn();
            return clone;
        }

        public static GameObject NetworkSpawn(int prefabId, UnityNetworkClient owner, OwnershipMode ownership = OwnershipMode.Server)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            GameObject prefab = UnityNetworkManager.GetPrefabByID(prefabId);
            if (prefab == null)
            {
                Log.GlobalError("Can't find Prefab with ID: " + prefabId);
                return null;
            }
            GameObject clone = GameObject.Instantiate(prefab);
            NetworkIdentity identity = clone.GetComponent<NetworkIdentity>();
            if (identity == null)
            {
                identity = clone.AddComponent<NetworkIdentity>();
            }
            int newNetId = UnityNetworkManager.GetNextNetworkObjectID();
            identity.NetworkID = newNetId;
            identity.OwnerClientID = owner == null ? -1 : owner.ClientID;
            identity.OwnershipMode = ownership;
            identity.NetworkSpawn();
            return clone;
        }

        public static GameObject NetworkSpawn(GameObject gameObject, UnityNetworkClient owner, OwnershipMode ownership = OwnershipMode.Server)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            NetworkPrefab prefab = gameObject.GetComponent<NetworkPrefab>();
            NetworkIdentity identity = gameObject.GetComponent<NetworkIdentity>();
            if (identity == null || prefab == null)
            {
                Log.GlobalError("In order to network spawn existing gameobjects, the gameobjects must have a NetworkPrefab and NetworkIdentity component.");
                return null;
            }
            identity.OwnershipMode = ownership;
            identity.OwnerClientID = owner == null ? -1 : owner.ClientID;
            identity.NetworkSpawn();
            return gameObject;
        }

        public static GameObject NetworkSpawn(NetworkPrefab prefab, bool createNew, UnityNetworkClient owner, OwnershipMode ownership = OwnershipMode.Server)
        {
            if (!Active)
            {
                throw new InvalidOperationException("Tried to call server only function when the server was not active!");
            }
            if (createNew)
            {
                return NetworkSpawn(prefab.PrefabID, owner, ownership);
            }
            else
            {
                return NetworkSpawn(prefab.gameObject, owner, ownership);
            }
        }
    }
}
