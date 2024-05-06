using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using WindingTale.Chapters;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;


namespace WindingTale.MapObjects.GameMap
{
    public class GameMap : MonoBehaviour
    {
        public GameObject fieldLayer;

        public GameObject creaturesLayer;


        public FDMap Map { get; private set; }

        public void Initialize(int chapterId)
        {
            this.Map = FDMap.loadFromChapter(chapterId);

            FieldLayer fieldComponent = fieldLayer.GetComponent<FieldLayer>();
            fieldComponent.Initialize(this.Map.Field);

        }


        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        //// public FDEvent[] Events { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="pos"></param>
        public void addCreature(FDCreature creature, FDPosition pos)
        {

        }
    }
}