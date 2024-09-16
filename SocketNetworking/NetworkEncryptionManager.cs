using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking
{
    public class NetworkEncryptionManager
    {
        public RSA AsymetricalEncryption {  get; set; } 

        public Aes SymetricalEncryption { get; set; }

        public Tuple<byte[], byte[]> ModulusAndExponent
        {
            get
            {
                RSAParameters parameters = AsymetricalEncryption.ExportParameters(false);
                return new Tuple<byte[], byte[]>(parameters.Modulus, parameters.Exponent);
            }
            set
            {
                RSAParameters encParams = AsymetricalEncryption.ExportParameters(true);
                encParams.Modulus = value.Item1;
                encParams.Exponent = value.Item2;
                AsymetricalEncryption.ImportParameters(encParams);
            }
        }

        public Tuple<byte[], byte[]> IVAndKey
        {
            get
            {
                return new Tuple<byte[], byte[]>(SymetricalEncryption.IV, SymetricalEncryption.Key);
            }
            set
            {
                SymetricalEncryption.IV = value.Item1;
                SymetricalEncryption.Key = value.Item2;
            }
        }

        public NetworkEncryptionManager()
        {
            SymetricalEncryption = Aes.Create();
            AsymetricalEncryption = RSA.Create();
        }

        public byte[] Encrypt(byte[] data, bool useSymmetry = true)
        {
            if (useSymmetry)
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(stream, SymetricalEncryption.CreateEncryptor(), CryptoStreamMode.Write))
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
                return AsymetricalEncryption.Encrypt(data, RSAEncryptionPadding.Pkcs1);
            }
        }

        public byte[] Decrypt(byte[] data, bool useSymmetry = true)
        {
            if (useSymmetry)
            {
                byte[] outputBytes = data;
                using (MemoryStream memoryStream = new MemoryStream(outputBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, SymetricalEncryption.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        cryptoStream.Read(outputBytes, 0, outputBytes.Length);
                    }
                }
                return outputBytes;
            }
            else
            {
                return AsymetricalEncryption.Decrypt(data, RSAEncryptionPadding.Pkcs1);
            }
        }
    }
}
