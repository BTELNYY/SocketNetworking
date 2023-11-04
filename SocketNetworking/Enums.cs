using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking
{
    /// <summary>
    /// Location of the client, if its currently on the Server or Local machine
    /// </summary>
    public enum ClientLocation
    {
        Local,
        Remote
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
