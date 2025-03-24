using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SocketNetworking
{
    public static class Extensions
    {
        public static T[] DeepClone<T>(this T[] array) where T : ICloneable
        {
            return array.Select(a => (T)a.Clone()).ToArray();
        }

        public static string GetHashSHA1(this byte[] data)
        {
            using (SHA1CryptoServiceProvider sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider())
            {
                return string.Concat(sha1.ComputeHash(data).Select(x => x.ToString("X2")));
            }
        }


        /// <summary>
        /// Removes a specific amount of elements from the start of the array. Note, this will create a copy of the given input array, therefore the return should NOT be ignored.
        /// </summary>
        /// <returns>
        /// The modified array.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the amount of items is greater then the amount of elements in the array, this will be thrown.
        /// </exception>
        public static T[] RemoveFromStart<T>(this T[] array, int amount)
        {
            if (array.Length < amount)
            {
                throw new ArgumentOutOfRangeException("amount", $"Amount ({amount}) is out of range for specified array. ({array.Length})");
            }
            //T[] result = new T[array.Length - amount];
            //Array.Copy(array, amount, result, 0, array.Length - amount);
            return array.Skip(amount).ToArray();
        }


        public static T[] Push<T>(this T[] array, T[] newData)
        {
            if (array.Length <= newData.Length)
            {
                array = newData.Take(array.Length).ToArray();
                return array;
            }
            else
            {
                foreach (T t in newData.Reverse())
                {
                    array = array.Prepend(t).ToArray();
                }
                array = array.Take(array.Length - newData.Length).ToArray();
                return array;
            }
        }

        public static T[] FastConcat<T>(this T[] first, T[] second)
        {
            T[] result = new T[first.Length + second.Length];
            Array.Copy(first, result, first.Length);
            Array.Copy(second, 0, result, first.Length, second.Length);
            return result;
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
            foreach (T item in value)
            {
                target = target.Append(item).ToArray();
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
            object lastEnum = Enum.GetValues(obj.GetType()).Cast<object>().Max();
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

        public static ulong GetULongStringHash(this string read)
        {
            ulong hashedValue = 3074457345618258791ul;
            for (int i = 0; i < read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }

        public static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static FieldInfo[] GetAllFields(this Type type, BindingFlags bindingAttr)
        {
            List<FieldInfo> fields = new List<FieldInfo>();
            while (type != typeof(object))
            {
                fields.AddRange(type.GetFields(bindingAttr));
                type = type.BaseType;
            }
            return fields.ToArray();
        }

        public static byte[] Compress(this byte[] bytes)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
                {
                    gzipStream.Write(bytes, 0, bytes.Length);
                }
                byte[] finalArray = memoryStream.ToArray();
#if DEBUG
                Log.GlobalDebug($"Compression Data. Input Length: {bytes.Length}, Compressed Length: {finalArray.Length}");
#endif
                return finalArray;
            }
        }

        public static byte[] Decompress(this byte[] bytes)
        {
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                using (MemoryStream outputStream = new MemoryStream())
                {
                    using (GZipStream decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
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

        public static int GetFirstEmptySlot(this IEnumerable<int> ints)
        {
            HashSet<int> set = new HashSet<int>(ints);
            int i = 1;
            while (set.Contains(i))
            {
                i++;
            }
            return i;
        }

        public static ushort GetFirstEmptySlot(this IEnumerable<ushort> ushorts)
        {
            HashSet<ushort> set = new HashSet<ushort>(ushorts);
            ushort i = 1;
            while (set.Contains(i))
            {
                i++;
            }
            return i;
        }

        public static IEnumerable<TSource> SkipLong<TSource>(this IEnumerable<TSource> source, long count)
        {
            while (count > int.MaxValue)
            {
                source = source.Skip(int.MaxValue).ToList();
                count -= int.MaxValue;
            }
            source = source.Skip(int.MaxValue).ToList();
            return source;
        }

        public static IEnumerable<TSource> TakeLong<TSource>(this IEnumerable<TSource> source, long count)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return TakeIterator(source, count);
        }

        private static IEnumerable<TSource> TakeIterator<TSource>(IEnumerable<TSource> source, long count)
        {
            if (count <= 0)
            {
                yield break;
            }

            foreach (TSource item in source)
            {
                yield return item;
                long num = count - 1;
                count = num;
                if (num == 0)
                {
                    break;
                }
            }
        }


        /// <summary>
        /// Checks if the type is a sublcass of the generic type <paramref name="generic"/>.
        /// </summary>
        /// <param name="toCheck"></param>
        /// <param name="generic"></param>
        /// <returns></returns>
        public static bool IsSubclassDeep(this Type toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public static int HowManyClassesUp(this Type toCheck, Type generic)
        {
            int counter = -1;
            while (toCheck != null && toCheck != typeof(object))
            {
                Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return counter + 1;
                }
                toCheck = toCheck.BaseType;
                counter++;
            }
            return counter;
        }

        public static T SetFlag<T>(this T value, T flag, bool set) where T : Enum
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

        public static List<T> GetActiveFlags<T>(this T value) where T : Enum
        {
            List<T> allValues = Enum.GetValues(typeof(T)).Cast<T>().ToList();
            List<T> activeFlags = new List<T>();
            foreach (T t in allValues)
            {
                if (value.HasFlag(t))
                {
                    activeFlags.Add(t);
                }
            }
            return activeFlags;
        }

        public static string GetActiveFlagsString<T>(this T @enum) where T : Enum
        {
            return string.Join(", ", @enum.GetActiveFlags());
        }

        public static IEnumerable<MethodInfo> GetMethodsDeep(this Type type, BindingFlags flags = BindingFlags.Default)
        {
            List<MethodInfo> list = new List<MethodInfo>();
            if (type == null)
            {
                return list;
            }
            while (type != typeof(object))
            {
                MethodInfo[] methods = type.GetMethods(flags);
                foreach (MethodInfo m in methods)
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

        /// <summary>
        /// Tries to match the parameters of an method to a given array of parameters. 
        /// This method does not support methods which take several params of the same <see cref="Type"/>
        /// </summary>
        /// <param name="method">
        /// The <see cref="MethodInfo"/> to try and match.
        /// </param>
        /// <param name="parameters">
        /// The expected parameters.
        /// </param>
        /// <returns>
        /// The properly ordered parameters for the <see cref="MethodInfo"/> or <see cref="null"/>
        /// </returns>
        public static IEnumerable<object> MatchParameters(this MethodInfo method, List<object> parameters)
        {
            if (method == null)
            {
                return null;
            }
            //Fuck... optional params...
            if (!method.GetParameters().Any(x => x.IsOptional) && method.GetParameters().Length > parameters.Count)
            {
                return null;
            }
            object[] result = new object[method.GetParameters().Length];
            for (int i = 0; i < method.GetParameters().Length; i++)
            {
                ParameterInfo parameter = method.GetParameters()[i];
                int index = -1;
                for (int j = 0; j < parameters.Count; j++)
                {
                    if (parameters[j].GetType().IsSubclassDeep(parameter.ParameterType))
                    {
                        index = j;
                    }
                }
                //didnt find one
                if (index == -1)
                {
                    if (parameter.IsOptional)
                    {
                        parameters[i] = parameter.DefaultValue;
                    }
                    else
                    {
                        //Failed to match param types.
                        return null;
                    }
                }
                result[i] = parameters[index];
                parameters.Remove(index);
                //reset index
                index = -1;
            }
            return result;
        }
    }
}
