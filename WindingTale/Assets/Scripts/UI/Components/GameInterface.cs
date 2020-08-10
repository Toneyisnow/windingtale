using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WindingTale.Core.Definitions;
using WindingTale.Core.Components;
using WindingTale.Core.Components.Packs;
using WindingTale.Core.ObjectModels;
using WindingTale.UI.Components.Activities;
using WindingTale.UI.FieldMap;
using WindingTale.UI.MapObjects;
using WindingTale.Common;
using System;

namespace WindingTale.UI.Components
{

    public class GameInterface : MonoBehaviour, IGameCallback, IGameInterface
    {
        public int ChapterId = 0;

        private GameManager gameManager = null;

        private GameActivityManager activityManager = null;

        private Global globalVariables = null;
        
        public GameObject creaturePrefab = null;

        private Transform fieldObjectsRoot = null;

        private Transform fieldMapRoot = null;


        // Start is called before the first frame update
        void Start()
        {
            activityManager = new GameActivityManager(this);

            gameManager = new GameManager(this);
            ChapterRecord record = ChapterRecord.NewGame();
            gameManager.StartGame(record);

            RenderFieldMap(record.ChapterId);

            globalVariables = Global.Instance();
            // Add creature
            //// FDCreature creature = new FDCreature(2, new CreatureDefinition() { DefinitionId = 2 }, FDPosition.At(1, 2));
            //// PlaceCreature(creature);

            fieldObjectsRoot = this.transform.Find("FieldObjects");
        }

        // Update is called once per frame
        void Update()
        {
            globalVariables.Tick();

            if (activityManager != null)
            {
                activityManager.Update();
            }
        }

        public IGameAction GetGameAction()
        {
            return gameManager;
        }

        void RenderFieldMap(int chapterId)
        {
            // Draw map on UI
            GameField field = gameManager.GetField();
            Material defaultMaterial = Resources.Load<Material>(@"common-mat");

            fieldMapRoot = this.transform.Find("FieldMap");
            string mappingData = File.ReadAllText(@"D:\GitRoot\toneyisnow\windingtale\Resources\Remastered\Shapes\ShapePanel1\shape-mapping-1.json");
            ShapeMappings mappings = JsonConvert.DeserializeObject<ShapeMappings>(mappingData);

            HashSet<int> missingShapes = new HashSet<int>();
            for (int x = 0; x < field.Width; x++)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    int shapeIndex = field.Map[x, y];
                    string voxIndexStr = shapeIndex.ToString();
                    foreach (string key in mappings.mappings.Keys)
                    {
                        if (mappings.mappings[key].Contains(shapeIndex))
                        {
                            voxIndexStr = key;
                            break;
                        }
                    }

                    Int32.TryParse(voxIndexStr, out int voxIndex);
                    GameObject shapePrefab = AssetManager.Instance().LoadShapePrefab(chapterId, voxIndex);
                    if (shapePrefab == null)
                    {
                        missingShapes.Add(shapeIndex);
                        continue;
                    }
                    
                    var renderer = shapePrefab.GetComponentInChildren<MeshRenderer>();
                    renderer.sharedMaterial = defaultMaterial;

                    // Note the the FlameDragon is counting matrix from 1, not 0
                    GameObject go = FieldTransform.CreateShapeObject(shapePrefab, fieldMapRoot, x + 1, y + 1);
                }
            }

        }

        public void PlaceCreature(int creatureId, int animationid, FDPosition position)
        {
            GameObject creatureObj = GameObject.Instantiate(creaturePrefab);
            creatureObj.name = string.Format(@"creature_{0}", creatureId);
            creatureObj.transform.parent = fieldObjectsRoot;
            creatureObj.transform.localPosition = FieldTransform.GetCreaturePixelPosition(position);

            var creatureCom = creatureObj.GetComponent<UICreature>();
            creatureCom.Initialize(animationid);


        }

        public void OnReceivePack(PackBase pack)
        {
            activityManager.PushPack(pack);
        }



        #region IGameInterface

        public UICreature GetUICreature(int creatureId)
        {
            string name = string.Format(@"creature_{0}", creatureId);
            Transform cTransform = this.fieldObjectsRoot.Find(name);
            if (cTransform != null)
            {
                return cTransform.GetComponent<UICreature>();
            }

            return null;
        }

        #endregion

    }
}