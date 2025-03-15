using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketNetworking.Shared.PacketSystem.Packets;
using System;
using System.Linq;

namespace SocketNetworking.Tests.Unit
{
    [TestClass]
    public class PacketTest
    {
        [TestMethod]
        public void EncryptionPacketTest()
        {
            EncryptionPacket packet = new EncryptionPacket();
            packet.EncryptionFunction = EncryptionFunction.AsymmetricalKeySend;
            byte[] complete = packet.Serialize().Data;
            complete = BitConverter.GetBytes(complete.Length).Concat(complete).ToArray();
            EncryptionPacket returned = new EncryptionPacket();
            returned.Deserialize(complete);
            byte[] returnedReSerial = returned.Serialize().Data;
            returnedReSerial = BitConverter.GetBytes(returnedReSerial.Length).Concat(returnedReSerial).ToArray();
            Assert.AreEqual(complete.Length, returnedReSerial.Length);
            for (int i = 0; i < returnedReSerial.Length; i++)
            {
                byte comp = complete[i];
                byte ret = returnedReSerial[i];
                if (comp != ret)
                {
                    Assert.Fail($"Byte Mismatch: Index: {i}, Expected: {complete[i]}, Got: {returnedReSerial[i]}");
                }
                else
                {
                    //huh
                }
            }
            return;
        }
    }
}
