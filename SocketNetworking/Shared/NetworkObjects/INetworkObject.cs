﻿using System.Collections.Generic;
using SocketNetworking.Client;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.SyncVars;

namespace SocketNetworking.Shared.NetworkObjects
{
    /// <summary>
    /// The <see cref="INetworkObject"/> interface is the base for any network objects. If you want to make a client avatar, see <see cref="INetworkAvatar"/>.
    /// </summary>
    public interface INetworkObject : INetworkSpawnable
    {
        /// <summary>
        /// List of <see cref="INetworkObject"/>s which are required to be spawned before this object. Note that you can most likely cause stack overflows if you are not careful.
        /// </summary>
        List<INetworkObject> RequiredObjects { get; }

        /// <summary>
        /// Used when sorting <see cref="INetworkObject"/>s for spawning.
        /// </summary>
        int SpawnPriority { get; }

        /// <summary>
        /// The <see cref="NetworkClient.ClientID"/> of the current owner. This must not be a live property, Do NOT use the setter to update it networkside. Use <see cref="NetworkObjectExtensions.NetworkSetOwner(INetworkObject, int)"/> to set this network wide.
        /// </summary>
        int OwnerClientID { get; set; }

        /// <summary>
        /// Determines the objects ownership mode. <see cref="OwnershipMode.Client"/> = the <see cref="OwnerClientID"/> owns the object. <see cref="OwnershipMode.Server"/> = the Server owns the object. <see cref="OwnershipMode.Public"/> = Everybody owns the object. This must not be a live property, Do NOT use the setter to update it networkside. Use <see cref="NetworkObjectExtensions.NetworkSetOwnershipMode(INetworkObject, OwnershipMode)"/> to set this network wide.
        /// </summary>
        OwnershipMode OwnershipMode { get; set; }

        /// <summary>
        /// Determines if the <see cref="INetworkObject"/> can be modified (Have its <see cref="OwnershipMode"/> or <see cref="OwnerClientID"/> or <see cref="ObjectVisibilityMode"/> changed) while being marked as <see cref="OwnershipMode.Public"/>. It is recommended to keep this as false, instead, you should create a RPC call to claim the object.
        /// </summary>
        bool AllowPublicModification { get; }


        /// <summary>
        /// What should the library do if the owner of the object disconnects from the server?
        /// </summary>
        OwnershipMode FallBackIfOwnerDisconnects { get; }

        /// <summary>
        /// Determines the object visibility. This must not be a live property, Do NOT use the setter to update it networkside.
        /// </summary>
        ObjectVisibilityMode ObjectVisibilityMode { get; set; }

        /// <summary>
        /// The network ID of the object. This should be the same on the client and server. This must not be a live property, Do NOT use the setter to update it networkside.
        /// </summary>
        int NetworkID { get; set; }

        /// <summary>
        /// If this is false, the object is never updated. This includes <see cref="INetworkSyncVar"/> fields, <see cref="PacketListener>"/> and <see cref="NetworkInvokable"/> methods.
        /// </summary>
        bool Active { get; set; }

        /// <summary>
        /// All Privileged <see cref="NetworkClient"/> IDs. <b>This property is internal and does not update the network. Use <see cref="NetworkObjectExtensions.NetworkSetPrivilege(INetworkObject, IEnumerable{int})"/>.</b> Privilege allows the specified client(s) to call <see cref="NetworkInvokable"/> methods and change <see cref="INetworkSyncVar"/>s while not being the <see cref="INetworkObject.OwnerClientID"/>.
        /// </summary>
        IEnumerable<int> PrivilegedIDs { get; set; }

        /// <summary>
        /// Determines if a client has Privilege to use this object. Granting a client Privilege will allow it to act as its owner without actually being able to change the <see cref="OwnerClientID"/> or <see cref="OwnershipMode"/>, or <see cref="NetworkID"/>. Although this is synchronized to clients, client IDs are meaningless to everyone but the Server. Privilege allows the specified client(s) to call <see cref="NetworkInvokable"/> methods and change <see cref="INetworkSyncVar"/>s while not being the <see cref="INetworkObject.OwnerClientID"/>.
        /// </summary>
        /// <param name="clientId"></param>
        bool HasPrivilege(int clientId);

        /// <summary>
        /// Grants Privilege to a client to use this object. Granting a client Privilege will allow it to act as its owner without actually being able to change the <see cref="OwnerClientID"/> or <see cref="OwnershipMode"/>, or <see cref="NetworkID"/>. <b>This method is internal and does not update the network. Use <see cref="NetworkObjectExtensions.NetworkAddPrivilege(INetworkObject, int)"/>.</b> Privilege allows the specified client(s) to call <see cref="NetworkInvokable"/> methods and change <see cref="INetworkSyncVar"/>s while not being the <see cref="INetworkObject.OwnerClientID"/>.
        /// </summary>
        /// <param name="clientId"></param>
        void GrantPrivilege(int clientId);

