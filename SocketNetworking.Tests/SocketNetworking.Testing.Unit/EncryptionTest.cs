using SocketNetworking.Shared;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Testing.Unit
{
    [TestClass]
    public class EncryptionTest
    {
        [TestMethod]
        public void TestSym()
        {
            Random random = new Random();
            byte[] bytes = new byte[1024];
            random.NextBytes(bytes);
            byte[] old = new byte[bytes.Length];
            bytes.CopyTo(old, 0);
            Assert.IsTrue(old.SequenceEqual(bytes));
            NetworkEncryption manager = new NetworkEncryption(null);
            byte[] encrypted = manager.Encrypt(bytes);
            Assert.IsTrue(old.SequenceEqual(manager.Decrypt(encrypted)));
        }

        [TestMethod]
        public void TestAsym()
        {
            Random random = new Random();
            byte[] bytes = new byte[64];
            random.NextBytes(bytes);
            byte[] old = new byte[bytes.Length];
            bytes.CopyTo(old, 0);
            Assert.IsTrue(old.SequenceEqual(bytes));
            NetworkEncryption manager = new NetworkEncryption(null);
            byte[] encrypted = manager.Encrypt(bytes, false, true);
            byte[] returned = manager.Decrypt(encrypted, false);
            Assert.IsTrue(old.SequenceEqual(returned));
        }

        [TestMethod]
        public void TestInvoke()
        {
            NetworkInvocationPacket packet = new NetworkInvocationPacket()
            {
                TargetType = GetType(),
                MethodName = nameof(NetworkInvocationPacket),
                CallbackID = 0,
                Arguments = new List<SerializedData>()
                {
                    ByteConvert.Serialize(1f),
                    ByteConvert.Serialize(100u),
                    ByteConvert.Serialize("dddd"),
                    ByteConvert.Serialize(""),
                    ByteConvert.Serialize(1L),
                },
                Destination = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("172.0.0.1"), 233),
                Source = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("172.0.0.1"), 233),
                NetworkIDTarget = 333333,
                SendTime = DateTime.Now.Ticks,
                IgnoreResult = true,
            };
            byte[] old = packet.Serialize().Data;
            NetworkEncryption manager = new NetworkEncryption(null);
            byte[] encrypted = manager.Encrypt(old);
            Assert.IsTrue(old.SequenceEqual(manager.Decrypt(encrypted)));
        }
    }
}