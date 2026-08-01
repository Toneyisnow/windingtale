using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Definitions;

/// <summary>
/// The shop's item picker (Buy), a flat canvas dialog pushed over the home dialog. It reads
/// the shop's stock for (chapterId, shopId) out of Data/Shop.txt -- the same
/// "chapterId shopId count / item0 item1 ... , repeated until chapterId = -1" file that
/// DefinitionStore already parses into ShopDefinitions -- and lays the items out six to a
/// page in a 2 x 3 grid:
///
///     item0   item1
///     item2   item3
///     item4   item5
///
/// Each cell is one row: a type icon (attack / defend / usable, the sprites under
/// Resources/Dialogs, picked by the item's ItemType), the item's localized name, and its
/// attribute line ("AP+5", "DP+10", ...). Everything is a canvas element -- the six Item
/// slots are RectTransforms under the dialog's Canvas, and each slot's icon Image and two
/// TextMeshProUGUI labels are built as UI children under it at Init, then refilled each time
/// the page turns.
///
/// The arrow keys walk a translucent highlight over the cells; stepping off the bottom or the
/// top of a page turns to the next / previous one, and the up / down buttons turn it too --
/// exactly the way ShoppingCreaturesDialog and ShoppingRecordDialog do. Space / Enter confirms
/// the highlighted item through OnItemSelected; Esc backs out through OnClosed, which the
/// caller uses to pop back to the home dialog.
/// </summary>
public class ShoppingItemsDialog : MonoBehaviour
{
    // The six slots, row-major: Item0 Item1 / Item2 Item3 / Item4 Item5. Each is an empty
    // RectTransform under the Canvas; its icon and labels are built as children at Init.
    public GameObject Item0;

    public GameObject Item1;

    public GameObject Item2;

    public GameObject Item3;

    public GameObject Item4;

    public GameObject Item5;


    public GameObject ButtonUp;

    public GameObject ButtonDown;

    /// <summary>The translucent highlight band (a canvas Image), moved onto the selected cell each frame.</summary>
    public GameObject Indicator;

    /// <summary>World-space size of each type icon, in canvas pixels.</summary>
    public float IconSize = 30f;

    /// <summary>Icon's x offset off its cell centre, in canvas pixels (it sits to the left of the name).</summary>
    public float IconOffsetX = -105f;

    /// <summary>Point size of the item-name label.</summary>
    public float NameFontSize = 20f;

    /// <summary>Left edge of the name label off the cell centre; the name is left-anchored and grows right.</summary>
    public float NameOffsetX = -80f;

    /// <summary>
    /// Point size of the attribute, shown in parentheses right after the name -- "阔剑（AP+20）".
    /// The attribute (and its parentheses) is drawn as an inline &lt;size&gt; run inside the name
    /// label, so it reads smaller than the name it trails.
    /// </summary>
    public float AttributeFontSize = 14f;

    /// <summary>Point size of the price label ("250元").</summary>
    public float PriceFontSize = 18f;

    /// <summary>
    /// Right edge of the price label off the cell centre, in canvas pixels. The price is
    /// right-anchored, so it sits at the cell's right end and the name / attribute grow up to
    /// it from the left.
    /// </summary>
    public float PriceOffsetX = 125f;

    /// <summary>Nudge of the highlight band off the selected cell's centre, in canvas pixels.</summary>
    public float IndicatorOffsetX = 0f;

    public float IndicatorOffsetY = 0f;

    /// <summary>The indicator's brightest opacity, the high point of its pulse.</summary>
    public float IndicatorAlpha = 0.2f;

    /// <summary>The indicator's dimmest opacity, the low point of its pulse.</summary>
    public float IndicatorAlphaMin = 0.05f;

    /// <summary>Seconds for one full IndicatorAlpha -> IndicatorAlphaMin -> IndicatorAlpha pulse.</summary>
    public float IndicatorFadePeriod = 2f;

    /// <summary>Raised with the chosen item's id when a cell is confirmed (Space / Enter).</summary>
    public Action<int> OnItemSelected = null;

    /// <summary>
    /// Raised when the player backs out of the picker (Esc). The caller pops this dialog and
    /// returns to the home dialog -- the same way ShoppingCreaturesDialog reports a cancel.
    /// </summary>
    public Action OnClosed = null;

    // Six cells to a page, a 2 x 3 grid walked row-major.
    private const int Columns = 2;
    private const int Rows = 3;
    private const int SlotsPerPage = Columns * Rows;

    private const string MessageFontPath = @"Fonts/FontAssets/zh/FZB_Message";

    private GameObject[] slots = null;
    private readonly RectTransform[] slotRects = new RectTransform[SlotsPerPage];

    // The whole shop stock being shown, in file order. A page is six of these; the cell at
    // grid index i on page p stands for items[p * 6 + i].
    private List<ItemDefinition> items = null;

