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
using SocketNetworking.PacketSystem;
using System.Reflection;

namespace SocketNetworking
{
    public static class Extensions
    {
        /// <summary>
        /// Invokes a network method on the current <see cref="INetworkObject"></see>, provide a <see cref="NetworkClient"/> which will get the network invoke.
        /// </summary>
        /// <param name="object"></param>
        /// <param name="sender"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public static void NetworkInvoke(this INetworkObject @object, NetworkClient sender, string methodName, object[] args)
        {
            NetworkManager.NetworkInvoke(@object, sender, methodName, args);
        }

        public static T NetworkInvoke<T>(this INetworkObject obj, NetworkClient sender, string methodName, object[] args, float timeOutMs = 5000f)
        {
            return NetworkManager.NetworkInvoke<T>(obj, sender, methodName, args, timeOutMs);
        } 

        /// <summary>
        /// Invokes a method. Note that this can only be run on the server, as not NetworkClient is provided. Also note that this will work as all client RPC, meaning every client will get the invocation.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public static void NetworkInvoke(this INetworkObject obj, string methodName, object[] args)
        {
            NetworkServer.NetworkInvokeOnAll(obj, methodName, args);
        }

        /// <summary>
        /// Removes a specific amount of elements from the start of the array.
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

        /// <summary>
        /// Compares two arrays. Since C#'s == operator is about as smart as a door nail regarding arrays.
        /// </summary>
        /// <typeparam name="T">
        /// Any type of array
        /// </typeparam>
        /// <param name="a1">
        /// Array to compare 1
        /// </param>
        /// <param name="a2">
        /// Array to compare 2
        /// </param>
        /// <returns>
        /// true if the array is the same, or false if it is different.
        /// </returns>
        public static bool ArraysEqual<T>(this T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++)
            {
                if (!comparer.Equals(a1[i], a2[i])) return false;
            }
            return true;
        }

        public static object LastEnum(this object obj)
        {
            var lastEnum = Enum.GetValues(obj.GetType()).Cast<object>().Max();
            return lastEnum;
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
                byte[] finalArray  = memoryStream.ToArray();
#if DEBUG
                Log.GlobalDebug($"Compression Data. Input Length: {bytes.Length}, Compressed Length: {finalArray.Length}");
#endif
                return finalArray;
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
                    byte[] output = outputStream.ToArray();
                    return output;
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

        /// <summary>
        /// Checks if the type is a sublcass of the generic type <paramref name="generic"/>.
        /// </summary>
        /// <param name="toCheck"></param>
        /// <param name="generic"></param>
        /// <returns></returns>
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public static T SetFlag<T>(this Enum value, T flag, bool set)
        {
            Type underlyingType = Enum.GetUnderlyingType(value.GetType());

            // note: AsInt mean: math integer vs enum (not the c# int type)
            dynamic valueAsInt = Convert.ChangeType(value, underlyingType);
            dynamic flagAsInt = Convert.ChangeType(flag, underlyingType);
            if (set)
            {
                valueAsInt |= flagAsInt;
            }
            else
            {
                valueAsInt &= ~flagAsInt;
            }

            return (T)valueAsInt;
        }

        public static List<T> GetActiveFlags<T>(this T value) where  T : Enum
        {
            List<T> allValues = Enum.GetValues(typeof(T)).Cast<T>().ToList();
            List<T> activeFlags = new List<T>();
            foreach(T t in allValues)
            {
                if (value.HasFlag(t))
                {
                    activeFlags.Add(t);
                }
            }
            return activeFlags;
        }

        public static IEnumerable<MethodInfo> GetMethodsDeep(this Type type, BindingFlags flags = BindingFlags.Default)
        {
            List<MethodInfo> list = new List<MethodInfo>();
            if(type == null)
            {
                return list;
            }
            while(type != typeof(object))
            {
                MethodInfo[] methods = type.GetMethods(flags);
                foreach(MethodInfo m in methods)
                {
                    if (list.Contains(m))
                    {
                        continue;
                    }
                    list.Add(m);
                }
                type = type.BaseType;
            }
            return list;
        }
    }
}
