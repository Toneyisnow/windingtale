using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.MapObjects.Blocks;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.MapObjects.GameMap
{

    public class FieldLayer : MonoBehaviour
    {
        private bool initialized = false;

        private Material defaultMaterial = null;

        public FDField Field { get; private set; }

        // Start is called before the first frame update
        void Start()
        {
            defaultMaterial = Resources.Load<Material>("Materials/material-fd2-palette");
        }

        public void Initialize(FDField field)
        {
            if (this.gameObject != null && !initialized)
            {
                buildField(field);
                initialized = true;
            }
        }


        private void buildField(FDField field)
        {
            for (int i = 1; i <= field.Width; i++)
            {
                for (int j = 1; j <= field.Height; j++)
                {
                    FDPosition pos = FDPosition.At(i, j);
                    ShapeDefinition shapeDef = field.GetShapeAt(pos);

                    GameObject shapePrefab = Resources.Load<GameObject>(string.Format("Maps/ShapePanel01/Shape_1_{0}", shapeDef.Id));
                    if (shapePrefab != null)
                    {
                        GameObject shapeObj = Instantiate(shapePrefab);
                        shapeObj.transform.SetParent(this.transform);
                        shapeObj.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(pos), Quaternion.Euler(90, 0, 0));
                        shapeObj.transform.localScale = new Vector3(0.083f, 0.083f, 0.083f);

                        Transform inner = shapeObj.transform.Find("default");
                        inner.SetLocalPositionAndRotation(new Vector3(24, -24, 0), Quaternion.Euler(180, 0, 0));

                        MeshRenderer renderer = inner.GetComponent<MeshRenderer>();
                        renderer.materials = new Material[1] { defaultMaterial };

                        Shape shape = shapeObj.AddComponent<Shape>();
                        shape.Init(pos, shapeDef);
                    }
                }
            }
        }
    }
}