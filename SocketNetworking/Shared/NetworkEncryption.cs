using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using SocketNetworking.Client;

namespace SocketNetworking.Shared
{
    /// <summary>
    /// The <see cref="NetworkEncryption"/> class handles RSA/AES Encryption for connections.
    /// </summary>
    public class NetworkEncryption
    {
        object _lock = new object();

        /// <summary>
        /// The <see cref="NetworkClient"/> which owns this object instance.
        /// </summary>
        public NetworkClient Client { get; }

        /// <summary>
        /// The RSA Key size.
        /// </summary>
        public const int KEY_SIZE = 2048;

        /// <summary>
        /// Maximum length of the buffer that can be encrypted using RSA.
        /// </summary>
        public static int MaxBytesForAsymmetricalEncryption
        {
            get
            {
                return ((KEY_SIZE - 384) / 8) + 37;
            }
        }

        public Dictionary<IPEndPoint, string> OthersRSAKeys = new Dictionary<IPEndPoint, string>();

        /// <summary>
        /// Adds a <paramref name="endPoint"/>s RSA public key to memory. 
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="publicKey"></param>
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

        /// <summary>
        /// Removes a <paramref name="endPoint"/>s public key from memory.
        /// </summary>
        /// <param name="endPoint"></param>
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


        /// <summary>
        /// AES Keys shared from other <see cref="IPEndPoint"/>s.
        /// </summary>
        public Dictionary<IPEndPoint, Tuple<byte[], byte[]>> OthersAesKeys = new Dictionary<IPEndPoint, Tuple<byte[], byte[]>>();

        /// <summary>
        /// Adds a <paramref name="endPoint"/>s AES key to memory.
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="keyAndIV"></param>
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

        /// <summary>
        /// Removes a <paramref name="endPoint"/>s AES key from memory.
        /// </summary>
        /// <param name="endPoint"></param>
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

        /// <summary>
        /// Local RSA provider.
        /// </summary>
        public RSACryptoServiceProvider MyRSA { get; set; }

        /// <summary>
        /// Remote RSA Provider.
        /// </summary>
        public RSACryptoServiceProvider OthersRSA { get; set; }

        /// <summary>
        /// Shared AES Provider.
        /// </summary>
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
                    SharedAes = Aes.Create();
                    SharedAes.Padding = PaddingMode.PKCS7;
                    SharedAes.Key = value.Item1;
                    SharedAes.IV = value.Item2;
                }
            }
        }


        /// <summary>
        /// Remote public key.
        /// </summary>
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

        /// <summary>
        /// Local public key.
        /// </summary>
        public string MyPublicKey
        {
            get
            {
                return MyRSA.ToXmlString(false);
            }
        }

        public NetworkEncryption(NetworkClient client)
        {
            //client?.Log.Info($"Start generating key pair...");
            Client = client;
            SharedAes = Aes.Create();
            SharedAes.GenerateIV();
            SharedAes.GenerateKey();
            SharedAes.Padding = PaddingMode.PKCS7;
            RSA rsa = RSA.Create();
            rsa.KeySize = KEY_SIZE;
            MyRSA = new RSACryptoServiceProvider(KEY_SIZE);
            MyRSA.ImportParameters(rsa.ExportParameters(true));
            OthersRSA = new RSACryptoServiceProvider(KEY_SIZE);
            //client?.Log.Info("Done generating crypto!");
        }

        /// <summary>
        /// Encrypts <paramref name="data"/> using the RSA or AES algorithim (determinted by <paramref name="useSymmetry"/>) using either local key or the key of the <paramref name="peer"/>, determined by <paramref name="useMyKey"/>.
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="data"></param>
        /// <param name="useSymmetry"></param>
        /// <param name="useMyKey"></param>
        /// <returns></returns>
        public byte[] Encrypt(IPEndPoint peer, byte[] data, bool useSymmetry = true, bool useMyKey = false)
        {
            lock (_lock)
            {
                if (useSymmetry)
                {
                    Aes aes = Aes.Create();
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = SharedAesKey.Item1;
                    aes.IV = SharedAesKey.Item2;
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
                    rsa.FromXmlString(OthersRSAKeys[peer]);
                    return rsa.Encrypt(data, false);
                }
            }
        }

        /// <summary>
        /// Decrypts <paramref name="data"/> from the <paramref name="peer"/> using either AES or RSA determined by <paramref name="useSymmetry"/>.
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="data"></param>
        /// <param name="useSymmetry"></param>
        /// <returns></returns>
        public byte[] Decrypt(IPEndPoint peer, byte[] data, bool useSymmetry = true)
        {
            lock (_lock)
            {
                if (useSymmetry)
                {
                    Aes aes = Aes.Create();
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = SharedAesKey.Item1;
                    aes.IV = SharedAesKey.Item2;
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
                    rsa.FromXmlString(OthersRSAKeys[peer]);
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
                    Aes aes = Aes.Create();
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = SharedAesKey.Item1;
                    aes.IV = SharedAesKey.Item2;
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

        /// <summary>
        /// Decrypts <paramref name="data"/> using <see cref="OthersPublicKey"/> or <see cref="SharedAes"/> determined by <paramref name="useSymmetry"/>.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="useSymmetry"></param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] data, bool useSymmetry = true)
        {
            lock (_lock)
            {
                if (useSymmetry)
                {
                    Aes aes = Aes.Create();
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = SharedAesKey.Item1;
                    aes.IV = SharedAesKey.Item2;
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
