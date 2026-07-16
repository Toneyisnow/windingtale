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

    public GameObject datoBoxObj;

    public GameObject conversationTextObj;

    public GameObject messageTextObj;

    public GameObject confirmArrowObj;

    public GameObject confirmButtonObj;

    public GameObject cancelButtonObj;

    public GameCanvas.DialogPosition displayPosition = GameCanvas.DialogPosition.Top;

    private String fullText = "";

    private bool skipToFullText = false;

    private bool textFinished = false;

    private bool needConfirm = false;

    // "Show only" dialogs (no explicit confirm) auto-dismiss after this many seconds
    // of no user input, counted from the moment the line has finished displaying.
    private const float AutoCloseSeconds = 5f;
    private float autoCloseTimer = AutoCloseSeconds;

    private int creatureAnimationId = 0;

    private Action<int> onSelected = null;

    private GameObject activeTextObj = null;

    // Dato portrait frames: [0] neutral/mouth-closed, [1][2] speaking, [3] eyes-closed (blink).
    private Sprite[] datoFrames = new Sprite[4];

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Show-only dialogs auto-dismiss after AutoCloseSeconds of no input, once the
        // line has fully displayed. Confirm dialogs always wait for the player.
        if (textFinished && !needConfirm)
        {
            autoCloseTimer -= Time.deltaTime;
            if (autoCloseTimer <= 0f)
            {
                onConfirm();
                return;
            }
        }

        if (!Input.anyKeyDown)
        {
            return;
        }

        if (!textFinished)
        {
            // First press skips the typewriter effect to the full text.
            skipToFullText = true;
        }
        else if (!needConfirm)
        {
            // Show only: pressing any keyboard or mouse button dismisses the dialog.
            onConfirm();
        }
    }

    /// <summary>
    /// Init a message dialog, using CommonString as text material
    /// </summary>
    /// <param name="creatureAnimationId"></param>
    /// <param name="text"></param>
    /// <param name="onSelected"></param>
    public void InitMessage(int creatureAnimationId, LocalizedString text, bool needConfirm, GameCanvas.DialogPosition displayPosition, Action<int> onSelected)
    {
        var textMeshComponent = messageTextObj.GetComponent<TextMeshProUGUI>();
        textMeshComponent.fontMaterial = Resources.Load<Material>(@"Fonts/FontAssets/zh/FZB_Message");
        textMeshComponent.fontSharedMaterial = Resources.Load<Material>(@"Fonts/FontAssets/zh/FZB_Message");
        //// textMeshComponent.UpdateFontAsset();
        textMeshComponent.ForceMeshUpdate();

        activeTextObj = messageTextObj;
        messageTextObj.SetActive(true);
        conversationTextObj.SetActive(false);

        Init(creatureAnimationId, text, needConfirm, displayPosition, onSelected);
    }

    /// <summary>
    /// Init a conversation dialog, the chapterId is used to load material for the text
    /// </summary>
    /// <param name="chapterId"></param>
    /// <param name="creatureAnimationId"></param>
    /// <param name="text"></param>
    /// <param name="onSelected"></param>
    public void InitConversation(int chapterId, int creatureAnimationId, LocalizedString text, GameCanvas.DialogPosition displayPosition, Action<int> onSelected)
    {
        var textMeshComponent = conversationTextObj.GetComponent<TextMeshProUGUI>();
        var chapterMaterial = Resources.Load<Material>(string.Format(@"Fonts/FontAssets/zh/FZB_Chapter-{0}", StringUtils.Digit2(chapterId)));
        if (chapterMaterial != null)
        {
            textMeshComponent.fontMaterial = chapterMaterial;
            textMeshComponent.UpdateFontAsset();
        }
        else
        {
            Debug.LogWarning(string.Format("TalkDialog: missing chapter font material FZB_Chapter-{0}", StringUtils.Digit2(chapterId)));
        }

        activeTextObj = conversationTextObj;
        conversationTextObj.SetActive(true);
        messageTextObj.SetActive(false);

        Init(creatureAnimationId, text, false, displayPosition, onSelected);
    }

    private void Init(int creatureAnimationId, LocalizedString text, bool needConfirm, GameCanvas.DialogPosition displayPosition, Action<int> onSelected)
    {
        this.creatureAnimationId = creatureAnimationId;

        // Load the 4 portrait frames. Frame 0 always exists; frames 1-3 may be
        // missing for characters without a talking animation (they fall back to 0).
        string id = StringUtils.Digit3(creatureAnimationId);
        for (int i = 0; i < datoFrames.Length; i++)
        {
            datoFrames[i] = Resources.Load<Sprite>(string.Format(@"Datos/{0}/Dato_{0}_{1}", id, i));
        }
        SetDatoFrame(0);

        Debug.Log("Talk Dialog Animation Id: " + creatureAnimationId);

        this.onSelected = onSelected;
        this.needConfirm = needConfirm;

        ApplyDisplayPosition(displayPosition);

        // Show only: display the ConfirmArrow and dismiss on any key press.
        // Need confirm: display the ConfirmButton / CancelButton and keep their callbacks.
        // The ConfirmArrow stays hidden while the line is being typed out; it is
        // shown (and starts bouncing) only after the text has finished (see AnimateConfirmArrow).
        if (confirmArrowObj != null)
        {
            confirmArrowObj.SetActive(false);
        }
        if (confirmButtonObj != null)
        {
            confirmButtonObj.SetActive(needConfirm);
        }
        if (cancelButtonObj != null)
        {
            cancelButtonObj.SetActive(needConfirm);
        }

        // '#' in the source text is a line-break marker: start a new line and drop the '#'.
        fullText = text.GetLocalizedString().Replace("#", "\n");
        skipToFullText = false;
        textFinished = false;
        autoCloseTimer = AutoCloseSeconds;

        ///this.messageTextObj.GetComponent<LocalizeStringEvent>().StringReference = text;

        // Restart cleanly in case the dialog is being reused for a new line.
        StopAllCoroutines();
        StartCoroutine(BuildText());
        StartCoroutine(AnimateDato());
        StartCoroutine(AnimateConfirmArrow());
    }

    /// <summary>
    /// Reposition the dialog elements along the X axis for the Up / Down layouts.
    /// </summary>
    private void ApplyDisplayPosition(GameCanvas.DialogPosition position)
    {
        this.displayPosition = position;
        bool up = position == GameCanvas.DialogPosition.Top;

        // Move the whole dialog: Top stays at screen center (Y=0), Bottom drops to Y=-200.
        RectTransform selfRt = GetComponent<RectTransform>();
        if (selfRt != null)
        {
            Vector2 selfPos = selfRt.anchoredPosition;
            selfPos.y = up ? 0f : -200f;
            selfRt.anchoredPosition = selfPos;
        }

        //                                   Up      Down
        SetAnchoredX(datoObj,             up ? 280f : -280f);
        SetAnchoredX(datoBoxObj,          up ? 280f : -280f);
        SetAnchoredX(conversationTextObj, up ? 0f   : 210f);
        SetAnchoredX(messageTextObj,      up ? 0f   : 210f);
        SetAnchoredX(confirmArrowObj,     up ? -100f : 100f);
        SetAnchoredX(confirmButtonObj,    up ? -64f  : 156f);
        SetAnchoredX(cancelButtonObj,     up ? 80f   : 300f);

        // In the Bottom position, flip the Dato portrait horizontally.
        if (datoObj != null)
        {
            RectTransform datoRt = datoObj.GetComponent<RectTransform>();
            if (datoRt != null)
            {
                Vector3 scale = datoRt.localScale;
                scale.x = Mathf.Abs(scale.x) * (up ? 1f : -1f);
                datoRt.localScale = scale;
            }
        }
    }

    private static void SetAnchoredX(GameObject obj, float x)
    {
        if (obj == null)
        {
            return;
        }

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null)
        {
            return;
        }

        Vector2 pos = rt.anchoredPosition;
        pos.x = x;
        rt.anchoredPosition = pos;
    }

    /// <summary>
    /// Drives the Dato portrait: a talking loop while text is being output,
    /// then an idle blink loop once the line has finished.
    /// </summary>
    private IEnumerator AnimateDato()
    {
        // Talking: 0 -> 1 -> 0 -> 2, 6 frames per second, looping.
        int[] talkSequence = { 0, 1, 0, 2 };
        int index = 0;
        while (!textFinished)
        {
            SetDatoFrame(talkSequence[index % talkSequence.Length]);
            index++;
            yield return new WaitForSeconds(1f / 6f);
        }

        // Idle blink: frame 0 held, then frame 3 for 0.5s, once every 5 seconds.
        while (true)
        {
            SetDatoFrame(0);
            yield return new WaitForSeconds(2.7f);
            SetDatoFrame(3);
            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>
    /// Keeps the ConfirmArrow hidden while the line is being typed out, then—only for the
    /// "show only" dialogs (no explicit confirm)—reveals it. The arrow is left at its
    /// resting position at the bottom of the dialog and is never moved: the previous
    /// up/down bounce captured its baseline from the live position, so an interrupted
    /// cycle made it drift upward across reused dialogs.
    /// </summary>
    private IEnumerator AnimateConfirmArrow()
    {
        if (confirmArrowObj == null || needConfirm)
        {
            yield break;
        }

        // Wait until the typewriter output has finished before showing the arrow.
        while (!textFinished)
        {
            yield return null;
        }

        confirmArrowObj.SetActive(true);
    }

    private void SetDatoFrame(int frame)
    {
        Sprite sprite = (frame >= 0 && frame < datoFrames.Length && datoFrames[frame] != null)
            ? datoFrames[frame]
            : datoFrames[0];

        if (sprite != null)
        {
            datoObj.GetComponent<Image>().sprite = sprite;
        }
    }

    private IEnumerator BuildText()
    {
        for (int i = 0; i < fullText.Length; i++)
        {
            if (skipToFullText)
            {
                activeTextObj.GetComponent<TextMeshProUGUI>().text = fullText;
                break;
            }
            
            String nowText = fullText.Substring(0, i + 1);
            activeTextObj.GetComponent<TextMeshProUGUI>().text = nowText;

            //Wait a certain amount of time, then continue with the for loop
            yield return new WaitForSeconds(0.05f);
        }

        textFinished = true;
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
