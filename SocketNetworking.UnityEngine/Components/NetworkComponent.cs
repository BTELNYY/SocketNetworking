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
        public NetworkIdentity Identity
        {
            get
            {
                if (_identity == null)
                {
                    EnsureIdentityExists();
                }
                return _identity;
            }
        }

        private NetworkIdentity _identity = null;

        public void OverrideIdentity(NetworkIdentity identity)
        {
            _identity?.UnregisterComponent(this);
            _identity = identity;
            identity.RegisterComponent(this);
        }

        public override int NetworkID { get => Identity.NetworkID; set => Identity.NetworkID = value; }

        public override int OwnerClientID { get => Identity.OwnerClientID; set => Identity.OwnerClientID = value; }

        public override bool Active { get => Identity.Active; set => Identity.Active = value; }

        public override ObjectVisibilityMode ObjectVisibilityMode { get => Identity.ObjectVisibilityMode; set => Identity.ObjectVisibilityMode = value; }

        public override OwnershipMode OwnershipMode { get => Identity.OwnershipMode; set => Identity.OwnershipMode = value; }

        public byte[] ComponentData = new byte[0];

        public virtual void OnSetupComponent()
        {

        }

        public virtual ByteWriter SendComponentData()
        {
            return new ByteWriter(ComponentData);
        }

        public virtual ByteReader ReceiveComponentData(byte[] data)
        {
            ComponentData = data;
            return new ByteReader(data);
        }

        public void EnsureIdentityExists()
        {
            _identity = GetComponent<NetworkIdentity>();
            if (Identity == null)
            {
                throw new InvalidOperationException("All Network Objects must have a NetworkIdentity.");
            }
            OverrideIdentity(_identity);
        }

        public override void Destroy()
        {
            Destroy(this);
        }

        void Awake()
        {
            EnsureIdentityExists();
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
