using SocketNetworking.Attributes;
using SocketNetworking.UnityEngine.Components;
using SocketNetworking.UnityEngine.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace SocketNetworking.UnityEngine
{
    public class UnityNetworkClient : NetworkClient
    {
        [PacketListener(typeof(NetworkObjectDestroyPacket), PacketDirection.Server)]
        public void OnServerDestroyObject(NetworkObjectDestroyPacket packet, UnityNetworkClient client)
        {
            NetworkIdentity identity = UnityNetworkManager.GetNetworkIdentity(packet.NetowrkIDTarget);
            if(identity != null)
            {
                GameObject.Destroy(identity.gameObject);
            }
        }

        [PacketListener(typeof(NetworkObjectSpawnPacket), PacketDirection.Server)]
        public void OnServerSpawnObject(NetworkObjectSpawnPacket packet, UnityNetworkClient client)
        {
            GameObject prefab = UnityNetworkManager.GetPrefabByID(packet.PrefabID);
            if(prefab == null)
            {
                Log.GlobalError("Cannot find prefab by ID: " + packet.PrefabID);
                return;
            }
            GameObject clone = GameObject.Instantiate(prefab);
            NetworkIdentity networkIdentity = clone.GetComponent<NetworkIdentity>();
            if(networkIdentity == null)
            {
                networkIdentity = clone.AddComponent<NetworkIdentity>();
            }
            networkIdentity.SetNetworkID(packet.NewNetworkID);
            NetworkObjectSpawnedPacket returnPacket = new NetworkObjectSpawnedPacket();
            returnPacket.SpawnedPrefabID = packet.PrefabID;
            returnPacket.SpawnedNetworkID = packet.NewNetworkID;
            Send(returnPacket);
        }

        [PacketListener(typeof(NetworkObjectSpawnedPacket), PacketDirection.Client)]
        public void OnClientSpawnedObject(NetworkObjectSpawnedPacket packet, UnityNetworkClient client)
        {
            foreach(NetworkBehavior behavior in UnityNetworkManager.GetNetworkBehaviors().Where(x => x.NetworkID == packet.SpawnedNetworkID))
            {
                behavior.OnClientObjectCreated(client);
            }
        }
    }
}
