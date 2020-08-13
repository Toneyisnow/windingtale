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
using SmartLocalization;

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


        private GameObject gameCursor = null;

        private Material defaultMaterial = null;


        // Start is called before the first frame update
        void Start()
        {
            TestLocalization();
            activityManager = new GameActivityManager(this);

            gameManager = new GameManager(this);
            ChapterRecord record = ChapterRecord.NewGame();
            gameManager.StartGame(record);

            defaultMaterial = Resources.Load<Material>(@"common-mat");
            RenderFieldMap(record.ChapterId);

            globalVariables = Global.Instance();
            // Add creature
            //// FDCreature creature = new FDCreature(2, new CreatureDefinition() { DefinitionId = 2 }, FDPosition.At(1, 2));
            //// PlaceCreature(creature);

            
            fieldObjectsRoot = this.transform.Find("FieldObjects");

            InitializeObjects();
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

        private void TestLocalization()
        {
            if (LanguageManager.Instance.IsCultureSupported("en-US"))
            {
                int here = 1;
            }

            if (LanguageManager.Instance.IsCultureSupported("zh-CN"))
            {
                int here = 2;
            }

            LanguageManager.Instance.ChangeLanguage("zh-CN");
            //Returns a text value in the current language for the key
            string myKey = LanguageManager.Instance.GetTextValue("talk1");
        }

        public IGameAction GetGameAction()
        {
            return gameManager;
        }

        void RenderFieldMap(int chapterId)
        {
            // Draw map on UI
            GameField field = gameManager.GetField();
            
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
            creatureCom.Initialize(this, creatureId, animationid);


        }

        public void TouchCreature(int creatureId)
        {
            UICreature creature = GetUICreature(creatureId);
            FDPosition creaturePosition = creature.GetCurrentPosition();
            FDPosition cursorPosition = FieldTransform.GetObjectUnitPosition(gameCursor.transform.localPosition);

            if(!creaturePosition.AreSame(cursorPosition))
            {
                gameCursor.transform.localPosition = FieldTransform.GetObjectPixelPosition(FieldTransform.FieldObjectLayer.Ground, creaturePosition.X, creaturePosition.Y);
            }
            else
            {
                // Do the actuall game event
                gameManager.OnSelectPosition(creaturePosition);
            }
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

        #region Private Methods

        private void InitializeObjects()
        {
            // Init the cursor
            gameCursor = new GameObject();
            gameCursor.name = string.Format(@"game_cursor");
            gameCursor.transform.parent = fieldObjectsRoot;
            gameCursor.transform.localPosition = FieldTransform.GetObjectPixelPosition(FieldTransform.FieldObjectLayer.Ground, 10, 10);

            var comp = gameCursor.AddComponent<UICursor>();
            comp.Initialize(this);

        }


        #endregion


    }
}