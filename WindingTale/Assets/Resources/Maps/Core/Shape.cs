using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.UI.Scenes.Game;

public class Shape : MonoBehaviour
{
    public FDPosition Position { get; private set; }

    public ShapeDefinition ShapeDefinition { get; private set; }    

    public MapField Field { get; private set; }

    // Start is called before the first frame update
    public void Init(FDPosition pos, ShapeDefinition def, MapField field)
    {
        this.Position = pos;
        this.ShapeDefinition = def;
        this.Field = field;

        EventTrigger trigger = this.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => { OnPointerDownDelegate((PointerEventData)data); });
        trigger.triggers.Add(entry);

        BoxCollider boxCollider = this.gameObject.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(24, 24, 1);
        boxCollider.center = new Vector3(12, -32, -24);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerDownDelegate(PointerEventData data)
    {
        Debug.Log("OnPointerDownDelegate called.");
        GameInterface.Instance.OnMapClicked(this.Position);
    }
}
