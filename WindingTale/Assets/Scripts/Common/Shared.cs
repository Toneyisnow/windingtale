using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WindingTale.Common
{
    public static class Shared
    {
        public static List<T> CloneList<T>(this List<T> listData)
        {
            return (listData != null) ? listData.Select(item => item).ToList() : null;
        }
    }
}