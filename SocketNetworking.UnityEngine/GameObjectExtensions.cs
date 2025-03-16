﻿using SocketNetworking.UnityEngine.Components;
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
            return list;
        }

        static void RecursiveTree(GameObject current, ref List<string> tree)
        {
            tree.Append(current.name);
            if (current.transform.parent != null)
            {
                RecursiveTree(current.transform.parent.gameObject, ref tree);
            }
            return;
        }

        public static NetworkTransform ToNetworkTransform(this Transform transform)
        {
            return UnityNetworkManager.GetNetworkTransform(transform.gameObject);
        }

        public static NetworkAnimator ToNetworkAnimator(this Animator animator)
        {
            return UnityNetworkManager.GetNetworkAnimator(animator.gameObject);
        }
    }
}
