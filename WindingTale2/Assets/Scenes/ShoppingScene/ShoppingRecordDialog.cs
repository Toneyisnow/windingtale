using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WindingTale.Core.Files;
using WindingTale.MapObjects.CreatureIcon;
using WindingTale.UI.Audio;

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
///
/// InitRows turns it into a general picker over the caller's own rows (the church's
/// transfer list): each row a creature's voxel icon and a line, then a second icon and line,
/// laid out on the slot label. The icons are the same 3D models, idle loop and facing the
/// shop's creature list (ShoppingCreaturesDialog) shows, so the canvas is drawn through the
/// camera behind them, as that list does. Paging, the cursor and its sounds are the same; a
/// row's index is what OnSlotSelected reports.
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

    /// <summary>
    /// One row of an InitRows picker: <LeftIcon> <LeftText> -> <RightIcon> <RightText>, the
    /// arrow held at the middle of the screen whatever the left side's length. An icon
    /// is a creature's fight animation id (CreatureDefinition.AnimationId), whose voxel icon
    /// Icons/NNN/Icon_NNN_01..03 is shown; 0 leaves its space empty.
    /// </summary>
    public class ListRow
    {
        public int LeftIconAnimationId;
        public string LeftText;
        public int RightIconAnimationId;
        public string RightText;
    }

    // The caller's rows when opened through InitRows; null for the save / load picker.
    private List<ListRow> customRows = null;

    // Row layout for InitRows: an icon's space is this many times the label's font size, with
    // this gap (canvas units) between an icon and the text beside it.
    private const float RowIconScale = 1.6f;
    private const float RowGap = 6f;
    private const string RowContentName = "RowContent";

    /// <summary>InitRows: how far in front of the camera the voxel icons stand, in world units.</summary>
    public float RowIconDistance = 12f;

    /// <summary>
    /// InitRows: how far behind the icons the canvas is drawn, so the frame and the text sit
    /// behind the voxels (an overlay canvas would paint over them). Kept well in front of the
    /// shop background (plane 32), as ShoppingCreaturesDialog.PanelDistanceBehindIcons is.
    /// </summary>
    public float RowPanelDistanceBehindIcons = 6f;

    /// <summary>InitRows: the icons' yaw once squared up to the camera -- ShoppingCreaturesDialog.IconYaw.</summary>
    public float RowIconYaw = 13.3f;

    /// <summary>InitRows: an icon's height as a share of its space in the row (1 = fills it).</summary>
    public float RowIconFill = 1f;

    /// <summary>
    /// InitRows: how far the icons are dropped below the line's top, as a share of the row
    /// pitch (the gap between two rows' cursor positions, IndicatorRowY).
    /// </summary>
    public float RowIconDrop = 0.5f;

    /// <summary>InitRows: the separator drawn at the middle of the screen between the two halves.</summary>
    private const string RowArrowText = "->";

    // Hundredths of a second each idle frame is held for -- the creature list's value.
    private const int IdleAnimationSpeed = 30;

    /// <summary>One voxel icon of an InitRows row: where it goes in the row and what is shown.</summary>
    private class RowIcon
    {
        public RectTransform Anchor;
        public Transform Holder;
        public GameObject[] Clips;
        public int AnimationId;

        // The model's height at scale 1, measured once it is built, to fit it to its space.
        public float ModelHeight;
    }

    // The icons of the four rows, two to a row; null for the save / load picker.
    private RowIcon[] rowIcons = null;

    // The world-space parent of every row icon. Not under the canvas, so the canvas scale does
    // not reach the models; shown and hidden with this dialog (OnEnable / OnDisable).
    private Transform rowIconRoot = null;

    private Camera rowCamera = null;

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

        records = GameRecordManager.GetAllFiles();
        customRows = null;

        Open(GameRecordManager.RecordSlotCount);
    }

    /// <summary>
    /// Opens the picker over the caller's own rows instead of the save slots, with the same
    /// cursor, paging and sounds. <paramref name="onRowSelected"/> gets the chosen row's index,
    /// or -1 when the picker is backed out of with Esc.
    /// </summary>
    public void InitRows(IList<ListRow> rows, Action<int> onRowSelected)
    {
        this.OnSlotSelected = onRowSelected;
        this.allowEmptySlots = true;

        records = null;
        customRows = rows != null ? new List<ListRow>(rows) : new List<ListRow>();

        SetupRowIcons();
        Open(customRows.Count);
    }

    /// <summary>
    /// Readies the InitRows voxel icons: the canvas is switched to Screen Space - Camera behind
    /// the icon plane, and the world-space root the models are built under is made.
    /// </summary>
    private void SetupRowIcons()
    {
        rowCamera = Camera.main;

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null && rowCamera != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = rowCamera;
            canvas.planeDistance = Mathf.Max(0.1f, RowIconDistance) + Mathf.Max(0f, RowPanelDistanceBehindIcons);
        }

        if (rowIconRoot == null)
        {
            rowIconRoot = new GameObject("RowIcons").transform;
        }

        rowIcons = new RowIcon[RowsPerPage * 2];
    }

    void OnEnable()
    {
        if (rowIconRoot != null)
        {
            rowIconRoot.gameObject.SetActive(true);
        }
    }

    void OnDisable()
    {
        if (rowIconRoot != null)
        {
            rowIconRoot.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (rowIconRoot != null)
        {
            Destroy(rowIconRoot.gameObject);
        }
    }

    /// <summary>
    /// Stands every shown row icon on its space in the row, sized to it and turned like the
    /// creature list's, and steps the idle loop. After the canvas has laid out (LateUpdate), so
    /// the spaces are where they are drawn this frame.
    /// </summary>
    void LateUpdate()
    {
        if (rowIcons == null || rowCamera == null)
        {
            return;
        }

        // Laid out again every frame: the arrow follows the screen's middle, which the canvas
        // only settles on once it has been drawn through the camera.
        if (labels != null)
        {
            foreach (GameObject label in labels)
            {
                LayoutRowContent(label);
            }
        }

        int frame = ((int)(Time.fixedTime * 100) / IdleAnimationSpeed) % 4;
        Quaternion rotation = rowCamera.transform.rotation * Quaternion.Euler(0f, 180f + RowIconYaw, 0f);
        float distance = Mathf.Max(0.1f, RowIconDistance);

        foreach (RowIcon icon in rowIcons)
        {
            if (icon == null || icon.Holder == null || !icon.Holder.gameObject.activeSelf || icon.Anchor == null)
            {
                continue;
            }

            Vector3[] corners = new Vector3[4];
            icon.Anchor.GetWorldCorners(corners);
            Vector3 bottom = rowCamera.WorldToScreenPoint((corners[0] + corners[3]) * 0.5f);
            Vector3 top = rowCamera.WorldToScreenPoint((corners[1] + corners[2]) * 0.5f);

            Vector3 worldBottom = rowCamera.ScreenToWorldPoint(new Vector3(bottom.x, bottom.y, distance));
            Vector3 worldTop = rowCamera.ScreenToWorldPoint(new Vector3(top.x, top.y, distance));

            icon.Holder.position = (worldBottom + worldTop) * 0.5f;
            icon.Holder.rotation = rotation;

            float height = (worldTop - worldBottom).magnitude * Mathf.Max(0f, RowIconFill);
            float scale = icon.ModelHeight > 0.0001f ? height / icon.ModelHeight : 1f;
            icon.Holder.localScale = Vector3.one * scale;

            for (int c = 0; c < icon.Clips.Length; c++)
            {
                bool visible = c == 1 ? (frame == 1 || frame == 3) : frame == c;
                if (icon.Clips[c] != null && icon.Clips[c].activeSelf != visible)
                {
                    icon.Clips[c].SetActive(visible);
                }
            }
        }
    }

    /// <summary>
    /// Shows <paramref name="animationId"/>'s voxel icon on row icon <paramref name="slot"/>,
    /// standing on <paramref name="anchor"/>: the three idle frames, built again only when the
    /// creature changes. 0 hides it.
    /// </summary>
    private void ShowRowIcon(int slot, RectTransform anchor, int animationId)
    {
        if (rowIcons == null || slot < 0 || slot >= rowIcons.Length)
        {
            return;
        }

        RowIcon icon = rowIcons[slot];
        if (icon == null)
        {
            icon = new RowIcon();
            icon.Holder = new GameObject("RowIcon" + slot).transform;
            icon.Holder.SetParent(rowIconRoot, false);
            rowIcons[slot] = icon;
        }

        icon.Anchor = anchor;
        icon.Holder.gameObject.SetActive(animationId > 0);
        if (animationId <= 0 || icon.AnimationId == animationId)
        {
            return;
        }

        for (int c = icon.Holder.childCount - 1; c >= 0; c--)
        {
            Destroy(icon.Holder.GetChild(c).gameObject);
        }

        // Built at scale 1 and unturned, so the measured height is the model's own.
        icon.Holder.localScale = Vector3.one;
        icon.Holder.rotation = Quaternion.identity;

        icon.Clips = new GameObject[3];
        icon.ModelHeight = 0f;
        for (int frame = 0; frame < 3; frame++)
        {
            GameObject clip = new GameObject("clip" + (frame + 1));
            clip.transform.SetParent(icon.Holder, false);

            string iconPath = string.Format("Icons/{0:D3}/Icon_{0:D3}_{1:D2}", animationId, frame + 1);
            GameObject prefab = Resources.Load<GameObject>(iconPath);
            if (prefab != null)
            {
                GameObject model = Instantiate(prefab);
                CreatureMaterial.Apply(model);
                model.transform.SetParent(clip.transform, false);
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
                {
                    icon.ModelHeight = Mathf.Max(icon.ModelHeight, renderer.bounds.size.y);
                }
            }
            else
            {
                Debug.LogWarning("ShoppingRecordDialog: cannot load creature icon " + iconPath);
            }

            icon.Clips[frame] = clip;
        }

        icon.AnimationId = animationId;
    }

    private void Open(int count)
    {
        labels = new GameObject[] { RecordLabel1, RecordLabel2, RecordLabel3, RecordLabel4 };

        indicatorImage = Indicator != null ? Indicator.GetComponent<Image>() : null;
        indicatorRect = Indicator != null ? Indicator.GetComponent<RectTransform>() : null;

        slotCount = count;
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

    // Holding Up / Down keeps stepping: wait this long after the first step, then step
    // once every RepeatInterval seconds until released.
    private const float RepeatInitialDelay = 0.3f;
    private const float RepeatInterval = 0.1f;
    private float repeatTimer = 0f;

    void Update()
    {
        if (!initialized)
        {
            return;
        }

        ReleaseUiSelection();

        if (firstFrame)
        {
            // Swallow the frame Init ran on; act on the next one.
            firstFrame = false;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelectionWithSound(-1);
            repeatTimer = RepeatInitialDelay;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelectionWithSound(1);
            repeatTimer = RepeatInitialDelay;
        }
        else if (Input.GetKey(KeyCode.UpArrow) != Input.GetKey(KeyCode.DownArrow))
        {
            // Held: after RepeatInitialDelay, one more row every RepeatInterval.
            repeatTimer -= Time.deltaTime;
            if (repeatTimer <= 0f)
            {
                repeatTimer = RepeatInterval;
                MoveSelectionWithSound(Input.GetKey(KeyCode.UpArrow) ? -1 : 1);
            }
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
    /// <summary>MoveSelection, with the cursor sound when the highlight (or page) actually moved.</summary>
    private void MoveSelectionWithSound(int delta)
    {
        int pageBefore = currentPage;
        int indexBefore = selectedIndex;
        MoveSelection(delta);
        if (pageBefore != currentPage || indexBefore != selectedIndex)
        {
            SoundEffects.Play(SoundEffect.DialogCursorMove);
        }
    }

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
        else if (currentPage * RowsPerPage + next < slotCount)
        {
            // A part-filled last page (the InitRows lists) stops on its last row.
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
            if (customRows != null)
            {
                SetLabelText(labels[i], string.Empty);
                SetRowContent(i, labels[i], slotIndex < slotCount ? customRows[slotIndex] : null);
            }
            else
            {
                SetLabelText(labels[i], slotIndex < slotCount ? DescribeSlot(slotIndex) : string.Empty);
            }
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
    /// The chapter's own name, the "孤岛" half of "第X关：孤岛". Taken from the original
    /// game's "Title-NN" strings (FlameDragon Resources/Strings/Maps/Chapter-NN.strings,
    /// e.g. "第一章  孤岛") with the "第N章" prefix dropped, since the row already says
    /// 第X关. Kept here as a small table -- the only place a chapter's name is needed.
    /// </summary>
    private static readonly string[] ChapterNames =
    {
        "",                 // 0: no chapter
        "孤岛",
        "罗德镇",
        "往塞拉村途中",
        "塞拉村前",
        "塞拉村",
        "普里兹港",
        "往王城的途中",
        "王城前的战斗",
        "骑士的抉择",
        "洞窟中的激战",
        "幻之森林",
        "北山道",
        "哈斯米尔之战",
        "平原的会战",
        "拉卡湖的激战",
        "冰原之战",
        "血与冰之刃",
        "遥远的彼岸",
        "黑暗中的狙击",
        "死亡般的沉寂",
        "亚述森林",
        "远古的呼唤",
        "向天空之旅",
        "在天空的彼方",
        "火焰的审判",
        "未知的回廊",
        "命运的交会点",
        "探索者",
        "无边的黑暗之中",
        "传说的终章",
    };

    private static string GetChapterName(int chapterId)
    {
        return chapterId >= 0 && chapterId < ChapterNames.Length ? ChapterNames[chapterId] : "";
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

        SoundEffects.Play(SoundEffect.DialogConfirm);

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
        SoundEffects.Play(SoundEffect.DialogCancel);
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

        // The dialog steers by its own Up / Down handling. Left on Automatic, the EventSystem
        // would also walk its UI selection to these arrows (or the title's buttons) on the
        // same key press, and a following Space / Enter would submit that button as well as
        // confirm the highlighted slot -- turning the page under the confirm.
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => TurnPage(dir));
    }

    /// <summary>
    /// Keeps the EventSystem from holding a selected button. A mouse click on an arrow (or
    /// on the title's Load) leaves it selected, and Space / Enter would then submit it in
    /// addition to this dialog's own Confirm.
    /// </summary>
    private static void ReleaseUiSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
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

    // A dynamic font asset over the Chinese source font, built on first use and shared by
    // every picker opened afterwards. See GetRecordFont.
    private static TMP_FontAsset recordFont = null;

    /// <summary>
    /// The font the slot rows are drawn in. The baked FZB_Message atlas only holds the
    /// glyphs of the fixed shop/field messages -- not 第, 关, 空, most digits, nor any
    /// chapter name -- so the rows came out as boxes. A dynamic asset over FangZhengBlack
    /// (the font the chapter atlases are baked from) rasterizes whatever a row asks for.
    /// Falls back to FZB_Message if the source font cannot be loaded.
    /// </summary>
    public static TMP_FontAsset GetRecordFont()
    {
        if (recordFont == null)
        {
            Font sourceFont = Resources.Load<Font>(@"Fonts/FangZhengBlack");
            if (sourceFont != null)
            {
                recordFont = TMP_FontAsset.CreateFontAsset(
                    sourceFont, 64, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    1024, 1024, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
            }
        }

        return recordFont != null ? recordFont : Resources.Load<TMP_FontAsset>(@"Fonts/FontAssets/zh/FZB_Message");
    }

    /// <summary>
    /// Drops a line onto a row, in the Chinese record font. The row prefab ships with
    /// LiberationSans, which carries no Chinese glyphs, so the whole font asset is assigned
    /// (which pulls its own material with it) rather than only the material.
    /// </summary>
    private static void SetLabelText(GameObject labelObject, string text)
    {
        TextMeshProUGUI textMesh = labelObject != null ? labelObject.GetComponent<TextMeshProUGUI>() : null;
        if (textMesh == null)
        {
            return;
        }

        TMP_FontAsset font = GetRecordFont();
        if (font != null)
        {
            textMesh.font = font;
        }

        textMesh.text = text;
        textMesh.ForceMeshUpdate();
    }

    /// <summary>
    /// Lays an InitRows row out on slot label <paramref name="rowIndex"/>: icon, line, icon,
    /// line, left to right from the label's top-left corner, each line in the record font at
    /// the label's own size and colour, each icon a space the voxel model is stood on
    /// (ShowRowIcon / LateUpdate). The pieces are built once per label and reused as the pages
    /// turn; a null row hides them.
    /// </summary>
    private void SetRowContent(int rowIndex, GameObject labelObject, ListRow row)
    {
        TextMeshProUGUI labelText = labelObject != null ? labelObject.GetComponent<TextMeshProUGUI>() : null;
        if (labelText == null)
        {
            return;
        }

        Transform content = labelObject.transform.Find(RowContentName);
        if (content == null && row != null)
        {
            content = BuildRowContent(labelObject.transform);
        }

        if (content != null)
        {
            content.gameObject.SetActive(row != null);
        }

        RectTransform leftSpace = content != null ? content.Find("LeftIcon") as RectTransform : null;
        RectTransform rightSpace = content != null ? content.Find("RightIcon") as RectTransform : null;
        ShowRowIcon(rowIndex * 2, leftSpace, row != null ? row.LeftIconAnimationId : 0);
        ShowRowIcon(rowIndex * 2 + 1, rightSpace, row != null ? row.RightIconAnimationId : 0);

        if (row == null)
        {
            return;
        }

        SetRowText(content.Find("LeftText"), row.LeftText, labelText);
        SetRowText(content.Find("Arrow"), RowArrowText, labelText);
        SetRowText(content.Find("RightText"), row.RightText, labelText);

        LayoutRowContent(labelObject);
    }

    /// <summary>
    /// Places a shown row's pieces: the left icon and line from the label's left edge, the
    /// arrow centred on the middle of the screen (or just after the left line, should that run
    /// past it), and the right icon and line after the arrow. The icons' spaces sit RowIconDrop
    /// of a row pitch below the line's top.
    /// </summary>
    private void LayoutRowContent(GameObject labelObject)
    {
        TextMeshProUGUI labelText = labelObject != null ? labelObject.GetComponent<TextMeshProUGUI>() : null;
        Transform content = labelObject != null ? labelObject.transform.Find(RowContentName) : null;
        if (labelText == null || content == null || !content.gameObject.activeSelf)
        {
            return;
        }

        float iconSize = Mathf.Round(labelText.fontSize * RowIconScale);
        float iconY = iconSize * 0.1f - RowIconDrop * GetRowPitch(labelText);

        float x = PlaceRowIconSpace(content.Find("LeftIcon") as RectTransform, iconSize, 0f, iconY);
        x = PlaceRowText(content.Find("LeftText"), x);

        RectTransform arrow = content.Find("Arrow") as RectTransform;
        float arrowWidth = arrow != null ? arrow.sizeDelta.x : 0f;
        float arrowX = Mathf.Max(x, GetScreenMiddleX(content as RectTransform) - arrowWidth * 0.5f);
        x = PlaceRowText(arrow, arrowX);

        x = PlaceRowIconSpace(content.Find("RightIcon") as RectTransform, iconSize, x, iconY);
        PlaceRowText(content.Find("RightText"), x);
    }

    /// <summary>The distance between two rows (IndicatorRowY), or the label's height when there is no table.</summary>
    private float GetRowPitch(TextMeshProUGUI labelText)
    {
        if (IndicatorRowY != null && IndicatorRowY.Length > 1)
        {
            return Mathf.Abs(IndicatorRowY[1] - IndicatorRowY[0]);
        }
        return labelText.rectTransform.rect.height;
    }

    /// <summary>The middle of the screen, as an x from the left edge of <paramref name="content"/>.</summary>
    private float GetScreenMiddleX(RectTransform content)
    {
        if (content == null)
        {
            return 0f;
        }

        Canvas canvas = content.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        Vector2 middle = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(content, middle, eventCamera, out Vector2 local))
        {
            return 0f;
        }

        return local.x - content.rect.xMin;
    }

    private static Transform BuildRowContent(Transform label)
    {
        GameObject content = new GameObject(RowContentName, typeof(RectTransform));
        RectTransform rect = content.GetComponent<RectTransform>();
        rect.SetParent(label, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // The icons' spaces: empty rects the voxel models are stood on.
        foreach (string name in new[] { "LeftIcon", "RightIcon" })
        {
            GameObject icon = new GameObject(name, typeof(RectTransform));
            SetTopLeft(icon.GetComponent<RectTransform>(), content.transform);
        }

        foreach (string name in new[] { "LeftText", "Arrow", "RightText" })
        {
            GameObject text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            SetTopLeft(text.GetComponent<RectTransform>(), content.transform);

            TextMeshProUGUI textMesh = text.GetComponent<TextMeshProUGUI>();
            textMesh.raycastTarget = false;
            textMesh.alignment = TextAlignmentOptions.TopLeft;
            textMesh.textWrappingMode = TextWrappingModes.NoWrap;
            textMesh.overflowMode = TextOverflowModes.Overflow;
        }

        return content.transform;
    }

    private static void SetTopLeft(RectTransform rect, Transform parent)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
    }

    /// <summary>Puts an icon's space at (<paramref name="x"/>, <paramref name="y"/>) and returns where the next piece starts.</summary>
    private static float PlaceRowIconSpace(RectTransform rect, float size, float x, float y)
    {
        if (rect == null)
        {
            return x;
        }

        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = new Vector2(x, y);

        return x + size + RowGap;
    }

    /// <summary>
    /// Fills one of a row's lines in the record font at the label's size and colour, and sizes
    /// its rect to the text (LayoutRowContent places it).
    /// </summary>
    private static void SetRowText(Transform textTransform, string text, TextMeshProUGUI style)
    {
        TextMeshProUGUI textMesh = textTransform != null ? textTransform.GetComponent<TextMeshProUGUI>() : null;
        if (textMesh == null)
        {
            return;
        }

        TMP_FontAsset font = GetRecordFont();
        if (font != null)
        {
            textMesh.font = font;
        }
        textMesh.fontSize = style.fontSize;
        textMesh.color = style.color;
        textMesh.text = text ?? string.Empty;

        Vector2 size = textMesh.GetPreferredValues(textMesh.text);
        textMesh.rectTransform.sizeDelta = new Vector2(size.x, Mathf.Max(size.y, style.fontSize));
        textMesh.ForceMeshUpdate();
    }

    /// <summary>Puts a line at <paramref name="x"/> and returns where the next piece starts.</summary>
    private static float PlaceRowText(Transform textTransform, float x)
    {
        RectTransform rect = textTransform as RectTransform;
        if (rect == null)
        {
            return x;
        }

        rect.anchoredPosition = new Vector2(x, 0f);
        return x + rect.sizeDelta.x + RowGap;
    }
}
