using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using WindingTale.Core.Common;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.Scenes.GameFieldScene;

public class TalkDialog : MonoBehaviour
{
    public GameObject datoObj;

    public GameObject messageTextObj;

    private String fullText = "";

    private bool skipToFullText = false;

    private int creatureAnimationId = 0;

    private Action<int> onSelected = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKey)
        {
            skipToFullText = true;
        }
    }

    public void Init(int creatureAnimationId, LocalizedString text, Action<int> onSelected)
    {
        this.creatureAnimationId = creatureAnimationId;
        this.datoObj.GetComponent<Image>().sprite = Resources.Load<Sprite>(
            string.Format(@"Datos/Dato_{0}", StringUtils.Digit3(creatureAnimationId))
        );

        Debug.Log("Talk Dialog Animation Id: " + creatureAnimationId);

        this.onSelected = onSelected;

        fullText = text.GetLocalizedString();
        skipToFullText = false;

        ///this.messageTextObj.GetComponent<LocalizeStringEvent>().StringReference = text;

        StartCoroutine(BuildText());
    }


    private IEnumerator BuildText()
    {
        for (int i = 0; i < fullText.Length; i++)
        {
            if (skipToFullText)
            {
                this.messageTextObj.GetComponent<TextMeshProUGUI>().text = fullText;
                break;
            }
            
            String nowText = fullText.Substring(0, i + 1);
            this.messageTextObj.GetComponent<TextMeshProUGUI>().text = nowText;

            //Wait a certain amount of time, then continue with the for loop
            yield return new WaitForSeconds(0.05f);
        }
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
