using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.Scenes.GameFieldScene;

public class TalkDialog : MonoBehaviour
{
    public GameObject messageText;


    private Action<int> onSelected = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(int creatureAnimationId, LocalizedString text, Action<int> onSelected)
    {
        this.onSelected = onSelected;

        //// this.messageText.GetComponent<TextMeshProUGUI>().text = "听说再越过这片海洋就到马拉大陆了，我们先在此休息一会儿，等海水涨潮适合我们上岸的时候船夫就会来接我们。";



        // LocalizedString contentString = LocalizationManager.GetConversationString(1, 1, 3);
        this.messageText.GetComponent<LocalizeStringEvent>().StringReference = text;

    }

    public void onConfirm()
    {
        this.onSelected(1);
        GameMain.getDefault().gameCanvas.CloseDialog();
    }


    public void onCancel()
    {
        this.onSelected(-1);
        GameMain.getDefault().gameCanvas.CloseDialog();
    }
}
