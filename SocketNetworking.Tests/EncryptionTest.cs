using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketNetworking.Shared;

namespace SocketNetworking.Tests
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
            Assert.IsTrue(Enumerable.SequenceEqual(old, bytes));
            NetworkEncryption manager = new NetworkEncryption(null);
            byte[] encrypted = manager.Encrypt(bytes);
            Assert.IsTrue(Enumerable.SequenceEqual(old, manager.Decrypt(encrypted)));
        }

        [TestMethod]
        public void TestAsym()
        {
            Random random = new Random();
            byte[] bytes = new byte[128];
            random.NextBytes(bytes);
            byte[] old = new byte[bytes.Length];
            bytes.CopyTo(old, 0);
            Assert.IsTrue(Enumerable.SequenceEqual(old, bytes));
            NetworkEncryption manager = new NetworkEncryption(null);
            byte[] encrypted = manager.Encrypt(bytes, false, true);
            byte[] returned = manager.Decrypt(encrypted, false);
            Assert.IsTrue(Enumerable.SequenceEqual(old, returned));
        }
    }
}