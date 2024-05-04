using SocketNetworking.UnityEngine.Components;
using SocketNetworking.UnityEngine.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SocketNetworking.UnityEngine
{
    public class UnityNetworkServer : NetworkServer
    {

        public static void NetworkDestroy(NetworkIdentity identity)
        {
            int netId = identity.NetworkID;
            NetworkObjectDestroyPacket packet = new NetworkObjectDestroyPacket();
            packet.DestroyID = identity.NetworkID;
            SendToAll(packet);
            GameObject.Destroy(identity.gameObject);
        }

        public static void NetworkDestroy(GameObject gameObject)
        {
            NetworkIdentity identity = gameObject.GetComponent<NetworkIdentity>();
            if(identity == null)
            {
                return;
            }
            NetworkDestroy(identity);
        }

        public static GameObject NetworkSpawn(NetworkPrefab prefab, bool createNew)
        {
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
            int newNetId = UnityNetworkManager.GetNextNetworkID();
            identity.SetNetworkID(newNetId);
            NetworkObjectSpawnPacket packet = new NetworkObjectSpawnPacket();
            packet.PrefabID = prefabId;
            packet.NewNetworkID = newNetId;
            SendToAll(packet);
            return clone;
        }
    }
}
