using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Utility
{
    public class Utility
    {
        public static GameObject FindByTree(IEnumerable<string> path)
        {
            GameObject obj = GameObject.Find(path.ElementAt(0));
            int counter = 0;
            while (obj != null)
            {
                if (path.Count() == counter + 1)
                {
                    return obj;
                }
                obj = obj.transform.Find(path.ElementAt(counter++))?.gameObject;
            }
            return null;
        }
    }
}
