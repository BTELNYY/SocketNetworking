using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

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
            var dm = new DynamicMethod("SizeOfType", typeof(int), new Type[] { });
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Sizeof, type);
            il.Emit(OpCodes.Ret);
            return (int)dm.Invoke(null, null);
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
    }
}
