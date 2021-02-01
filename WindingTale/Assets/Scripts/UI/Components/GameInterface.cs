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
using WindingTale.UI.Common;
using WindingTale.UI.Dialogs;
using WindingTale.Core.Components.Data;
using Assets.Scripts.Common;
using Assets.Scripts.UI.Common;
using WindingTale.Core.Components.Algorithms;
using UnityEngine.SceneManagement;

namespace WindingTale.UI.Components
{
    public class GameInterface : MonoBehaviour, IGameCallback, IGameInterface
    {
        public int ChapterId = 0;

        public Canvas GameCanvas = null;

        private IGameHandler gameManager = null;

        private GameActivityManager activityManager = null;

        private Global globalVariables = null;
        
        private Transform fieldObjectsRoot = null;

        private Transform fieldMapRoot = null;

        private GameObject gameCursor = null;

        private List<GameObject> cancellableObjects = null;

        private GameObject currentDialog = null;

        public bool IsAIMode
        {
            get; private set;
        }

        // Start is called before the first frame update
        void Start()
        {
            TestLocalization();
            activityManager = new GameActivityManager(this);

            gameManager = new GameManager(this);
            ChapterRecord record = ChapterRecord.NewGame();
            gameManager.StartGame(record);

            RenderFieldMap(record.ChapterId);

            globalVariables = Global.Instance();
            
            fieldObjectsRoot = this.transform.Find("FieldObjects");
            InitializeObjects();

            cancellableObjects = new List<GameObject>();
            ///PlaceMenu(MenuId.ActionMenu, FDPosition.At(3, 3));

            // ShowMessageDialog(1, "This is test");

        }

