using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Files;

/// <summary>
/// The save / load slot picker, pushed over the shop's home dialog when the player
/// chooses Save or Load at the bar (and shown by the title screen's Load). It reads every
/// slot with GameRecordManager.GetAllFiles and lists all twenty across pages of four, each
/// row "<n>. 第X关：<chapter name>" or, when the slot is empty, "<n>.  ==空==".
///
/// Up / Down walk the four rows; stepping down off the last row turns to the next page
/// (cursor to the top), stepping up off the first turns back a page (cursor to the bottom),
/// with no wrap at either end of the twenty. Space / Enter confirms the highlighted slot
/// and hands its index back through OnSlotSelected; Esc backs out with -1.
///
/// It does not itself save or load -- it only reports which slot the player picked. The
/// caller owns what happens next (write the record, or read it), which is why the same
/// dialog serves Save, the shop's Load, and the title screen's Load alike.
/// </summary>
public class ShoppingRecordDialog : MonoBehaviour
{
    public GameObject RecordLabel1;

    public GameObject RecordLabel2;

    public GameObject RecordLabel3;

    public GameObject RecordLabel4;

    public GameObject ButtonUp;

    public GameObject ButtonDown;

    public GameObject Indicator;

    /// <summary>
    /// The indicator's anchored Y for each of the four rows, so it sits over the highlighted
    /// one. Seeded to roughly the Record 1..4 label positions (their Y with the small offset
    /// the indicator already carries over row 1); exposed for fine-tuning in the inspector.
    /// </summary>
    public float[] IndicatorRowY = { -51f, -88f, -126.6f, -164f };

    /// <summary>The indicator's brightest opacity, 0..1 -- the high point of its pulse.</summary>
    public float IndicatorAlpha = 0.2f;

    /// <summary>The indicator's dimmest opacity, the low point of its pulse.</summary>
    public float IndicatorAlphaMin = 0.05f;

    /// <summary>Seconds for one full IndicatorAlpha -> IndicatorAlphaMin -> IndicatorAlpha pulse.</summary>
    public float IndicatorFadePeriod = 2f;


    /// <summary>
    /// Raised when the player settles on a slot: the slot's index (0..19) on a confirm, or
    /// -1 when the dialog is backed out of with Esc. The caller reads it, closes this
    /// dialog, and decides what to do with the slot.
    /// </summary>
    public Action<int> OnSlotSelected = null;

    // How many rows a page shows. The twenty slots fill five of these pages.
    private const int RowsPerPage = 4;

    private GameObject[] labels = null;

    // The indicator's Image and RectTransform, cached in Init so the per-frame pulse and
    // reposition do not re-fetch them. The elapsed clock drives the alpha pulse.
    private Image indicatorImage = null;
    private RectTransform indicatorRect = null;
    private float alphaElapsed = 0f;

    // Whether an empty slot can be confirmed. True for saving (an empty slot is a fresh
    // slot to write into); false for the load pickers, where confirming an empty slot does
    // nothing and leaves the dialog up. Set from Init.
    private bool allowEmptySlots = true;

    // The saves that exist, keyed by slot index. Read once in Init.
    private Dictionary<int, GameRecord> records = null;

    // How many slots there are in all, and how many pages that comes to.
    private int slotCount = 0;
    private int pageCount = 0;

    // Which page is shown (0-based) and which of its rows is highlighted (0..RowsPerPage-1).
    // The slot a row stands for is currentPage * RowsPerPage + selectedIndex.
    private int currentPage = 0;
    private int selectedIndex = 0;

    private bool initialized = false;

    // Ignore input on the very first frame after Init: the key press that opened this
    // dialog (Space on the home dialog's Save/Load button) is still down this frame, and
    // reading it here would confirm a slot the instant the picker appeared.
    private bool firstFrame = false;