        /// <summary>
        /// Removes Privilege from a client to use this object. Granting a client Privilege will allow it to act as its owner without actually being able to change the <see cref="OwnerClientID"/> or <see cref="OwnershipMode"/>, or <see cref="NetworkID"/>. <b>This method is internal and does not update the network. Use <see cref="NetworkObjectExtensions.NetworkRemovePrivilege(INetworkObject, int)"/>.</b> Privilege allows the specified client(s) to call <see cref="NetworkInvokable"/> methods and change <see cref="INetworkSyncVar"/>s while not being the <see cref="INetworkObject.OwnerClientID"/>.
        /// </summary>
        /// <param name="clientId"></param>
        void RemovePrivilege(int clientId);

        /// <summary>
        /// Called on the object spanwer when the peer has finished spawning it.
        /// </summary>
        void OnNetworkSpawned(NetworkClient spawner);

        /// <summary>
        /// Called on the server when the <see cref="NetworkClient"/> who owns this object has spawned it successfully.
        /// </summary>
        /// <param name="spawner"></param>
        void OnOwnerNetworkSpawned(NetworkClient spawner);

        /// <summary>
        /// Called on the client when the <see cref="NetworkClient"/> who owns this object has spawned it.
        /// </summary>
        /// <param name="spawner"></param>
        void OnOwnerLocalSpawned(NetworkClient spawner);

        /// <summary>
        /// Called when the object is being destroyed by the peer.
        /// </summary>
        void OnClientDestroy(NetworkClient client);

        /// <summary>
        /// Called when the server issues the objects destruction.
        /// </summary>
        void OnServerDestroy();

        /// <summary>
        /// This method must actually destroy the object.
        /// </summary>
        void Destroy();

        /// <summary>
        /// This is called on the server and client when the object is added to the list of currently updated network objects.
        /// </summary>
        void OnAdded(INetworkObject addedObject);

        /// <summary>
        /// Called on server and client when an object is removed.
        /// </summary>
        /// <param name="removedObject"></param>
        void OnRemoved(INetworkObject removedObject);

        /// <summary>
        /// Called when the current object is modified.
        /// </summary>
        /// <param name="modifier"></param>
        void OnModified(NetworkClient modifier);

        /// <summary>
        /// Called when an <see cref="INetworkObject"/> is modified.
        /// </summary>
        /// <param name="modifiedObject"></param>
        /// <param name="modifier"></param>
        void OnModified(INetworkObject modifiedObject, NetworkClient modifier);

        /// <summary>
        /// Called before the object modifications are applied.
        /// </summary>
        /// <param name="modification"></param>
        /// <param name="modifier"></param>
        void OnModify(ObjectManagePacket modification, NetworkClient modifier);

        /// <summary>
        /// Called on the server and client when a <see cref="INetworkObject"/> is destroyed fully, being confirmed as destroyed by the server and client.
        /// </summary>
        /// <param name="destroyedObject"></param>
        void OnDestroyed(INetworkObject destroyedObject, NetworkClient client);

        /// <summary>
        /// Called on the server and client when a <see cref="INetworkObject"/> is created fully, being confirmed as created by the server and client.
        /// </summary>
        /// <param name="createdObject"></param>
        void OnCreated(INetworkObject createdObject, NetworkClient client);

        /// <summary>
        /// Called on the Server and Client when the <see cref="NetworkClient.Ready"/> property is set to true.
        /// </summary>
        /// <param name="client"></param>
        void OnReady(NetworkClient client, bool isReady);

        /// <summary>
        /// Called on the server and client when the <see cref="NetworkClient.CurrentConnectionState"/> is set to <see cref="ConnectionState.Connected"/>
        /// </summary>
        /// <param name="client"></param>
        void OnConnected(NetworkClient client);

        /// <summary>
        /// Called on Server and Client when the <see cref="NetworkClient"/> is disconnected.
        /// </summary>
        /// <param name="client"></param>
        void OnDisconnected(NetworkClient client);

        /// <summary>
        /// Called when the <see cref="NetworkClient"/> who owns this object disconnects.
        /// </summary>
        /// <param name="client"></param>
        void OnOwnerDisconnected(NetworkClient client);

        /// <summary>
        /// Called on the server when a client has begun the sync state of the connection, if its enabled.
        /// </summary>
        /// <param name="client"></param>
        void OnSync(NetworkClient client);

        /// <summary>
        /// Called when a <see cref="INetworkSyncVar"/> is changed on this object.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="what"></param>
        void OnSyncVarChanged(NetworkClient client, INetworkSyncVar what);

        /// <summary>
        /// Called when any <see cref="INetworkSyncVar"/> is changed.
        /// </summary>
        void OnSyncVarsChanged();
    }
}
