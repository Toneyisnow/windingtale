using Assets.Scripts.Common;
using Assets.Scripts.UI.Common;
using Assets.Scripts.UI.Dialogs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Common;
using WindingTale.UI.Common;

public class MessageDialog2 : MonoBehaviour
{
    public Canvas canvas;


    public void Initialize(Camera camera)
    {
        canvas.worldCamera = camera;

        GameObject messageBox = GameObject.Find("MessageBox");
        Clickable clickable = messageBox.GetComponent<Clickable>();
        clickable.Initialize(() => { this.OnClicked(); });

        var datoControl = GameObjectExtension.CreateFromPrefab<DatoControl>("Prefabs/DatoControl");
        datoControl.Initialize(canvas, 2, new Vector2(-240, -150));

    }

    // Start is called before the first frame update
    void Start()
    {
        ConversationId conv = ConversationId.Create(1, 1, 1);
        string localizedMessage = LocalizedStrings.GetConversationString(conv);

        GameObject textObj = FontAssets.ComposeTextMeshObjectForChapter(1, localizedMessage);
        textObj.transform.parent = GameObject.Find("TextAnchor1").transform;
        textObj.layer = 5;
        textObj.transform.localPosition = new Vector3(0, 0, 0);

        
        /*
        Texture2D texture = Resources.Load<Texture2D>(@"Datos/Dato_" + StringUtils.Digit3(1));
        Sprite normalSprite = Sprite.Create(texture, new Rect(0, 80, 80, 80), new Vector2(0.5f, 0.5f));
        Sprite talking1Sprite = Sprite.Create(texture, new Rect(80, 80, 80, 80), new Vector2(0.5f, 0.5f));
        Sprite talking2Sprite = Sprite.Create(texture, new Rect(0, 0, 80, 80), new Vector2(0.5f, 0.5f));
        Sprite blinkingSprite = Sprite.Create(texture, new Rect(80, 0, 80, 80), new Vector2(0.5f, 0.5f));

        GameObject datoImage0 = GameObject.Find("DatoImage0");
        Image image = datoImage0.GetComponent<Image>();
        image.sprite = normalSprite;
        */
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnClicked()
    {
        Debug.Log("Clicked on MessageDialog!");
    }
}