    /// <summary>
    /// Reads the saves off disk and shows the first page. When <paramref name="allowEmpty"/>
    /// is true an empty slot can be confirmed (that is how a save is written into a fresh
    /// slot); when false, confirming an empty slot does nothing and the picker stays up --
    /// what the load flows want, since there is nothing there to read.
    /// </summary>
    public void Init(Action<int> onSlotSelected, bool allowEmpty = true)
    {
        this.OnSlotSelected = onSlotSelected;
        this.allowEmptySlots = allowEmpty;

        labels = new GameObject[] { RecordLabel1, RecordLabel2, RecordLabel3, RecordLabel4 };

        indicatorImage = Indicator != null ? Indicator.GetComponent<Image>() : null;
        indicatorRect = Indicator != null ? Indicator.GetComponent<RectTransform>() : null;

        records = GameRecordManager.GetAllFiles();

        slotCount = GameRecordManager.RecordSlotCount;
        pageCount = Mathf.Max(1, (slotCount + RowsPerPage - 1) / RowsPerPage);

        WireNavButtons();

        currentPage = 0;
        selectedIndex = 0;
        RefreshPage();
        PositionIndicator();

        alphaElapsed = 0f;
        AnimateIndicatorAlpha();

        firstFrame = true;
        initialized = true;
    }

    void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (firstFrame)
        {
            // Swallow the frame Init ran on; act on the next one.
            firstFrame = false;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelection(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Confirm();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            Cancel();
        }

        PositionIndicator();
        AnimateIndicatorAlpha();
    }

    /// <summary>
    /// Steps the highlight by one row. Off the bottom of a page it turns to the next page
    /// with the cursor at the top; off the top it turns back a page with the cursor at the
    /// bottom. At the first and last of the twenty slots the step is simply refused -- there
    /// is no wrap from slot 20 back to slot 1.
    /// </summary>
    private void MoveSelection(int delta)
    {
        int next = selectedIndex + delta;

        if (next >= RowsPerPage)
        {
            if (currentPage < pageCount - 1)
            {
                currentPage++;
                selectedIndex = 0;
                RefreshPage();
            }
        }
        else if (next < 0)
        {
            if (currentPage > 0)
            {
                currentPage--;
                selectedIndex = RowsPerPage - 1;
                RefreshPage();
            }
        }
        else
        {
            selectedIndex = next;
        }
    }

    /// <summary>Turns a whole page from an arrow click: dir -1 back, dir +1 on, cursor to the top.</summary>
    private void TurnPage(int dir)
    {
        if (dir < 0 && currentPage > 0)
        {
            currentPage--;
            selectedIndex = 0;
            RefreshPage();
        }
        else if (dir > 0 && currentPage < pageCount - 1)
        {
            currentPage++;
            selectedIndex = 0;
            RefreshPage();
        }
    }

    /// <summary>
    /// Repaints the four rows for the current page and shows the up / down arrows only where
    /// there is a page to turn to -- no up arrow on the first page, no down arrow on the last.
    /// </summary>
    private void RefreshPage()
    {
        for (int i = 0; i < labels.Length; i++)
        {
            int slotIndex = currentPage * RowsPerPage + i;
            SetLabelText(labels[i], slotIndex < slotCount ? DescribeSlot(slotIndex) : string.Empty);
        }

        if (ButtonUp != null)
        {
            ButtonUp.SetActive(currentPage > 0);
        }
        if (ButtonDown != null)
        {
            ButtonDown.SetActive(currentPage < pageCount - 1);
        }
    }

    /// <summary>The line shown for a slot: "<n>. 第X关：<name>", or "<n>.  ====空====" when empty.</summary>
    private string DescribeSlot(int slotIndex)
    {
        int shownNumber = slotIndex + 1;

        GameRecord record;
        if (records != null && records.TryGetValue(slotIndex, out record) && record != null)
        {
            return string.Format("{0}. 第{1}关：{2}", shownNumber, record.ChapterId, GetChapterName(record.ChapterId));
        }

        return string.Format("{0}.  ====空====", shownNumber);
    }

