using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketNetworking.Tests.Unit
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
