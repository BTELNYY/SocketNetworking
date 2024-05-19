using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkComponent : NetworkObject
    {
        /// <summary>
        /// Gets the network ID of the object. if a <see cref="NetworkIdentity"/> (or any subclass of it) is present, returns its NetworkID.
        /// </summary>
        public sealed override int NetworkID
        {
            get
            {
                if (Identity == null)
                {
                    return base.NetworkID;
                }
                else
                {
                    return Identity.NetworkID;
                }
            }
        }

        private NetworkIdentity _identity;

        /// <summary>
        /// All objects should have a <see cref="NetworkIdentity"/> attached to them or referenced somehow. This is not a hard coded requirement, but is suggested for larger systems (e.g. the player uses this to prevent having to make 80 NetIDs for one thing)
        /// </summary>
        public NetworkIdentity Identity
        {
            get
            {
                return _identity;
            }
            set
            {
                if (value == null)
                {
                    Logger.Error("Can't set null NetworkIdentity!");
                    return;
                }
                if (_identity != value)
                {
                    if (_identity != null)
                    {
                        _identity.UnregisterObject(this);
                    }
                    _identity = value;
                    _identity.RegisterObject(this);
                }
                else
                {
                    _identity = value;
                }
                SetNetworkID(value.NetworkID);
            }
        }

        void Awake()
        {
            NetworkIdentity identity = GetComponent<NetworkIdentity>();
            if (Identity == null)
            {
                if (identity != null)
                {
                    Identity = identity;
                }
                else
                {
                    Logger.Warning("Can't find Identity attached to this object!");
                    SetNetworkID(-1);
                    return;
                }
            }
        }
    }
}
