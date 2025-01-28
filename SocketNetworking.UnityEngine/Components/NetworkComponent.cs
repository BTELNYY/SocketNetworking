using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        void Awake()
        {
            _identity = GetComponent<NetworkIdentity>();
            if(Identity == null)
            {
                throw new InvalidOperationException("All Network Objects must have a NetowrkIdentity.");
            }
        }
    }
}
