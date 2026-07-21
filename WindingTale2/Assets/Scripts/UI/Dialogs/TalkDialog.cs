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

    // ConfirmArrow bounce, once the line has finished typing: the arrow dips below its
    // resting line and comes back, over and over, to invite the next key press.
    private const float ConfirmArrowBounceDepth = 8f;   // how far it dips, in pixels
    private const float ConfirmArrowBounceSpeed = 4f;   // radians per second

    // The arrow's resting Y, captured once from the prefab. Every bounce offset is measured
    // from this rather than from the arrow's live position: Init stops the bounce coroutine
    // mid-cycle, so the live Y is wherever the dip happened to be interrupted. Reading that
    // back as the next baseline is what used to walk the arrow up the dialog.
    private float confirmArrowBaseY = 0f;
    private bool confirmArrowBaseCaptured = false;

    // Confirm / Cancel buttons bounce the same way the ConfirmArrow does, but only the
    // one currently selected. Their resting Y is captured in Awake for the same reason.
    private float confirmButtonBaseY = 0f;
    private float cancelButtonBaseY = 0f;
    private bool confirmButtonsBaseCaptured = false;

    // Which button a keyboard confirm would trigger. Yes is selected when a dialog opens;
    // Right moves to No, Left comes back. Meaningless while needConfirm is false.
    private bool confirmSelected = true;

    private int creatureAnimationId = 0;

    private Action<int> onSelected = null;

    private GameObject activeTextObj = null;

    // Dato portrait frames: [0] neutral/mouth-closed, [1][2] speaking, [3] eyes-closed (blink).
    private Sprite[] datoFrames = new Sprite[4];

    // Runs on the dialog's first activation, before any Init: the one moment the arrow is
    // guaranteed to still be sitting where the prefab put it.
    void Awake()
    {
        if (confirmArrowObj != null)
        {
            RectTransform rt = confirmArrowObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                confirmArrowBaseY = rt.anchoredPosition.y;
                confirmArrowBaseCaptured = true;
            }
        }

        RectTransform confirmRt = confirmButtonObj != null ? confirmButtonObj.GetComponent<RectTransform>() : null;
        RectTransform cancelRt = cancelButtonObj != null ? cancelButtonObj.GetComponent<RectTransform>() : null;
        if (confirmRt != null && cancelRt != null)
        {
            confirmButtonBaseY = confirmRt.anchoredPosition.y;
            cancelButtonBaseY = cancelRt.anchoredPosition.y;
            confirmButtonsBaseCaptured = true;
        }
    }

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
        else
        {
            // Need confirm: Left / Right move the selection between Yes and No,
            // Space / Enter triggers whichever is selected.
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                confirmSelected = false;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                confirmSelected = true;
            }
            else if (Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (confirmSelected)
                {
                    onConfirm();
                }
                else
                {
                    onCancel();
                }
            }
        }
    }

    /// <summary>
    /// Init a message dialog, using CommonString as text material
    /// </summary>
    /// <param name="creatureAnimationId"></param>
    /// <param name="text"></param>
    /// <param name="onSelected"></param>
    public void InitMessage(int creatureAnimationId, LocalizedString text, bool needConfirm, GameCanvas.DialogPosition displayPosition, Action<int> onSelected, string resolvedText = null)
    {
        var textMeshComponent = messageTextObj.GetComponent<TextMeshProUGUI>();
        textMeshComponent.fontMaterial = Resources.Load<Material>(@"Fonts/FontAssets/zh/FZB_Message");
        textMeshComponent.fontSharedMaterial = Resources.Load<Material>(@"Fonts/FontAssets/zh/FZB_Message");
        //// textMeshComponent.UpdateFontAsset();
        textMeshComponent.ForceMeshUpdate();

        activeTextObj = messageTextObj;
        messageTextObj.SetActive(true);
        conversationTextObj.SetActive(false);

        Init(creatureAnimationId, text, needConfirm, displayPosition, onSelected, resolvedText);
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

    /// <summary>
    /// resolvedText, when supplied, is displayed instead of resolving `text` through the
    /// localization table. Used for lines assembled from several table entries (the
    /// level-up summary, for example), which no single LocalizedString can express.
    /// </summary>
    private void Init(int creatureAnimationId, LocalizedString text, bool needConfirm, GameCanvas.DialogPosition displayPosition, Action<int> onSelected, string resolvedText = null)
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
            ResetConfirmArrowPosition();
        }
        if (confirmButtonObj != null)
        {
            confirmButtonObj.SetActive(needConfirm);
        }
        if (cancelButtonObj != null)
        {
            cancelButtonObj.SetActive(needConfirm);
        }

        // Every confirm dialog opens with Yes selected.
        confirmSelected = true;
        ResetConfirmButtonPositions();

        // '#' in the source text is a line-break marker: start a new line and drop the '#'.
        fullText = (resolvedText ?? text.GetLocalizedString()).Replace("#", "\n");
        skipToFullText = false;
        textFinished = false;
        autoCloseTimer = AutoCloseSeconds;

        ///this.messageTextObj.GetComponent<LocalizeStringEvent>().StringReference = text;

        // Restart cleanly in case the dialog is being reused for a new line.
        StopAllCoroutines();
        StartCoroutine(BuildText());
        StartCoroutine(AnimateDato());
        StartCoroutine(AnimateConfirmArrow());
        StartCoroutine(AnimateConfirmButtons());
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
    /// "show only" dialogs (no explicit confirm)—reveals it and bounces it until the player
    /// moves on. Runs off confirmArrowBaseY, so an interrupted cycle leaves no trace on the
    /// next line's bounce.
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

        RectTransform rt = confirmArrowObj.GetComponent<RectTransform>();
        if (rt == null || !confirmArrowBaseCaptured)
        {
            yield break; // no rect to move, or no trustworthy baseline: leave it resting
        }

        // Abs(Sin) dips and returns rather than crossing the baseline, so the arrow's
        // resting line stays its upper limit and it cannot ride up into the text above.
        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime;

            Vector2 pos = rt.anchoredPosition;
            pos.y = confirmArrowBaseY
                - Mathf.Abs(Mathf.Sin(elapsed * ConfirmArrowBounceSpeed)) * ConfirmArrowBounceDepth;
            rt.anchoredPosition = pos;

            yield return null;
        }
    }

    /// <summary>
    /// Bounces whichever of the Yes / No buttons is currently selected, using the same curve
    /// as the ConfirmArrow so the two read as one idiom — but lifting rather than dipping:
    /// the buttons sit low enough in the dialog that a downward bounce read as too far down.
    /// The unselected button is held on its resting line, so the selection is visible the
    /// instant Left / Right is pressed.
    /// </summary>
    private IEnumerator AnimateConfirmButtons()
    {
        if (!needConfirm || !confirmButtonsBaseCaptured)
        {
            yield break;
        }

        // Wait until the typewriter output has finished, matching the arrow's timing.
        while (!textFinished)
        {
            yield return null;
        }

        RectTransform confirmRt = confirmButtonObj.GetComponent<RectTransform>();
        RectTransform cancelRt = cancelButtonObj.GetComponent<RectTransform>();

        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime;
            float dip = Mathf.Abs(Mathf.Sin(elapsed * ConfirmArrowBounceSpeed)) * ConfirmArrowBounceDepth;

            SetAnchoredY(confirmRt, confirmButtonBaseY + (confirmSelected ? dip : 0f));
            SetAnchoredY(cancelRt, cancelButtonBaseY + (confirmSelected ? 0f : dip));

            yield return null;
        }
    }

    /// <summary>
    /// Squares both buttons back up on their resting line, for the same reason the arrow
    /// is reset: Init stops the bounce wherever the dip happened to be.
    /// </summary>
    private void ResetConfirmButtonPositions()
    {
        if (!confirmButtonsBaseCaptured)
        {
            return;
        }

        SetAnchoredY(confirmButtonObj.GetComponent<RectTransform>(), confirmButtonBaseY);
        SetAnchoredY(cancelButtonObj.GetComponent<RectTransform>(), cancelButtonBaseY);
    }

    private static void SetAnchoredY(RectTransform rt, float y)
    {
        if (rt == null)
        {
            return;
        }

        Vector2 pos = rt.anchoredPosition;
        pos.y = y;
        rt.anchoredPosition = pos;
    }

    /// <summary>
    /// Puts the ConfirmArrow back on its resting line. Init stops the bounce wherever it
    /// happened to be, so the arrow is squared up before the next line reuses the dialog.
    /// </summary>
    private void ResetConfirmArrowPosition()
    {
        if (!confirmArrowBaseCaptured || confirmArrowObj == null)
        {
            return;
        }

        RectTransform rt = confirmArrowObj.GetComponent<RectTransform>();
        if (rt == null)
        {
            return;
        }

        Vector2 pos = rt.anchoredPosition;
        pos.y = confirmArrowBaseY;
        rt.anchoredPosition = pos;
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
