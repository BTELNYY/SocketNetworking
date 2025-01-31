using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SocketNetworking.UnityEngine
{
    public static class GameObjectExtensions
    {
        public static IEnumerable<string> GetTree(this GameObject @object)
        {
            List<string> list = new List<string>();
            RecursiveTree(@object, ref list);
            return list;
        }

        static void RecursiveTree(GameObject current, ref List<string> tree)
        {
            tree.Append(current.name);
            if(current.transform.parent != null)
            {
                RecursiveTree(current.transform.parent.gameObject, ref tree);
            }
            return;
        }
    }
}
