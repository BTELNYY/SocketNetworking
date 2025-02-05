﻿using SocketNetworking.Client;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.PacketSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Shared
{
    public class NetworkObjectBase : INetworkObject
    {
        public virtual int OwnerClientID { get; set; }

        public virtual OwnershipMode OwnershipMode { get; set; }

        public virtual bool AllowPublicModification => false;

        public virtual OwnershipMode FallBackIfOwnerDisconnects => OwnershipMode.Server;

        public virtual ObjectVisibilityMode ObjectVisibilityMode { get; set; }
        public virtual int NetworkID { get; set; }
        public virtual bool Active { get; set; }

        public virtual bool Spawnable => true;

        public virtual void Destroy()
        {

        }

        public virtual void OnAdded(INetworkObject addedObject)
        {

        }

        public virtual void OnClientDestroy(NetworkClient client)
        {

        }

        public virtual void OnConnected(NetworkClient client)
        {

        }

        public virtual void OnCreated(INetworkObject createdObject, NetworkClient client)
        {

        }

        public virtual void OnDestroyed(INetworkObject destroyedObject, NetworkClient client)
        {

        }

        public virtual void OnDisconnected(NetworkClient client)
        {

        }

        public virtual void OnLocalSpawned(ObjectManagePacket packet)
        {

        }

        public virtual void OnModified(NetworkClient modifier)
        {

        }

        public virtual void OnModified(INetworkObject modifiedObject, NetworkClient modifier)
        {

        }

        public virtual void OnModify(ObjectManagePacket modification, NetworkClient modifier)
        {

        }

        public virtual void OnNetworkSpawned(NetworkClient spawner)
        {
            
        }

        public virtual void OnReady(NetworkClient client, bool isReady)
        {

        }

        public virtual void OnRemoved(INetworkObject removedObject)
        {

        }

        public virtual void OnServerDestroy()
        {

        }

        public virtual void OnSync(NetworkClient client)
        {

        }

        public virtual ByteReader RecieveExtraData(byte[] extraData)
        {
            return new ByteReader(extraData);
        }

        public virtual ByteWriter SendExtraData()
        {
            return new ByteWriter();
        }
    }
}
