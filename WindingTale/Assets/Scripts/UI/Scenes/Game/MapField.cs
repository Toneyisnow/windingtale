using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.UI.Common;
using WindingTale.UI.Scenes.Game;

public class MapField : MonoBehaviour
{
    private bool initialized = false;

    private Material defaultMaterial = null;


    // Start is called before the first frame update
    void Start()
    {
        defaultMaterial = Resources.Load<Material>("Materials/material-fd2-palette");

    }

    // Update is called once per frame
    void Update()
    {
        if (GameMain.Instance != null && !initialized)
        {
            initialized = true;
            buildField(GameMain.Instance.GameMap.Field);
        }
    }

    public void OnBlockClicked(FDPosition position)
    {
        if (!initialized) { return; }

        GameInterface.Instance.OnMapClicked(position);
    }


    private void buildField(GameField field)
    {
        for(int i = 1; i <= field.Width; i++)
        {
            for (int j = 1; j <= field.Height; j++)
            {
                FDPosition pos = FDPosition.At(i, j);
                ShapeDefinition shapeDef = field.GetShapeAt(pos);
                Debug.Log("get shape " + i + "," + j + " - " + shapeDef.Id);
                
                GameObject shapePrefab = Resources.Load<GameObject>(string.Format("Maps/ShapePanel1/Shape_1_{0}", shapeDef.Id));
                if (shapePrefab != null)
                {
                    GameObject shapeObj = Instantiate(shapePrefab, new Vector3(i, 0, j), Quaternion.identity);
                    shapeObj.transform.SetParent(this.transform);
                    shapeObj.transform.SetLocalPositionAndRotation(MapCoordinate.ConvertPosToVec3(pos), Quaternion.Euler(-90, 0, 0));
                    shapeObj.transform.localScale = new Vector3(0.083f, 0.083f, 0.083f);

                    MeshRenderer renderer = shapeObj.transform.Find("default").GetComponent<MeshRenderer>();
                    renderer.materials = new Material[1] { defaultMaterial };

                    Shape shape = shapeObj.AddComponent<Shape>();
                    shape.Init(pos, shapeDef, this);
                }
            }
        }
    }

}
