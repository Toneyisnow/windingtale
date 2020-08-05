using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.iOS;
using UnityEngine;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.Components.Activities;
using WindingTale.UI.FieldMap;
using WindingTale.UI.MapObjects;

namespace WindingTale.UI.Components
{

    public class GameInterface : MonoBehaviour, IGameCallback
    {
        public int ChapterId = 0;

        private GameManager gameManager = null;

        private GameActivityManager activityManager = null;

        public GameObject creaturePrefab = null;


        // Start is called before the first frame update
        void Start()
        {
            activityManager = new GameActivityManager(this);

            gameManager = new GameManager(this);
            ChapterRecord record = ChapterRecord.NewGame();
            gameManager.StartGame(record);

            RenderFieldMap(record.ChapterId);


            // Add creature
            ComposeCreatureAt(2, 1, 1);
            ComposeCreatureAt(3, 1, 2);
            ComposeCreatureAt(4, 1, 3);
            ComposeCreatureAt(5, 1, 5);

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

                    // Note the the FlameDragon is counting matrix from 1, not 0
                    GameObject go = FieldTransform.CreateShapeObject(shape, x + 1, y + 1);
                    go.transform.parent = fieldRoot;
                }
            }


        }

        public void ComposeCreatureAt(int creatureId, int posX, int posY)
        {
            GameObject creature = GameObject.Instantiate(creaturePrefab);

            creature.transform.parent = this.transform.Find("FieldObjects");
            creature.transform.localPosition = FieldTransform.GetCreaturePosition(posX, posY);
            creature.transform.localRotation = new Quaternion(0, 180, 0, 0);
            var creatureCom = creature.GetComponent<UICreature>();

            FDCreature c = new FDCreature(creatureId);
            creatureCom.Initialize(c);
        }

        public void OnReceivePack(PackBase pack)
        {
            activityManager.PushPack(pack);
        }

    }
}