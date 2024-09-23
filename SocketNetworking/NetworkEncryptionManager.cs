using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SocketNetworking
{
    public class NetworkEncryptionManager
    {
        public RSACryptoServiceProvider MyRSA {  get; set; } 

        public RSACryptoServiceProvider OthersRSA { get; set; }

        public Aes SharedAes { get; set; }

        /// <summary>
        /// Key Then IV
        /// </summary>
        public Tuple<byte[], byte[]> SharedAesKey
        {
            get
            {
                return new Tuple<byte[], byte[]>(SharedAes.Key, SharedAes.IV);
            }
            set
            {
                SharedAes.Key = value.Item1;
                SharedAes.IV = value.Item2;
            }
        }

        public string OthersPublicKey
        {
            set
            {
                OthersRSA.FromXmlString(value);
            }
        }

        public string MyPublicKey
        {
            get
            {
                return MyRSA.ToXmlString(false);
            }
        }

        public NetworkEncryptionManager()
        {
            SharedAes = new AesCryptoServiceProvider();
            MyRSA = new RSACryptoServiceProvider();
            OthersRSA = new RSACryptoServiceProvider();
        }

        /// <summary>
        /// Encrypts data using <see cref="OthersPublicKey"/> by default.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="useSymmetry"></param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] data, bool useSymmetry = true, bool useMyKey = false)
        {
            if (useSymmetry)
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(stream, SharedAes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0 , data.Length);
                        byte[] output = new byte[cryptoStream.Length];
                        cryptoStream.Read(output, 0, output.Length);
                        return output;
                    };
                };
            }
            else
            {
                if (useMyKey)
                {
                    return MyRSA.Encrypt(data, RSAEncryptionPadding.Pkcs1);
                }
                return OthersRSA.Encrypt(data, RSAEncryptionPadding.Pkcs1);
            }
        }

        public byte[] Decrypt(byte[] data, bool useSymmetry = true)
        {
            if (useSymmetry)
            {
                byte[] outputBytes = data;
                using (MemoryStream memoryStream = new MemoryStream(outputBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, SharedAes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        cryptoStream.Read(outputBytes, 0, outputBytes.Length);
                    }
                }
                return outputBytes;
            }
            else
            {
                return MyRSA.Decrypt(data, RSAEncryptionPadding.Pkcs1);
            }
        }
    }
}
