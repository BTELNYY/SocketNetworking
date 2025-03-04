using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketNetworking.Client;
using SocketNetworking.Shared.Streams;

namespace SocketNetworking.Tests
{
    [TestClass]
    public class StreamTest
    {
        
    }

    public class TestStream : NetworkSyncedStream
    {
        public TestStream(NetworkClient client, ushort id, int bufferSize) : base(client, id, bufferSize)
        {
        }
    }
}
