using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SocketNetworking.UnityEngine
{
    public static class GameObjectExtensions
    {
        public static IEnumerable<string> GetTree(this GameObject @object)
        {
            List<string> list = new List<string>();
            RecursiveTree(@object, ref list);
            list.Reverse();
            return list;
        }

        static void RecursiveTree(GameObject current, ref List<string> tree)
        {
            tree.Add(current.name);
            if (current.transform.parent != null)
            {
                RecursiveTree(current.transform.parent.gameObject, ref tree);
            }
            return;
        }

        public static GameObject FindParentByTree(this List<string> tree)
        {
            return FindParentByTree(ref tree, 0, GameObject.Find(tree[0]));
        }

        static GameObject FindParentByTree(ref List<string> tree, int index, GameObject current)
        {
            if(current == null)
            {
                return null;
            }
            GameObject obj = current.transform.Find(tree[index]).gameObject;
            if (obj != null)
            {
                if(tree.Count == index + 2)
                {
                    return obj;
                }
                else
                {
                    return FindParentByTree(ref tree, index + 1, obj);
                }
            }
            return null;
        }
    }
}
