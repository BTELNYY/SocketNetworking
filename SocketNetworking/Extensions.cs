using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;

namespace SocketNetworking
{
    public static class Extensions
    {
        /// <summary>
        /// Removes a specific amount of elements from the start of the array. Note that the original array is alos modified.
        /// </summary>
        /// <typeparam name="T">
        /// Any Array.
        /// </typeparam>
        /// <param name="array">
        /// The array to modify
        /// </param>
        /// <param name="amount">
        /// Items to remove
        /// </param>
        /// <returns>
        /// The modified array.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the amount of items is greater then the amount of elements in the array, this will be thrown.
        /// </exception>
        public static T[] RemoveFromStart<T>(this T[] array, int amount)
        {
            //Log.Debug("Old Size: " + array.Length);
            if (array.Length < amount)
            {
                throw new ArgumentOutOfRangeException("amount", "Amount is out of range for specified array.");
            }
            List<T> newArray = array.ToList();
            newArray.RemoveRange(0, amount);
            //Log.Debug("New size: " + newArray.ToArray().Length + " Should have removed: " + amount);
            return newArray.ToArray();
        }

        /// <summary>
        /// Appends all elements of the value array to the target.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="value">
        /// The array to append
        /// </param>
        /// <returns>
        /// Modified array
        /// </returns>
        public static T[] AppendAll<T>(this T[] target, T[] value)
        {
            foreach(T item in value)
            {
                target.Append(item);
            }
            return target;
        }


        public static int SizeOf(this Type type)
        {
            return Marshal.SizeOf(type);
        }

        public static int SerializedSize(this string str)
        {
            return Encoding.UTF8.GetBytes(str).Length;
        }

        /// <summary>
        /// Hashes the given <see cref="string"/>.
        /// </summary>
        /// <param name="inputString">
        /// The string to hash
        /// </param>
        /// <returns>
        /// The hashed <see cref="string"/>
        /// </returns>
        public static string GetStringHash(this string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static byte[] Compress(this byte[] bytes)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
                {
                    gzipStream.Write(bytes, 0, bytes.Length);
                }
                return memoryStream.ToArray();
            }
        }

        public static byte[] Decompress(this byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        decompressStream.CopyTo(outputStream);
                    }
                    return outputStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Converts the byte array into its binary form
        /// </summary>
        /// <param name="bytes">
        /// The Byte array to convert
        /// </param>
        /// <returns>
        /// The String representation of the byte array in binary.
        /// </returns>
        public static string ConvertToBinary(this byte[] bytes)
        {
            string s = string.Empty;
            for (int i = 0; i < bytes.Length; i++)
            {
                s += Convert.ToString(bytes[i], 2).PadLeft(8, '0') + " ";
            }
            return s;
        }

        //I mean, AI is getting *better* but your O(1) time means jack shit becuase we have linear time now since you just iterate.
        public static int GetFirstEmptySlot(this IEnumerable<int> ints)
        {
            // Convert the list to a HashSet for O(1) lookup time.
            HashSet<int> set = new HashSet<int>(ints);

            int i = 1;
            // Iterate from 1 upward to find the first "free" spot.
            while (set.Contains(i))
            {
                i++;
            }

            return i; // Return the first "free" spot.
        }
    }
}
