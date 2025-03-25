using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketNetworking.Tests.Unit
{
    [TestClass]
    public class CompressionTest
    {
        [TestMethod]
        public void TestCompression()
        {
            Random random = new Random();
            byte[] bytes = new byte[1024];
            random.NextBytes(bytes);
            byte[] old = new byte[1024];
            Buffer.BlockCopy(bytes, 0, old, 0, bytes.Length);
            bytes = bytes.Compress();
            byte[] newBytes = bytes.Decompress();
            Assert.IsTrue(Enumerable.SequenceEqual(newBytes, old));
        }
    }
}
