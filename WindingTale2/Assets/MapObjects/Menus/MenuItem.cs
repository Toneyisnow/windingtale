using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using WindingTale.Core.Common;
using WindingTale.Scenes.GameFieldScene;

public class MenuItem : MonoBehaviour
{
    private FDMenuItem fDMenuItem = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(FDMenuItem fDMenuItem)
    {
        this.fDMenuItem = fDMenuItem;
        EventTrigger trigger = this.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => { OnPointerDownDelegate((PointerEventData)data); });
        trigger.triggers.Add(entry);

        BoxCollider boxCollider = this.gameObject.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(24, 24, 1);
        boxCollider.center = new Vector3(-12, 11, 1);
    } 


    public void OnPointerDownDelegate(PointerEventData data)
    {
        Debug.Log("MenuItem: OnPointerDownDelegate called. " + fDMenuItem.Position.ToString());
        PlayerInterface.getDefault().onSelectedPosition(fDMenuItem.Position);
    }
}
