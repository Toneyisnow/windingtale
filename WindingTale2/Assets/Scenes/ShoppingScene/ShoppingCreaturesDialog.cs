using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Core.Files;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.UI.Dialogs;

/// <summary>
/// The shop's creature picker, pushed over the home dialog when the player chooses Sell.
/// It lays the party out in a 3 x 3 grid (Creature0..8, row-major), each cell showing the
/// creature's animated voxel Idle icon -- the same one the VillageScene cursor wears, only
/// for that creature's animation id -- and its localized name. A translucent indicator sits
/// over the selected cell and the arrow keys walk it; a party larger than nine pages with
/// the up / down buttons, exactly the way ShoppingRecordDialog does.
///
/// Confirming a cell opens the shared CreatureInfoDialog for that creature, reusing the
/// battlefield slide-in / slide-out animation. The picker stays up behind it and only
/// resumes taking input once the info dialog has closed (Esc). Backing out of the picker
/// itself (Esc) reports through OnClosed, which the caller uses to pop back to the home
/// dialog.
/// </summary>
public class ShoppingCreaturesDialog : MonoBehaviour
{
    /// <summary>Which slice of the party the picker shows.</summary>
    public enum CreatureSelectType
    {
        /// <summary>Every party member, the fallen included.</summary>
        All,

        /// <summary>Only the fallen -- those back at 0 HP, waiting to be revived.</summary>
        Dead,
    }

    public GameObject Creature0;

    public GameObject Creature1;

    public GameObject Creature2;

    public GameObject Creature3;

    public GameObject Creature4;

    public GameObject Creature5;

    public GameObject Creature6;

    public GameObject Creature7;

    public GameObject Creature8;

    public GameObject ButtonUp;

    public GameObject ButtonDown;

    public GameObject Indicator;

    /// <summary>
    /// The CreatureInfoDialog prefab opened when a cell is confirmed. Assigned in the
    /// inspector; instantiated under this dialog's canvas so it renders (the prefab carries
    /// no canvas of its own -- on the battlefield it lives under the shared GameCanvas).
    /// </summary>
    public GameObject creatureInfoDialogPrefab = null;

    /// <summary>Size of each voxel icon relative to the map model it is built from.</summary>
    public float IconScale = 0.5f;

    /// <summary>
    /// Nudge of each icon off its cell centre, in world units on the icon plane. Shifts only
    /// the icon, not the cell anchor -- the name label and indicator stay put over the cell.
    /// </summary>
    public float IconOffsetX = 0f;

    public float IconOffsetY = 0f;

    /// <summary>
    /// Yaw each icon is turned by after it is squared up to face the camera, in degrees.
    /// Applied to every icon alike, so they all present the same angle wherever they sit in
    /// the grid (e.g. 30 turns them all a little to one side) rather than fanning out around
    /// the perspective vanishing point the way a single fixed world rotation would.
    /// </summary>
    public float IconYaw = 30f;

    /// <summary>
    /// How far in front of the camera the icon grid sits, in world units. Must be nearer than
    /// the shop background (drawn at plane distance 32) so the icons show in front of it, and
    /// within the camera's far clip. The slot transforms shipped in the prefab sit at
    /// placeholder world coordinates far past the far clip, so the grid is laid out here in
    /// front of the camera instead -- the same way VillageScene places its cursor spots.
    /// </summary>
    public float IconDistance = 12f;

    /// <summary>
    /// How far behind the icon plane the dialog's own canvas is pushed, in world units. The
    /// canvas is switched to Screen Space - Camera at IconDistance + this, so its panel and
    /// labels sit behind the 3 x 3 of world-space icons -- an overlay canvas would always
    /// draw on top of them. Kept well in front of the shop background (plane distance 32).
    /// </summary>
    public float PanelDistanceBehindIcons = 4f;