        // Update is called once per frame
        void Update()
        {
            // If dialog is showing, stop Update
            if (currentDialog != null)
            {
                return;
            }

            globalVariables.Tick();

            // Call the update only if there is no dialog
            if (activityManager != null && currentDialog == null)
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

        public IGameHandler GetGameHandler()
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

                    GameObject shapeObject = new GameObject();
                    shapeObject.name = string.Format(@"shape_{0}_{1}", x + 1, y + 1);
                    UIShape shapeCom = shapeObject.AddComponent<UIShape>();
                    shapeCom.Initialize(this, fieldMapRoot, chapterId, voxIndex, FDPosition.At(x + 1, y + 1));

                    /*
                    GameObject shapeGO = AssetManager.Instance().InstantiateShapeGO(this.transform, chapterId, voxIndex);
                    if (shapeGO == null)
                    {
                        missingShapes.Add(shapeIndex);
                        continue;
                    }

                    shapeGO.transform.parent = fieldMapRoot;
                    shapeGO.transform.localPosition = FieldTransform.GetShapePixelPosition(x + 1, y + 1);

                    var box = shapeGO.AddComponent<BoxCollider>();
                    box.size = new Vector3(2.0f, 0.5f, 2.0f);
                    box.center = new Vector3(0f, 1f, 0f);
                    */

                    //var renderer = shapePrefab.GetComponentInChildren<MeshRenderer>();
                    //renderer.sharedMaterial = defaultMaterial;

                    // Note the the FlameDragon is counting matrix from 1, not 0
                    // GameObject go = FieldTransform.CreateShapeObject(shapePrefab, fieldMapRoot, x + 1, y + 1);
                }
            }
        }

        public UICreature PlaceCreature(int creatureId, int animationId, FDPosition position)
        {
            GameObject creatureObj = new GameObject();
            creatureObj.name = string.Format(@"creature_{0}", creatureId);
            creatureObj.transform.parent = fieldObjectsRoot;
            creatureObj.transform.localPosition = FieldTransform.GetCreaturePixelPosition(position);

            var creatureCom = creatureObj.AddComponent<UICreature>();
            creatureCom.Initialize(this, creatureId, animationId);
            //// creatureCom.SetAnimateState(UICreature.AnimateStates.Dying);

            return creatureCom;
        }

        public void DisposeCreature(int creatureId)
        {
            UICreature creature = GetUICreature(creatureId);
            Destroy(creature.gameObject);
        }

        public UIMenuItem PlaceMenu(MenuItemId menuItemId, FDPosition position, bool enabled, bool selected)
        {
            GameObject obj = new GameObject();
            obj.transform.parent = fieldObjectsRoot;

            var menuItem = obj.AddComponent<UIMenuItem>();
            menuItem.Initialize(this, menuItemId, position, enabled, selected);

            cancellableObjects.Add(obj);

            return menuItem;
        }

        public void ClearCancellableObjects()
        {
            foreach(GameObject obj in cancellableObjects)
            {
                GameObject.Destroy(obj);
            }

            cancellableObjects.Clear();
        }

        public void PlaceIndicators(FDRange range)
        {
            ClearCancellableObjects();

            foreach(FDPosition pos in range.Positions)
            {
                // Put indicator on the position
                GameObject obj = new GameObject();
                obj.transform.parent = fieldObjectsRoot;

                UIIndicator indicator = obj.AddComponent<UIIndicator>();
                indicator.Initialize(this, pos);

                cancellableObjects.Add(obj);
            }
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
                gameManager.HandleOperation(creaturePosition);
            }
        }

        public void TouchShape(FDPosition position)
        {
            // Move the cursor position to the current position
            FDPosition cursorPosition = FieldTransform.GetObjectUnitPosition(gameCursor.transform.localPosition);
            gameCursor.transform.localPosition = FieldTransform.GetObjectPixelPosition(FieldTransform.FieldObjectLayer.Ground, position.X, position.Y);
        }

        public void TouchMenu(FDPosition position)
        {
            // Do the actuall game event
            gameManager.HandleOperation(position);
        }

        public void TouchCursor()
        {
            FDPosition cursorPosition = FieldTransform.GetObjectUnitPosition(gameCursor.transform.localPosition);
            
            // Do the actuall game event
            gameManager.HandleOperation(cursorPosition);
        }

        public void OnHandlePack(PackBase pack)
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

        public void RefreshCreature(FDCreature creature)
        {
            if (creature == null)
            {
                throw new ArgumentNullException("creature");
            }

            UICreature uiCreature = GetUICreature(creature.CreatureId);
            if (uiCreature == null)
            {
                Debug.LogError("Cannot find creature on UI: creatureId = " + creature.CreatureId);
                return;
            }

            // Update position
            uiCreature.transform.localPosition = FieldTransform.GetCreaturePixelPosition(creature.Position);


            // Update status
            uiCreature.SetHasActioned(creature.HasActioned);
        }

        public void RefreshAllCreatures()
        {
            foreach(GameObject creatureObj in this.fieldObjectsRoot.gameObject.FindChildrenByName("creature_"))
            {
                UICreature creatureCom = creatureObj.GetComponent<UICreature>();
                if (creatureCom == null)
                {
                    continue;
                }

                creatureCom.SetHasActioned(false);
            }
        }

        public void ShowCreatureDialog(FDCreature creature, CreatureDialog.ShowType showType)
        {
            // Canvas
            if (currentDialog != null)
            {
                Destroy(currentDialog);
                currentDialog = null;
            }

            currentDialog = new GameObject();
            CreatureDialog creatureDialog = currentDialog.AddComponent<CreatureDialog>();
            creatureDialog.Initialize(this.GameCanvas, creature, showType,
                (index) => { this.OnDialogCallback(index); });

        }

        public void ShowConversationDialog(FDCreature creature, ConversationId conversation)
        {
            // Canvas
            if (currentDialog != null)
            {
                Destroy(currentDialog);
                currentDialog = null;
            }

            // Move the map to corresponding location

            // Show dialog
            string message = LocalizedStrings.GetConversationString(conversation);
            currentDialog = new GameObject();
            MessageDialog dialog = currentDialog.AddComponent<MessageDialog>();
            dialog.Initialize(this.GameCanvas, creature.Definition.AnimationId, conversation,
                (index) => { this.OnDialogCallback(index); } );

        }

        public void ShowMessageDialog(FDCreature creature, MessageId message)
        {
            // Canvas
            if (currentDialog != null)
            {
                Destroy(currentDialog);
                currentDialog = null;
            }

            // Move the map to corresponding location

            // Show dialog
            currentDialog = new GameObject();
            MessageDialog dialog = currentDialog.AddComponent<MessageDialog>();

            int animationId = (creature != null) ? creature.Definition.AnimationId : 0;
            dialog.Initialize(this.GameCanvas, animationId, message,
                (index) => { this.OnDialogCallback(index); });
        }

        public void BattleFight(FDCreature subject, FDCreature target, FightInformation fightInfo)
        {
            Debug.Log("Entering BattleFight");
            SceneManager.LoadScene("BattleScene", LoadSceneMode.Additive);
        }

        public void BattleMagic(FDCreature subject, FDCreature target, FightInformation magicInfo)
        {
            Debug.Log("Entering BattleMagic");


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

        private void OnDialogCallback(int index)
        {
            Debug.Log(@"GameInterface: OnDialogCallback: " + index);

            if (currentDialog != null)
            {
                Destroy(currentDialog);
                currentDialog = null;
            }

            gameManager.HandleOperation(index);
        }

        #endregion


    }
}