using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.UI.FieldMap;

namespace WindingTale.UI.Components
{

    public class GameInterface : MonoBehaviour, IGameCallback
    {
        public int ChapterId = 0;

        private GameManager gameManager = null;

        // Start is called before the first frame update
        void Start()
        {
            gameManager = new GameManager(this);
            ChapterRecord record = ChapterRecord.NewGame();
            gameManager.StartGame(record);

            RenderFieldMap(record.ChapterId);
        }

        // Update is called once per frame
        void Update()
        {

        }

        void RenderFieldMap(int chapterId)
        {
            // Draw map on UI
            GameField field = gameManager.GetField();

            Material defaultMaterial = Resources.Load<Material>(@"common-mat");

            string mappingData = File.ReadAllText(@"D:\GitRoot\toneyisnow\windingtale\Resources\Remastered\Shapes\ShapePanel1\shape-mapping-1.json");
            ShapeMappings mappings = JsonConvert.DeserializeObject<ShapeMappings>(mappingData);

            Transform fieldRoot = this.transform.Find("FieldMap");

            HashSet<int> missingShapes = new HashSet<int>();
            for (int x = 0; x < field.Width; x++)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    int shapeIndex = field.Map[x, y];
                    string voxIndex = shapeIndex.ToString();
                    foreach (string key in mappings.mappings.Keys)
                    {
                        if (mappings.mappings[key].Contains(shapeIndex))
                        {
                            voxIndex = key;
                            break;
                        }
                    }

                    GameObject shape = Resources.Load<GameObject>(string.Format(@"Maps/ShapePanel{0}/Shape_{0}_{1}", chapterId, voxIndex));
                    if (shape == null)
                    {
                        missingShapes.Add(shapeIndex);
                        continue;
                    }

                    var renderer = shape.GetComponentInChildren<MeshRenderer>();
                    renderer.sharedMaterial = defaultMaterial;

                    GameObject go = FieldTransform.CreateShapeObject(shape, x, y);
                    go.transform.parent = this.transform;
                }
            }


        }
    }
}