﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