    // Per-cell built pieces, created once at Init and reused: the icon Image, the name label
    // (which carries the parenthesised attribute inline) and the price label, all UI children
    // of the slot. RefreshPage fills or hides them per page.
    private readonly Image[] slotIcons = new Image[SlotsPerPage];
    private readonly TextMeshProUGUI[] nameTexts = new TextMeshProUGUI[SlotsPerPage];
    private readonly TextMeshProUGUI[] priceTexts = new TextMeshProUGUI[SlotsPerPage];
    private readonly bool[] slotFilled = new bool[SlotsPerPage];

    // The highlight's Image and RectTransform, cached in Init so the per-frame pulse and
    // reposition do not re-fetch them. The elapsed clock drives the alpha pulse.
    private Image indicatorImage = null;
    private RectTransform indicatorRect = null;
    private float alphaElapsed = 0f;

    private int pageCount = 1;
    private int currentPage = 0;

    // Which cell on the current page is highlighted, 0..5. The item it stands for is
    // currentPage * SlotsPerPage + selectedIndex.
    private int selectedIndex = 0;

    private bool initialized = false;

    // Swallow the frame Init ran on: the Space / Enter that chose Buy on the home dialog is
    // still down this frame and would otherwise confirm a cell the instant the picker opened.
    private bool firstFrame = false;

