using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.MapObjects.Blocks
{

    public class Shape : MonoBehaviour
    {
        public FDPosition Position { get; private set; }

        public ShapeDefinition ShapeDefinition { get; private set; }

        // Start is called before the first frame update
        public void Init(FDPosition pos, ShapeDefinition def)
        {
            this.Position = pos;
            this.ShapeDefinition = def;

            EventTrigger trigger = this.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerDown;
            entry.callback.AddListener((data) => { OnPointerDownDelegate((PointerEventData)data); });
            trigger.triggers.Add(entry);

            // Collider covers one 2-unit tile cell, centred on the shapeObj origin
            // (= ConvertPosToVec3(pos), the tile centre). The old (24,24,1)/(12,-36,-24)
            // values were sized for the 0.083 scale; at scale 1.0 they became ~12 tiles
            // wide and offset, so tile clicks no longer registered.
            BoxCollider boxCollider = this.gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(2, 2, 1);
            boxCollider.center = new Vector3(0, 0, 0);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnPointerDownDelegate(PointerEventData data)
        {
            Debug.Log("OnPointerDownDelegate called. " + Position.ToString());
            PlayerInterface.getDefault().onSelectedPosition(this.Position);
        }
    }
}