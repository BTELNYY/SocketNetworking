using SocketNetworking.Client;
using SocketNetworking.Shared.Streams;

namespace SocketNetworking.Testing.Unit
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
