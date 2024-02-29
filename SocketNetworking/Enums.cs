using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking
{
    /// <summary>
    /// Location of the client. Local means on the local machine ("client") and remote means on the remote machine ("server"). Unknown is used when the connection isn't active or the location can't be determined.
    /// </summary>
    public enum ClientLocation
    {
        Local,
        Remote,
        Unknown
    }

    /// <summary>
    /// An enum which represents the direction from which the Packet was sent.
    /// </summary>
    public enum PacketDirection
    {
        Client,
        Server,
        Any,
    }
}
