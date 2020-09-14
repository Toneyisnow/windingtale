using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.UI.Common
{
    public static class GameObjectExtension
    {
        public static List<GameObject> FindChildrenByName(this GameObject obj, string namePattern)
        {
            List<GameObject> children = new List<GameObject>();
            foreach (Transform tr in obj.transform)
            {
                if (tr.gameObject.name.StartsWith(namePattern))
                {
                    children.Add(tr.gameObject);
                }
            }

            return children;
        }
    }
}