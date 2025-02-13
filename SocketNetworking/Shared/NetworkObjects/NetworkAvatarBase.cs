using SocketNetworking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Shared.NetworkObjects
{
    public class NetworkAvatarBase : NetworkObjectBase
    {
        public override void OnDisconnected(NetworkClient client)
        {
            base.OnDisconnected(client);
            if(client.ClientID == OwnerClientID)
            {
                this.NetworkDestroy();
            }
        }
    }
}
