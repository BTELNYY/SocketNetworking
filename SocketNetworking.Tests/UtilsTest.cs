using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketNetworking.Tests
{
    [TestClass]
    public class UtilsTest
    {
        [TestMethod]
        public void TestShift()
        {
            byte[] array = { 1, 2, 3, 4, 5, 6 };
            byte[] shiftIn = { 7, 8 };
            byte[] test = array.Push(shiftIn);
            byte[] result = { 7, 8, 1, 2, 3, 4 };
            Assert.IsTrue(Enumerable.SequenceEqual(test, result));
        }
    }
}
