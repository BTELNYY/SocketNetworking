using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Attributes
{
    public class NetworkInvocable : Attribute
    {
        /// <summary>
        /// From where are you accepting packets from. Any = Anywhere, Client = Server-Bound packets only, Server = Client-Bound packets only
        /// </summary>
        public PacketDirection Direction { get; set; } = PacketDirection.Any;

        public NetworkInvocable() { }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkInvocable"/> attribute and assigns a <see cref="PacketDirection"/> value to <see cref="Direction"/>. The default value is <see cref="PacketDirection.Any"/>
        /// </summary>
        /// <param name="direction"></param>
        public NetworkInvocable(PacketDirection direction)
        {
            Direction = direction;
        }
    }
}
