using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared.NetworkObjects;

namespace SocketNetworking.Shared.SyncVars
{
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
            Value.NetworkSetOwnershipMode(OwnerObject.OwnershipMode);
            Value.NetworkSetVisibility(OwnerObject.ObjectVisibilityMode);
            
        }

        public void Spawn()
        {
            if(Value == null)
            {
                throw new NullReferenceException();
            }
            Value.NetworkSpawn();
        }

        public void Destroy()
        {
            if(Value == null)
            {
                throw new NullReferenceException();
            }
            Value.NetworkDestroy();
            RawSet(null, null);
        }
    }
}
