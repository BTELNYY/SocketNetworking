using System;
using System.Collections.Generic;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Components
{
    /// <summary>
    /// Essentially an additional script you can place on an object. Note: You must <b>Always</b> create and register these on init or you will have, interesting issues. See <see cref="NetworkIdentity.ReceiveExtraData(byte[])"/> for more on why you should adhere to my warnings.
    /// </summary>
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

        public void OverrideIdentity(NetworkIdentity identity)
        {
            if(_identity != null)
            {
                _identity.UnregisterComponent(this);
            }
            _identity = identity;
            identity.RegisterComponent(this);
        }

        public override int NetworkID { get => _identity.NetworkID; set => _identity.NetworkID = value; }

        public override int OwnerClientID { get => _identity.OwnerClientID; set => _identity.OwnerClientID = value; }

        public override bool Active { get => _identity.Active; set => _identity.Active = value; }

        public override ObjectVisibilityMode ObjectVisibilityMode { get => _identity.ObjectVisibilityMode; set => _identity.ObjectVisibilityMode = value; }

        public override OwnershipMode OwnershipMode { get => _identity.OwnershipMode; set => _identity.OwnershipMode = value; }

        public byte[] ComponentData = new byte[0];

        public virtual ByteWriter SendComponentData()
        {
            return new ByteWriter(ComponentData);
        }

        public virtual ByteReader ReceiveComponentData(byte[] data)
        {
            ComponentData = data;
            return new ByteReader(data);
        }

        void Awake()
        {
            _identity = GetComponent<NetworkIdentity>();
            if (Identity == null)
            {
                throw new InvalidOperationException("All Network Objects must have a NetworkIdentity.");
            }
            OverrideIdentity(_identity);
        }

        public override IEnumerable<int> PrivilegedIDs
        {
            get
            {
                return Identity.PrivilegedIDs;
            }
            set
            {
                Identity.PrivilegedIDs = value;
            }
        }

        public override bool HasPrivilege(int clientId)
        {
            return Identity.HasPrivilege(clientId);
        }

        public override void GrantPrivilege(int clientId)
        {
            Identity.GrantPrivilege(clientId);
        }

        public override void RemovePrivilege(int clientId)
        {
            Identity.RemovePrivilege(clientId);
        }
    }
}
