using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;

namespace WindingTale.UI.FieldMap
{
    public class FieldTransform
    {
        public enum FieldObjectLayer
        {
            Ground = 1,
            Creature = 2,
            GroundObject = 3,
        }


        private static readonly float FloatPrecision = 0.01f;

        public static Vector3 GetShapePixelPosition(int x, int y)
        {
            return new Vector3(x * 2.4f, 0, - y * 2.4f);
        }

        public static Vector3 GetObjectPixelPosition(FieldObjectLayer layer, int x, int y)
        {
            float height = 0;
            switch(layer)
            {
                case FieldObjectLayer.Ground:
                    height = 2.5f;
                    break;
                case FieldObjectLayer.Creature:
                    height = 2.4f;
                    break;
                default:
                    break;
            }

            return new Vector3(x * 2.4f + 1.2f, height, -y * 2.4f + 1.2f);
        }

        public static Vector3 GetCreaturePixelPosition(FDPosition position)
        {
            return GetObjectPixelPosition(FieldObjectLayer.Creature, position.X, position.Y);
        }

        public static Vector3 GetGroundPixelPosition(FDPosition position)
        {
            return GetObjectPixelPosition(FieldObjectLayer.Ground, position.X, position.Y);
        }

        public static FDPosition GetObjectUnitPosition(Vector3 vector3)
        {
            int posX = (int)((vector3.x + FloatPrecision - 1.2f) / 2.4f);
            int posY = (int)(-(vector3.z - 1.2f - FloatPrecision) / 2.4f);
            return FDPosition.At(posX, posY);
        }

    }
}
