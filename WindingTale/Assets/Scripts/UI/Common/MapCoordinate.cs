using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;

namespace WindingTale.UI.Common
{
    public class MapCoordinate
    {
        public static Vector3 ConvertPosToVec3(FDPosition pos)
        {
            return new Vector3(-pos.X * 2, 0, pos.Y * 2);
        }

    }
}
