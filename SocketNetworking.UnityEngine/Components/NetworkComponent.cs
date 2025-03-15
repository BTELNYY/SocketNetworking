using SocketNetworking.Shared;
using System;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkComponent : NetworkBehavior
    {
        /// <summary>
        /// Use <see cref="NetworkIdentity"/> to spawn objects from prefabs.
        /// </summary>
        public override bool Spawnable => false;

        /// <summary>
        /// The <see cref="NetworkIdentity"/> of the current <see cref="GameObject"/>.
        /// </summary>
        public NetworkIdentity Identity => _identity;

        private NetworkIdentity _identity = null;

        public override int NetworkID { get => _identity.NetworkID; set => _identity.NetworkID = value; }

        public override int OwnerClientID { get => _identity.OwnerClientID; set => _identity.OwnerClientID = value; }

        public override bool Active { get => _identity.Active; set => _identity.Active = value; }

        public override ObjectVisibilityMode ObjectVisibilityMode { get => _identity.ObjectVisibilityMode; set => _identity.ObjectVisibilityMode = value; }

        public override OwnershipMode OwnershipMode { get => _identity.OwnershipMode; set => _identity.OwnershipMode = value; }

        void Awake()
        {
            _identity = GetComponent<NetworkIdentity>();
            if (Identity == null)
            {
                throw new InvalidOperationException("All Network Objects must have a NetowrkIdentity.");
            }
        }
    }
}