    /// <summary>Viewport point (0..1) the 3 x 3 grid is centred on; 0.5,0.5 is screen centre.</summary>
    public Vector2 GridViewportCenter = new Vector2(0.5f, 0.34f);

    /// <summary>Viewport gap between neighbouring cells, x across columns and y between rows.</summary>
    public Vector2 GridViewportSpacing = new Vector2(0.24f, 0.15f);

    /// <summary>Point size of the name labels, in the Chinese message font.</summary>
    public float NameFontSize = 28f;

    /// <summary>Horizontal nudge of a cell's name label off the cell centre, in canvas pixels.</summary>
    public float NameOffsetX = 0f;

    /// <summary>How far below a cell's icon its name label sits, in canvas pixels.</summary>
    public float NameOffsetY = -95f;

    /// <summary>Vertical nudge of the indicator off the cell's icon centre, in canvas pixels.</summary>
    public float IndicatorOffsetY = 0f;

    /// <summary>The indicator's brightest opacity, the high point of its pulse.</summary>
    public float IndicatorAlpha = 0.2f;

    /// <summary>The indicator's dimmest opacity, the low point of its pulse.</summary>
    public float IndicatorAlphaMin = 0.05f;

    /// <summary>Seconds for one full IndicatorAlpha -> IndicatorAlphaMin -> IndicatorAlpha pulse.</summary>
    public float IndicatorFadePeriod = 2f;

    /// <summary>
    /// Raised when the player backs out of the picker (Esc). The caller pops this dialog and
    /// returns to the home dialog -- the same way ShoppingRecordDialog reports a cancel.
    /// </summary>
    public Action OnClosed = null;

    // Nine cells to a page, a 3 x 3 grid walked row-major.
    private const int Columns = 3;
    private const int Rows = 3;
    private const int SlotsPerPage = Columns * Rows;

    // Hundredths of a second each idle frame is held for -- the same value VillageScene and
    // CreatureIdleSynced run at, so every icon in the game breathes in step.
    private const int IdleAnimationSpeed = 30;

    private GameObject[] slots = null;

    // The whole party slice being shown, ordered by id (001, 002, ...). A page is nine of
    // these; the cell at grid index i on page p stands for creatures[p * 9 + i].
    private List<FDCreature> creatures = null;

    // Which info dialog this picker opens on a confirm.
    private CreatureInfoType infoType = CreatureInfoType.View;

    // Per-cell built pieces, rebuilt each time the page turns: the three idle-frame holders
    // whose visibility AnimateIcons cycles, and the name label (kept on the canvas so the
    // text stays crisp UI while the icon lives out in the world).
    private readonly GameObject[][] slotClips = new GameObject[SlotsPerPage][];
    private readonly TextMeshProUGUI[] nameLabels = new TextMeshProUGUI[SlotsPerPage];
    private readonly bool[] slotFilled = new bool[SlotsPerPage];

    private Canvas canvas = null;
    private RectTransform canvasRect = null;

    // The camera the icons are placed and squared up against, cached in Init.
    private Camera mainCamera = null;

    private Image indicatorImage = null;
    private RectTransform indicatorRect = null;
    private float alphaElapsed = 0f;

    private int pageCount = 1;
    private int currentPage = 0;

    // Which cell on the current page is highlighted, 0..8. The creature it stands for is
    // currentPage * SlotsPerPage + selectedIndex.
    private int selectedIndex = 0;

    private bool initialized = false;

    // Swallow the frame Init ran on: the Space / Enter that chose Sell on the home dialog is
    // still down this frame and would otherwise confirm a cell the instant the picker opened.
    private bool firstFrame = false;

    // The nested CreatureInfoDialog, if one is open. While it is up the picker keeps drawing
    // but stops taking input, so its keys belong to the info dialog alone.
    private GameObject infoDialogObject = null;
    private bool infoDialogOpen = false;

