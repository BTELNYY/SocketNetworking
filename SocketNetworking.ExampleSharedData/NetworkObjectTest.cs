using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared;

namespace SocketNetworking.ExampleSharedData
{
    public class NetworkObjectTest : INetworkObject
    {
        public int OwnerClientID { get; set; }
        public OwnershipMode OwnershipMode { get; set; }

        public bool AllowPublicModification => false;

        public OwnershipMode FallBackIfOwnerDisconnects => OwnershipMode.Server;

        public ObjectVisibilityMode ObjectVisibilityMode { get; set; }
        public int NetworkID { get; set; }
        public bool Active { get; set; }

        public bool Spawnable => true;

        public void Destroy()
        {
            
        }

        public void OnAdded(INetworkObject addedObject)
        {
            
        }

        public void OnClientDestroy(NetworkClient client)
        {
            
        }

        public void OnConnected(NetworkClient client)
        {
            
        }

        public void OnCreated(INetworkObject createdObject, NetworkClient client)
        {
            
        }

        public void OnDestroyed(INetworkObject destroyedObject, NetworkClient client)
        {
            
        }

        public void OnDisconnected(NetworkClient client)
        {
            
        }

        public void OnLocalSpawned(ObjectManagePacket packet)
        {
            
        }

        public void OnModified(NetworkClient modifier)
        {
            
        }

        public void OnModified(INetworkObject modifiedObject, NetworkClient modifier)
        {
            
        }

        public void OnModify(ObjectManagePacket modification, NetworkClient modifier)
        {
            
        }

        public void OnNetworkSpawned(NetworkClient spawner)
        {
            
        }

        public void OnReady(NetworkClient client, bool isReady)
        {
            
        }

        public void OnRemoved(INetworkObject removedObject)
        {
            
        }

        public void OnServerDestroy()
        {
            
        }

        public void OnSync(NetworkClient client)
        {
            
        }

        public ByteReader RecieveExtraData(byte[] extraData)
        {
            return new ByteReader(extraData);
        }

        public ByteWriter SendExtraData()
        {
            return new ByteWriter();
        }
    }
}
