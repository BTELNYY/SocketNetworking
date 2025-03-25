using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using SocketNetworking.Client;

namespace SocketNetworking.Shared
{
    public class NetworkEncryption
    {
        object _lock = new object();

        public NetworkClient Client { get; }

        public const int KEY_SIZE = 2048;

        public static int MaxBytesForAsymmetricalEncryption
        {
            get
            {
                return ((KEY_SIZE - 384) / 8) + 37;
            }
        }

        public Dictionary<IPEndPoint, string> OthersRSAKeys = new Dictionary<IPEndPoint, string>();

        public void RegisterRSA(IPEndPoint endPoint, string publicKey)
        {
            lock (_lock)
            {
                if (OthersRSAKeys.ContainsKey(endPoint))
                {
                    OthersRSAKeys[endPoint] = publicKey;
                }
                else
                {
                    OthersRSAKeys.Add(endPoint, publicKey);
                }
            }
        }

        public void RemoveRSA(IPEndPoint endPoint)
        {
            lock (_lock)
            {
                if (OthersRSAKeys.ContainsKey(endPoint))
                {
                    OthersRSAKeys.Remove(endPoint);
                }
            }
        }


        public Dictionary<IPEndPoint, Tuple<byte[], byte[]>> OthersAesKeys = new Dictionary<IPEndPoint, Tuple<byte[], byte[]>>();

        public void RegisterAes(IPEndPoint endPoint, Tuple<byte[], byte[]> keyAndIV)
        {
            lock (_lock)
            {
                if (OthersAesKeys.ContainsKey(endPoint))
                {
                    OthersAesKeys[endPoint] = keyAndIV;
                }
                else
                {
                    OthersAesKeys.Add(endPoint, keyAndIV);
                }
            }
        }

        public void RemoveAex(IPEndPoint endPoint)
        {
            lock (_lock)
            {
                if (OthersAesKeys.ContainsKey(endPoint))
                {
                    OthersAesKeys.Remove(endPoint);
                }
            }
        }

        public RSACryptoServiceProvider MyRSA { get; set; }

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
                lock (_lock)
                {
                    SharedAes = new AesCryptoServiceProvider();
                    SharedAes.Padding = PaddingMode.PKCS7;
                    SharedAes.Key = value.Item1;
                    SharedAes.IV = value.Item2;
                }
            }
        }

        public string OthersPublicKey
        {
            get
            {
                return OthersRSA.ToXmlString(false);
            }
            set
            {
                lock (_lock)
                {
                    OthersRSA.FromXmlString(value);
                }
            }
        }

        public string MyPublicKey
        {
            get
            {
                return MyRSA.ToXmlString(false);
            }
        }

        public NetworkEncryption(NetworkClient client)
        {
            Client = client;
            SharedAes = new AesCryptoServiceProvider();
            SharedAes.GenerateIV();
            SharedAes.GenerateKey();
            SharedAes.Padding = PaddingMode.PKCS7;
            RSA rsa = RSA.Create(KEY_SIZE);
            MyRSA = new RSACryptoServiceProvider(KEY_SIZE);
            MyRSA.ImportParameters(rsa.ExportParameters(true));
            OthersRSA = new RSACryptoServiceProvider(KEY_SIZE);
        }

        public byte[] Encrypt(IPEndPoint to, byte[] data, bool useSymmetry = true, bool useMyKey = false)
        {
            lock (_lock)
            {
                if (useSymmetry)
                {
                    Aes aes = new AesCryptoServiceProvider();
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = OthersAesKeys[to].Item1;
                    aes.IV = OthersAesKeys[to].Item2;
                    MemoryStream stream = new MemoryStream(data);
                    using (CryptoStream cryptoStream = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                    }
                    return stream.ToArray();
                }
                else
                {
                    if (useMyKey)
                    {
                        return MyRSA.Encrypt(data, false);
                    }
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(KEY_SIZE);
                    rsa.FromXmlString(OthersRSAKeys[to]);
                    return rsa.Encrypt(data, false);
                }
            }
        }

        public byte[] Decrypt(IPEndPoint from, byte[] data, bool useSymmetry = true)
        {
            lock (_lock)
            {
                if (useSymmetry)
                {
                    Aes aes = new AesCryptoServiceProvider();
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = OthersAesKeys[from].Item1;
                    aes.IV = OthersAesKeys[from].Item2;
                    MemoryStream stream = new MemoryStream(data);
                    using (CryptoStream cryptoStream = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                    }
                    byte[] outputBytes = stream.ToArray();
                    return outputBytes;
                }
                else
                {
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(KEY_SIZE);
                    rsa.FromXmlString(OthersRSAKeys[from]);
                    return rsa.Decrypt(data, false);
                }
            }
        }

        /// <summary>
        /// Encrypts data using <see cref="OthersPublicKey"/> by default.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="useSymmetry"></param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] data, bool useSymmetry = true, bool useMyKey = false)
        {
            lock (_lock)
            {
                if (useSymmetry)
                {
                    AesCryptoServiceProvider aes = new AesCryptoServiceProvider
                    {
                        Padding = PaddingMode.PKCS7,
                        Key = SharedAesKey.Item1,
                        IV = SharedAesKey.Item2
                    };
                    MemoryStream stream = new MemoryStream();
                    using (CryptoStream cryptoStream = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                    }
                    return stream.ToArray();
                }
                else
                {
                    if (useMyKey)
                    {
                        return MyRSA.Encrypt(data, false);
                    }
                    return OthersRSA.Encrypt(data, false);
                }
            }
        }

        public byte[] Decrypt(byte[] data, bool useSymmetry = true)
        {
            lock (_lock)
            {
                if (useSymmetry)
                {
                    AesCryptoServiceProvider aes = new AesCryptoServiceProvider
                    {
                        Padding = PaddingMode.PKCS7,
                        Key = SharedAesKey.Item1,
                        IV = SharedAesKey.Item2
                    };
                    MemoryStream stream = new MemoryStream();
                    using (CryptoStream cryptoStream = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                    }
                    byte[] outputBytes = stream.ToArray();
                    return outputBytes;
                }
                else
                {
                    return MyRSA.Decrypt(data, false);
                }
            }
        }
    }
}