    /// <summary>
    /// The chapter's own name, the "初试身手" half of "第X关：初试身手". Kept here as a small
    /// table for now -- the only place a chapter's name is needed -- to be lifted into the
    /// localization tables (a "Chapter-XX" key) once every chapter has one written.
    /// </summary>
    private static string GetChapterName(int chapterId)
    {
        switch (chapterId)
        {
            case 1: return "初试身手";
            default: return "";
        }
    }

    private void Confirm()
    {
        int slotIndex = currentPage * RowsPerPage + selectedIndex;
        if (slotIndex < 0 || slotIndex >= slotCount)
        {
            return;
        }

        // Confirming an empty slot does nothing when the caller does not allow it (the load
        // pickers): no callback, no close -- the picker just stays put for another try.
        if (!allowEmptySlots && IsSlotEmpty(slotIndex))
        {
            return;
        }

        Debug.Log("ShoppingRecordDialog: selected slot " + slotIndex);

        if (OnSlotSelected != null)
        {
            OnSlotSelected(slotIndex);
        }
    }

    /// <summary>Whether the slot holds no save -- the same test DescribeSlot uses to label it.</summary>
    private bool IsSlotEmpty(int slotIndex)
    {
        GameRecord record;
        return records == null || !records.TryGetValue(slotIndex, out record) || record == null;
    }

    private void Cancel()
    {
        if (OnSlotSelected != null)
        {
            OnSlotSelected(-1);
        }
    }

    /// <summary>The up / down arrows, clicked, turn the page the same way stepping off an edge does.</summary>
    private void WireNavButtons()
    {
        WireNavButton(ButtonUp, -1);
        WireNavButton(ButtonDown, 1);
    }

    private void WireNavButton(GameObject buttonObject, int dir)
    {
        Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => TurnPage(dir));
    }

    /// <summary>
    /// Slides the indicator onto the highlighted row, its Y taken from IndicatorRowY. Only
    /// the Y is moved; the indicator keeps the X and size the prefab gave it, so it stays a
    /// full-width band over the row.
    /// </summary>
    private void PositionIndicator()
    {
        if (indicatorRect == null || IndicatorRowY == null
            || selectedIndex < 0 || selectedIndex >= IndicatorRowY.Length)
        {
            return;
        }

        Vector2 pos = indicatorRect.anchoredPosition;
        pos.y = IndicatorRowY[selectedIndex];
        indicatorRect.anchoredPosition = pos;
    }

    /// <summary>
    /// Pulses the indicator's opacity IndicatorAlpha -> IndicatorAlphaMin -> IndicatorAlpha
    /// over IndicatorFadePeriod seconds, keeping its colour. A cosine holds it brightest at
    /// the ends of the cycle and dimmest in the middle, so it breathes rather than blinks.
    /// </summary>
    private void AnimateIndicatorAlpha()
    {
        if (indicatorImage == null)
        {
            return;
        }

        alphaElapsed += Time.deltaTime;

        float period = IndicatorFadePeriod > 0.01f ? IndicatorFadePeriod : 2f;
        float t = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * alphaElapsed / period);

        Color color = indicatorImage.color;
        color.a = Mathf.Lerp(IndicatorAlphaMin, IndicatorAlpha, t);
        indicatorImage.color = color;
    }

    /// <summary>
    /// Drops a line onto a row, in the Chinese message font. The row prefab ships with
    /// LiberationSans, which carries no Chinese glyphs, so the whole FZB_Message font asset
    /// is assigned (which pulls its own material with it) rather than only the material --
    /// the same fix the field dialog's messages use.
    /// </summary>
    private static void SetLabelText(GameObject labelObject, string text)
    {
        TextMeshProUGUI textMesh = labelObject != null ? labelObject.GetComponent<TextMeshProUGUI>() : null;
        if (textMesh == null)
        {
            return;
        }

        TMP_FontAsset messageFont = Resources.Load<TMP_FontAsset>(@"Fonts/FontAssets/zh/FZB_Message");
        if (messageFont != null)
        {
            textMesh.font = messageFont;
        }

        textMesh.text = text;
        textMesh.ForceMeshUpdate();
    }
}
