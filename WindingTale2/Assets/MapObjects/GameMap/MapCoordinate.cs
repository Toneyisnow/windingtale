using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;

namespace WindingTale.MapObjects.GameMap
{
    public class MapCoordinate
    {
        public static Vector3 ConvertPosToVec3(FDPosition pos)
        {
            return new Vector3(-pos.X * 2, 0, pos.Y * 2);
        }

        public static Vector3 ConvertCreaturePosToVec3(FDPosition pos)
        {
            return new Vector3(-pos.X * 2, 2, pos.Y * 2);

        }
    }
}