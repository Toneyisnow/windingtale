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

    public class ShapesLayer : MonoBehaviour
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

                    GameObject shapePrefab = Resources.Load<GameObject>(string.Format("Shapes/Shapes_01/Shape_1_{0}", shapeDef.Id));
                    if (shapePrefab != null)
                    {
                        GameObject shapeObj = Instantiate(shapePrefab);
                        shapeObj.transform.SetParent(this.transform);
                        shapeObj.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(pos), Quaternion.Euler(90, 0, 0));
                        shapeObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                        Transform inner = shapeObj.transform.Find("default");
                        // New Shapes_01 OBJs are exported centered (center=true, scale=0.1),
                        // so the mesh origin is already at the tile centre -> no offset.
                        // (The old (24,-24,0) was to re-centre the corner-origin ShapePanel01
                        // meshes; at scale 1.0 it shifted every tile by ~half the map.)
                        inner.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.Euler(180, 0, 0));

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