    /// <summary>
    /// Builds the picker over the given party slice. <paramref name="dialogType"/> is the
    /// CreatureInfoDialog kind a confirmed cell opens (Sell uses SelectAllItem), and
    /// <paramref name="onClosed"/> is raised when the player backs out.
    /// </summary>
    public void Init(GameRecord record, CreatureSelectType creatureType, CreatureInfoType dialogType, Action onClosed)
    {
        this.infoType = dialogType;
        this.OnClosed = onClosed;

        mainCamera = Camera.main;

        slots = new[] { Creature0, Creature1, Creature2, Creature3, Creature4, Creature5, Creature6, Creature7, Creature8 };
        SetupSlotPositions();

        canvas = GetComponentInChildren<Canvas>(true);
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;

        // Draw the canvas through the camera on a plane behind the icons, so the world-space
        // voxel icons sit in front of the panel and labels. As an overlay canvas (its authored
        // mode) it would always paint over the icons, dropping them behind it.
        if (canvas != null && mainCamera != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            canvas.planeDistance = Mathf.Max(0.1f, IconDistance) + Mathf.Max(0f, PanelDistanceBehindIcons);
        }

        indicatorImage = Indicator != null ? Indicator.GetComponent<Image>() : null;
        indicatorRect = Indicator != null ? Indicator.GetComponent<RectTransform>() : null;
        CenterAnchors(indicatorRect);

        creatures = BuildCreatureList(record, creatureType);

        pageCount = Mathf.Max(1, (creatures.Count + SlotsPerPage - 1) / SlotsPerPage);
        currentPage = 0;
        selectedIndex = 0;

        WireNavButtons();
        RefreshPage();

        alphaElapsed = 0f;
        firstFrame = true;
        initialized = true;
    }

