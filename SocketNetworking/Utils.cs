using System;
using System.Collections.Generic;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking
{
    public class Utils
    {
        public static byte[] ShiftOut(ref byte[] input, int count)
        {
            byte[] output = new byte[count];
            // copy the first N elements to output
            Buffer.BlockCopy(input, 0, output, 0, count);
            // copy the back of the array forward (removing the elements we copied to output)
            // ie with count 2: [1, 2, 3, 4] -> [3, 4, 3, 4]
            // we dont update the end of the array to fill with zeros, because we dont need to (we just call it undefined behavior)
            Buffer.BlockCopy(input, count, input, 0, count);
            return output;
        }

        public static List<List<T>> SplitIntoChunks<T>(IEnumerable<T> list, int maxSize) where T : IByteSerializable
        {
            List<List<T>> result = new List<List<T>>();
            List<T> currentList = new List<T>();
            int currentSize = 0;
            foreach (var chunk in list)
            {
                int size = chunk.GetLength();
                if((currentSize + size) > maxSize)
                {
                    currentList = new List<T>();
                    currentSize = size;
                    currentList.Add(chunk);
                }
                else
                {
                    currentSize += size;
                    currentList.Add(chunk);
                }
            }
            return result;
        }
    }
}
