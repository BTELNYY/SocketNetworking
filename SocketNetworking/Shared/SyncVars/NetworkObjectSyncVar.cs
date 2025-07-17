using System;
using SocketNetworking.Shared.NetworkObjects;

namespace SocketNetworking.Shared.SyncVars
{
    /// <summary>
    /// The <see cref="NetworkObjectSyncVar"/> class is designed to sync <see cref="INetworkObject"/>s as SyncVars.
    /// </summary>
    public class NetworkObjectSyncVar : NetworkSyncVar<INetworkObject>
    {
        public NetworkObjectSyncVar(INetworkObject value) : base(value)
        {
        }

        public NetworkObjectSyncVar(INetworkObject ownerObject, INetworkObject value) : base(ownerObject, value)
        {
        }

        public NetworkObjectSyncVar(INetworkObject ownerObject, OwnershipMode syncOwner) : base(ownerObject, syncOwner)
        {
        }

        public NetworkObjectSyncVar(INetworkObject ownerObject, OwnershipMode syncOwner, INetworkObject value) : base(ownerObject, syncOwner, value)
        {
        }



        public void SyncToParent()
        {
            if (Value == null)
            {
                throw new NullReferenceException();
            }
            Value.NetworkSetOwner(OwnerObject.OwnerClientID);
            Value.NetworkSetPrivilege(OwnerObject.PrivilegedIDs);
            Value.NetworkSetOwnershipMode(OwnerObject.OwnershipMode);
            Value.NetworkSetVisibility(OwnerObject.ObjectVisibilityMode);
        }

        public void Spawn()
        {
            if (Value == null)
            {
                throw new NullReferenceException();
            }
            Value.NetworkSpawn();
        }

        public void Destroy()
        {
            if (Value == null)
            {
                throw new NullReferenceException();
            }
            Value.NetworkDestroy();
            RawSet(null, null);
        }
    }
}
