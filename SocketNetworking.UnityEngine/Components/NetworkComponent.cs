using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkComponent : NetworkObject
    {
        public override bool Spawnable => false;

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
