using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.TypeWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Server;
using SocketNetworking.Client;
using SocketNetworking.Shared;

namespace SocketNetworking.Tests
{
    [TestClass]
    public class SerializerTests
    {
        [TestMethod]
        public void IPEndPointTesting()
        {
            NetworkManager.ImportAssmebly(Assembly.GetExecutingAssembly()); 
            IPAddress iPAddress = IPAddress.Loopback;
            IPEndPoint endPoint = new IPEndPoint(iPAddress, 3877);
            SerializableIPEndpoinrt serializedEndpoint = new SerializableIPEndpoinrt();
            serializedEndpoint.Value = endPoint;
            byte[] serialized = serializedEndpoint.Serialize();
            IPEndPoint returned = serializedEndpoint.Deserialize(serialized).Item1;
            Assert.IsNotNull(returned);
            Assert.AreEqual(endPoint, returned);
        }
    }
}