    /// <summary>
    /// Loads the shop's stock for (chapterId, shopId) and shows the first page. shopId is the
    /// Shop.txt second column (1 = Amor, 2 = Item, 3 = Secret), matched one-to-one against the
    /// ShopDefinition keys DefinitionStore builds from that file.
    /// </summary>
    public void Init(int chapterId, int shopId)
    {
        slots = new GameObject[] { Item0, Item1, Item2, Item3, Item4, Item5 };

        indicatorImage = Indicator != null ? Indicator.GetComponent<Image>() : null;
        indicatorRect = Indicator != null ? Indicator.GetComponent<RectTransform>() : null;

        items = LoadShopItems(chapterId, shopId);
        pageCount = Mathf.Max(1, (items.Count + SlotsPerPage - 1) / SlotsPerPage);

        BuildSlots();
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

    /// <summary>
    /// The shop's item definitions for (chapterId, shopId), resolved from the id list on its
    /// ShopDefinition. DefinitionStore reads and caches Data/Shop.txt itself (the file the
    /// task describes), so this reuses that rather than re-opening the file. Ids that resolve
    /// to no definition are logged and skipped.
    /// </summary>
    private static List<ItemDefinition> LoadShopItems(int chapterId, int shopId)
    {
        List<ItemDefinition> result = new List<ItemDefinition>();

        ShopDefinition shop = DefinitionStore.Instance.GetShopDefinition(chapterId, (ShopType)shopId);
        if (shop == null || shop.Items == null)
        {
            Debug.LogWarning(string.Format("ShoppingItemsDialog: no shop stock for chapter {0} shop {1}.", chapterId, shopId));
            return result;
        }

        foreach (int itemId in shop.Items)
        {
            ItemDefinition def = DefinitionStore.Instance.GetItemDefinition(itemId);
            if (def != null)
            {
                result.Add(def);
            }
            else
            {
                Debug.LogWarning("ShoppingItemsDialog: unknown item id " + itemId);
            }
        }

        return result;
    }

    void Update()
    {
        if (!initialized)
        {
            return;
        }

        PositionIndicator();
        AnimateIndicatorAlpha();

        if (firstFrame)
        {
            firstFrame = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelection(0, -1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelection(0, 1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelection(-1, 0);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelection(1, 0);
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
    }

    /// <summary>
    /// Walks the highlight one grid step. Left / right stay on the row and refuse a step off
    /// its edge or onto an empty cell. Down off the last filled row turns to the next page
    /// (cursor to the top of the same column), up off the first row turns back a page (cursor
    /// to the bottom of the same column); neither wraps past the ends of the stock.
    /// </summary>
    private void MoveSelection(int deltaColumn, int deltaRow)
    {
        int column = selectedIndex % Columns;
        int row = selectedIndex / Columns;
        int filledThisPage = CountOnPage(currentPage);

        if (deltaColumn != 0)
        {
            int nextColumn = column + deltaColumn;
            if (nextColumn < 0 || nextColumn >= Columns)
            {
                return;
            }

            int target = row * Columns + nextColumn;
            if (target >= filledThisPage)
            {
                return;
            }

            selectedIndex = target;
            return;
        }

        if (deltaRow < 0)
        {
            if (row > 0)
            {
                selectedIndex -= Columns;
                return;
            }

            // Off the top: the previous page is always full (it is not the last), so its
            // bottom row exists in every column.
            if (currentPage > 0)
            {
                currentPage--;
                RefreshPage();
                selectedIndex = ClampToPage((Rows - 1) * Columns + column, currentPage);
            }
            return;
        }

        int below = (row + 1) * Columns + column;
        if (row < Rows - 1 && below < filledThisPage)
        {
            selectedIndex = below;
            return;
        }

        // Off the bottom (last row, or nothing filled below on this page): on to the next
        // page, same column at the top, clamped to what that page actually holds.
        if (currentPage < pageCount - 1)
        {
            currentPage++;
            RefreshPage();
            selectedIndex = ClampToPage(column, currentPage);
        }
    }

    /// <summary>Turns a whole page from an arrow-button click, cursor to the top-left.</summary>
    private void TurnPage(int direction)
    {
        if (direction < 0 && currentPage > 0)
        {
            currentPage--;
            selectedIndex = 0;
            RefreshPage();
        }
        else if (direction > 0 && currentPage < pageCount - 1)
        {
            currentPage++;
            selectedIndex = 0;
            RefreshPage();
        }
    }

    /// <summary>How many cells the given page fills, 0..6.</summary>
    private int CountOnPage(int page)
    {
        int remaining = items.Count - page * SlotsPerPage;
        return Mathf.Clamp(remaining, 0, SlotsPerPage);
    }

    /// <summary>Keeps a cell index inside what its page actually holds.</summary>
    private int ClampToPage(int index, int page)
    {
        int filled = CountOnPage(page);
        if (index >= filled)
        {
            index = filled - 1;
        }
        return Mathf.Max(0, index);
    }

    /// <summary>
    /// Builds the icon Image, the name label (which carries the parenthesised attribute inline)
    /// and the right-anchored price label once for every slot, as UI children under the slot's
    /// RectTransform. A slot with no RectTransform (a misconfigured prefab) is left empty and
    /// simply shows nothing.
    /// </summary>
    private void BuildSlots()
    {
        for (int i = 0; i < SlotsPerPage; i++)
        {
            slotRects[i] = slots[i] != null ? slots[i].GetComponent<RectTransform>() : null;
            if (slotRects[i] == null)
            {
                continue;
            }

            slotIcons[i] = CreateIcon(slotRects[i]);
            nameTexts[i] = CreateLabel(slotRects[i], "Name", NameFontSize, NameOffsetX, 200f, 0f, TextAlignmentOptions.Left);
            priceTexts[i] = CreateLabel(slotRects[i], "Price", PriceFontSize, PriceOffsetX, 110f, 1f, TextAlignmentOptions.Right);
        }
    }

    /// <summary>The slot's type-icon Image, a square anchored to the cell centre at IconOffsetX.</summary>
    private Image CreateIcon(RectTransform parent)
    {
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(IconSize, IconSize);
        rect.anchoredPosition = new Vector2(IconOffsetX, 0f);

        Image image = iconObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    /// <summary>
    /// A text label under a slot, in the Chinese message font, anchored at the cell centre.
    /// pivotX / alignment place it: the name is left-anchored (pivot 0, grows right), the price
    /// right-anchored (pivot 1, grows left), so the two meet in the middle of the cell.
    /// </summary>
    private TextMeshProUGUI CreateLabel(RectTransform parent, string name, float fontSize, float offsetX, float width, float pivotX, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(pivotX, 0.5f);
        rect.sizeDelta = new Vector2(width, Mathf.Max(24f, fontSize + 8f));
        rect.anchoredPosition = new Vector2(offsetX, 0f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;

        // The name label carries the attribute as an inline <size> run, so rich text must stay
        // on (it is TMP's default, set here so the dependency is explicit).
        text.richText = true;

        // The prefab has no font of its own; LiberationSans (TMP's default) carries no Chinese
        // glyphs, so assign the whole FZB_Message asset (which pulls its own atlas material
        // with it) -- the same fix the other shop dialogs use, or the line comes out as tofu.
        TMP_FontAsset messageFont = Resources.Load<TMP_FontAsset>(MessageFontPath);
        if (messageFont != null)
        {
            text.font = messageFont;
        }

        return text;
    }

    /// <summary>
    /// Repaints the six cells for the current page: each filled cell gets its icon, name and
    /// attribute, each empty one is hidden. The up / down buttons show only where there is a
    /// page to turn to -- no up on the first page, no down on the last.
    /// </summary>
    private void RefreshPage()
    {
        for (int i = 0; i < SlotsPerPage; i++)
        {
            int itemIndex = currentPage * SlotsPerPage + i;
            if (itemIndex < items.Count)
            {
                slotFilled[i] = true;
                FillSlot(i, items[itemIndex]);
            }
            else
            {
                slotFilled[i] = false;
                ClearSlot(i);
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

    /// <summary>Drops one item's icon, "name（attribute）" line and price onto a cell and shows them.</summary>
    private void FillSlot(int slotIndex, ItemDefinition item)
    {
        if (slotIcons[slotIndex] != null)
        {
            Sprite sprite = ItemIconHelper.LoadIcon(item);
            slotIcons[slotIndex].sprite = sprite;
            slotIcons[slotIndex].enabled = sprite != null;
            slotIcons[slotIndex].gameObject.SetActive(true);
        }

        if (nameTexts[slotIndex] != null)
        {
            nameTexts[slotIndex].gameObject.SetActive(true);
            nameTexts[slotIndex].text = BuildNameText(item);
            nameTexts[slotIndex].ForceMeshUpdate();
        }

        if (priceTexts[slotIndex] != null)
        {
            priceTexts[slotIndex].gameObject.SetActive(true);
            priceTexts[slotIndex].text = string.Format("{0}元", item.Price);
            priceTexts[slotIndex].ForceMeshUpdate();
        }
    }

    /// <summary>
    /// The name label's rich text: the item name -- off the definition (Strings/Item.strings,
    /// which covers every id, unlike the incomplete CommonStrings "Item-NNN" table) -- followed
    /// by its attribute in full-width parentheses as a smaller inline &lt;size&gt; run, e.g.
    /// "阔剑（AP+20）". An item with no attribute (a special / money item) shows the bare name,
    /// with no empty parentheses.
    /// </summary>
    private string BuildNameText(ItemDefinition item)
    {
        string name = item.Name != null ? item.Name : string.Empty;
        string attribute = item.ToAttributeString();
        if (string.IsNullOrEmpty(attribute))
        {
            return name;
        }

        int attributeSize = Mathf.Max(1, Mathf.RoundToInt(AttributeFontSize));
        return string.Format("{0} <size={1}>{2}</size>", name, attributeSize, attribute);
    }

    /// <summary>Hides an empty cell's icon and labels so nothing lingers on a short last page.</summary>
    private void ClearSlot(int slotIndex)
    {
        if (slotIcons[slotIndex] != null)
        {
            slotIcons[slotIndex].gameObject.SetActive(false);
        }
        if (nameTexts[slotIndex] != null)
        {
            nameTexts[slotIndex].gameObject.SetActive(false);
        }
        if (priceTexts[slotIndex] != null)
        {
            priceTexts[slotIndex].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Slides the highlight onto the selected filled cell (its cell centre plus the indicator
    /// offset) and hides it when the selected cell is empty. The pulse is driven separately in
    /// AnimateIndicatorAlpha.
    /// </summary>
    private void PositionIndicator()
    {
        if (indicatorRect == null)
        {
            return;
        }

        bool show = selectedIndex >= 0 && selectedIndex < SlotsPerPage
            && slotFilled[selectedIndex] && slotRects[selectedIndex] != null;
        if (indicatorImage != null)
        {
            indicatorImage.enabled = show;
        }
        if (!show)
        {
            return;
        }

        // The indicator and the slots are siblings under the Canvas, both anchored at its
        // centre, so their anchoredPositions share one coordinate system and the highlight
        // lands on the cell by copying its position.
        indicatorRect.anchoredPosition =
            slotRects[selectedIndex].anchoredPosition + new Vector2(IndicatorOffsetX, IndicatorOffsetY);
    }

    /// <summary>
    /// Pulses the indicator's opacity IndicatorAlpha -> IndicatorAlphaMin -> IndicatorAlpha
    /// over IndicatorFadePeriod seconds, keeping its colour -- the same breathing highlight the
    /// record and creature pickers use, so the three read alike.
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

        // Opacity is 0..1; clamp so an out-of-range field cannot blow the highlight out.
        Color color = indicatorImage.color;
        color.a = Mathf.Lerp(Mathf.Clamp01(IndicatorAlphaMin), Mathf.Clamp01(IndicatorAlpha), t);
        indicatorImage.color = color;
    }

    private void Confirm()
    {
        int itemIndex = currentPage * SlotsPerPage + selectedIndex;
        if (itemIndex < 0 || itemIndex >= items.Count)
        {
            return;
        }

        int itemId = items[itemIndex].ItemId;
        Debug.Log("ShoppingItemsDialog: selected item " + itemId);
        OnItemSelected?.Invoke(itemId);
    }

    private void Cancel()
    {
        OnClosed?.Invoke();
    }

    /// <summary>The up / down buttons turn the page the same way stepping off an edge does.</summary>
    private void WireNavButtons()
    {
        WireNavButton(ButtonUp, -1);
        WireNavButton(ButtonDown, 1);
    }

    private void WireNavButton(GameObject buttonObject, int direction)
    {
        Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => TurnPage(direction));
    }
}