    /// <summary>
    /// Lays the nine slot transforms out as a 3 x 3 grid on a plane in front of the camera,
    /// so their voxel icons sit within the far clip and ahead of the shop background. The
    /// grid's placeholder prefab coordinates are overwritten here; the cells are placed by
    /// viewport point so the layout holds at any resolution.
    /// </summary>
    private void SetupSlotPositions()
    {
        Camera camera = mainCamera;
        if (camera == null)
        {
            return;
        }

        for (int i = 0; i < SlotsPerPage; i++)
        {
            if (slots[i] == null)
            {
                continue;
            }

            int column = i % Columns;
            int row = i / Columns;

            float viewportX = GridViewportCenter.x + (column - 1) * GridViewportSpacing.x;
            float viewportY = GridViewportCenter.y - (row - 1) * GridViewportSpacing.y;

            // Distance must be positive, else the grid lands behind the camera and vanishes.
            slots[i].transform.position = camera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, Mathf.Max(0.1f, IconDistance)));
        }
    }

    /// <summary>
    /// Turns the party record into the ordered creature slice to show: every friend for All,
    /// only the fallen (0 HP) for Dead, each rebuilt from its saved record and sorted by id
    /// so the cells read 001, 002, ... in order.
    /// </summary>
    private static List<FDCreature> BuildCreatureList(GameRecord record, CreatureSelectType creatureType)
    {
        List<FDCreature> result = new List<FDCreature>();
        if (record == null || record.Friends == null)
        {
            return result;
        }

        foreach (CreatureMapRecord friend in record.Friends)
        {
            if (friend == null)
            {
                continue;
            }

            if (creatureType == CreatureSelectType.Dead && friend.Hp != 0)
            {
                continue;
            }

            result.Add(GameMapRecordManager.CreateCreatureFromRecord(friend));
        }

        return result.OrderBy(creature => creature.Id).ToList();
    }

    void Update()
    {
        if (!initialized)
        {
            return;
        }

        // While the info dialog is up the icons are hidden (they sit in front of its plane and
        // would otherwise poke through it), so there is nothing to animate; the labels and
        // indicator are left to settle behind it.
        if (!infoDialogOpen)
        {
            AnimateIcons();
            PositionIcons();
        }
        PositionOverlays();
        AnimateIndicatorAlpha();

        if (infoDialogOpen)
        {
            return;
        }

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
    /// to the bottom of the same column); neither wraps past the ends of the party.
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

    /// <summary>How many cells the given page fills, 0..9.</summary>
    private int CountOnPage(int page)
    {
        int remaining = creatures.Count - page * SlotsPerPage;
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
    /// Repaints the nine cells for the current page: each filled cell gets its icon and name,
    /// each empty one is cleared. The up / down buttons show only where there is a page to
    /// turn to -- no up on the first page, no down on the last.
    /// </summary>
    private void RefreshPage()
    {
        for (int i = 0; i < SlotsPerPage; i++)
        {
            int creatureIndex = currentPage * SlotsPerPage + i;
            if (creatureIndex < creatures.Count)
            {
                FDCreature creature = creatures[creatureIndex];
                slotFilled[i] = true;
                BuildSlotIcon(i, creature.Definition.AnimationId);
                SetSlotName(i, creature.Definition.DefinitionId);
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

    /// <summary>
    /// (Re)builds one cell's voxel icon: three idle-frame models under a shared holder, the
    /// same three GameMap.AddCreatureUI hangs off a battlefield creature. AnimateIcons cycles
    /// which one shows; PositionIcons places and squares the holder up to the camera each
    /// frame, so the icon's world position and facing are not set here.
    /// </summary>
    private void BuildSlotIcon(int slotIndex, int animationId)
    {
        GameObject slot = slots[slotIndex];
        if (slot == null)
        {
            return;
        }

        Transform iconRoot = FindOrCreateChild(slot.transform, "IconRoot");
        iconRoot.gameObject.SetActive(true);

        for (int c = iconRoot.childCount - 1; c >= 0; c--)
        {
            Destroy(iconRoot.GetChild(c).gameObject);
        }

        GameObject[] clips = new GameObject[3];
        for (int frame = 0; frame < 3; frame++)
        {
            GameObject holder = new GameObject("clip" + (frame + 1));
            holder.transform.SetParent(iconRoot, false);

            string iconPath = string.Format("Icons/{0:D3}/Icon_{0:D3}_{1:D2}", animationId, frame + 1);
            GameObject prefab = Resources.Load<GameObject>(iconPath);
            if (prefab != null)
            {
                GameObject model = Instantiate(prefab);
                model.transform.SetParent(holder.transform, false);
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("ShoppingCreaturesDialog: cannot load creature icon " + iconPath);
            }

            clips[frame] = holder;
        }

        slotClips[slotIndex] = clips;
    }

    /// <summary>Puts the creature's localized name on the cell's label, in the Chinese font.</summary>
    private void SetSlotName(int slotIndex, int definitionId)
    {
        TextMeshProUGUI label = GetOrCreateNameLabel(slotIndex);
        if (label == null)
        {
            return;
        }

        label.gameObject.SetActive(true);
        label.text = LocalizationManager.GetCreatureString(definitionId).GetLocalizedString();
        label.ForceMeshUpdate();
    }

    /// <summary>Hides an empty cell's icon and name so nothing lingers on a short last page.</summary>
    private void ClearSlot(int slotIndex)
    {
        GameObject slot = slots[slotIndex];
        if (slot != null)
        {
            Transform iconRoot = slot.transform.Find("IconRoot");
            if (iconRoot != null)
            {
                iconRoot.gameObject.SetActive(false);
            }
        }

        if (nameLabels[slotIndex] != null)
        {
            nameLabels[slotIndex].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Shows or hides the filled cells' icons. Used to pull them off screen while the info
    /// dialog is open: the icons stand in front of the dialog's plane and would otherwise
    /// show through it.
    /// </summary>
    private void SetIconsActive(bool active)
    {
        for (int i = 0; i < SlotsPerPage; i++)
        {
            if (slots[i] == null)
            {
                continue;
            }

            Transform iconRoot = slots[i].transform.Find("IconRoot");
            if (iconRoot != null)
            {
                iconRoot.gameObject.SetActive(active && slotFilled[i]);
            }
        }
    }

    /// <summary>
    /// Cycles every filled cell's icon through the idle loop -- frames 01, 02, 03, 02, each
    /// held for IdleAnimationSpeed hundredths of a second -- off Time.fixedTime, so all nine
    /// stay in step with each other and with the rest of the game's icons.
    /// </summary>
    private void AnimateIcons()
    {
        int timeDouble = (int)(Time.fixedTime * 100) / IdleAnimationSpeed;
        int frame = timeDouble % 4;

        for (int i = 0; i < SlotsPerPage; i++)
        {
            if (!slotFilled[i] || slotClips[i] == null)
            {
                continue;
            }

            SetClipVisible(slotClips[i], 0, frame == 0);
            SetClipVisible(slotClips[i], 1, frame == 1 || frame == 3);
            SetClipVisible(slotClips[i], 2, frame == 2);
        }
    }

    private static void SetClipVisible(GameObject[] clips, int clipIndex, bool visible)
    {
        GameObject clip = clips != null && clipIndex < clips.Length ? clips[clipIndex] : null;
        if (clip != null && clip.activeSelf != visible)
        {
            clip.SetActive(visible);
        }
    }

    /// <summary>
    /// Places and squares each filled cell's icon up to the camera every frame. The offset is
    /// applied along the camera's own right / up axes so it always reads as a screen-space
    /// nudge, whatever way the camera is turned; and the facing is a look-at-camera plus a
    /// uniform IconYaw, so every icon presents the same angle to the viewer rather than fanning
    /// out around the perspective vanishing point. Done per frame so the inspector fields tune
    /// it live, the way the name offset already does.
    /// </summary>
    private void PositionIcons()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector3 right = mainCamera.transform.right;
        Vector3 up = mainCamera.transform.up;

        // One rotation shared by every icon: the camera's own orientation (plus a half turn
        // so the front faces the camera) and the uniform IconYaw. Every icon gets this same
        // facing -- parallel, not fanned -- unlike aiming each at the camera's position, which
        // gives a different rotation per cell.
        Quaternion iconRotation = mainCamera.transform.rotation * Quaternion.Euler(0f, 180f + IconYaw, 0f);

        for (int i = 0; i < SlotsPerPage; i++)
        {
            if (!slotFilled[i] || slots[i] == null)
            {
                continue;
            }

            Transform iconRoot = slots[i].transform.Find("IconRoot");
            if (iconRoot == null)
            {
                continue;
            }

            iconRoot.position = slots[i].transform.position + right * IconOffsetX + up * IconOffsetY;
            iconRoot.rotation = iconRotation;

            // Scale must not go negative, which would mirror the mesh (a "negative scale").
            iconRoot.localScale = Vector3.one * Mathf.Max(0f, IconScale);
        }
    }

    /// <summary>
    /// Slides the name labels and the indicator onto their cells. The cells are world-space
    /// anchors and the labels / indicator are canvas UI, so each cell's world position is
    /// projected onto the canvas -- everything then tracks wherever the cells are placed.
    /// </summary>
    private void PositionOverlays()
    {
        for (int i = 0; i < SlotsPerPage; i++)
        {
            if (!slotFilled[i] || nameLabels[i] == null || slots[i] == null)
            {
                continue;
            }

            if (TryGetCanvasPoint(slots[i].transform.position, out Vector2 point))
            {
                nameLabels[i].rectTransform.anchoredPosition = point + new Vector2(NameOffsetX, NameOffsetY);
            }
        }

        if (indicatorRect != null && slotFilled[selectedIndex] && slots[selectedIndex] != null)
        {
            if (TryGetCanvasPoint(slots[selectedIndex].transform.position, out Vector2 point))
            {
                indicatorRect.anchoredPosition = point + new Vector2(0f, IndicatorOffsetY);
            }
        }
    }

    /// <summary>
    /// Projects a world point onto the dialog's canvas. Uses no UI camera for an overlay
    /// canvas (the screen point is already the canvas point), or the canvas camera otherwise.
    /// </summary>
    private bool TryGetCanvasPoint(Vector3 worldPosition, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        if (canvasRect == null || mainCamera == null)
        {
            return false;
        }

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out localPoint);
    }

    /// <summary>
    /// Pulses the indicator's opacity IndicatorAlpha -> IndicatorAlphaMin -> IndicatorAlpha
    /// over IndicatorFadePeriod seconds -- the same breathing highlight the record picker
    /// uses, so the two read alike.
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
        int creatureIndex = currentPage * SlotsPerPage + selectedIndex;
        if (creatureIndex < 0 || creatureIndex >= creatures.Count)
        {
            return;
        }

        OpenInfoDialog(creatures[creatureIndex]);
    }

    private void Cancel()
    {
        OnClosed?.Invoke();
    }

    /// <summary>
    /// Opens the shared CreatureInfoDialog for the confirmed creature, under this dialog's
    /// canvas so it renders. No battle map is in play here, so a null map is passed (the
    /// attribute formulas tolerate it) and the close is handled locally: the instance is
    /// destroyed and the picker resumes taking input. The picker stays visible behind it,
    /// as the field does on the battlefield.
    /// </summary>
    private void OpenInfoDialog(FDCreature creature)
    {
        if (creatureInfoDialogPrefab == null)
        {
            Debug.LogWarning("ShoppingCreaturesDialog: no CreatureInfoDialog prefab to show.");
            return;
        }

        Transform parent = canvas != null ? canvas.transform : this.transform;
        GameObject dialogObject = Instantiate(creatureInfoDialogPrefab, parent, false);

        CreatureInfoDialog dialog = dialogObject.GetComponent<CreatureInfoDialog>();
        if (dialog == null)
        {
            Debug.LogWarning("ShoppingCreaturesDialog: info dialog prefab has no CreatureInfoDialog component.");
            Destroy(dialogObject);
            return;
        }

        infoDialogObject = dialogObject;
        infoDialogOpen = true;
        SetIconsActive(false);

        dialog.Init(creature, infoType, _ => { }, (FDMap)null, () =>
        {
            if (infoDialogObject != null)
            {
                Destroy(infoDialogObject);
                infoDialogObject = null;
            }
            infoDialogOpen = false;
            SetIconsActive(true);
        });
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

    /// <summary>The cell's name label, created under the canvas on first use and then reused.</summary>
    private TextMeshProUGUI GetOrCreateNameLabel(int slotIndex)
    {
        if (nameLabels[slotIndex] != null)
        {
            return nameLabels[slotIndex];
        }

        if (canvasRect == null)
        {
            return null;
        }

        GameObject labelObject = new GameObject("CreatureName_" + slotIndex, typeof(RectTransform));
        labelObject.transform.SetParent(canvasRect, false);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = NameFontSize;
        label.color = Color.white;

        TMP_FontAsset messageFont = Resources.Load<TMP_FontAsset>(@"Fonts/FontAssets/zh/FZB_Message");
        if (messageFont != null)
        {
            label.font = messageFont;
        }

        RectTransform rect = label.rectTransform;
        CenterAnchors(rect);
        rect.sizeDelta = new Vector2(300f, 60f);

        nameLabels[slotIndex] = label;
        return label;
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child;
        }

        GameObject created = new GameObject(name);
        created.transform.SetParent(parent, false);
        created.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        return created.transform;
    }

    /// <summary>Anchors and pivots a rect to the canvas centre, so a projected point places it directly.</summary>
    private static void CenterAnchors(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
