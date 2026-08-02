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
/// Everything a cell is made of lives in one world-space GameObject: the slot carries its
/// voxel icon and a 3D TextMeshPro name label as children, at fixed local offsets, and the
/// nine slots are fixed local cells of a single Grid placed in front of the camera. Because
/// icon, name and indicator all share the one 3D coordinate system, they stay locked
/// together at any resolution -- no per-frame projection of world points onto a canvas, and
/// no second coordinate system to keep in step. Only the shop's background frame and the
/// page buttons stay on the dialog's canvas, drawn on a plane behind the icons.
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

    /// <summary>
    /// The authored highlight sprite, a canvas Image in the prefab. Its sprite is lifted into
    /// a world-space SpriteRenderer at Init (so the highlight lives in the same 3D space as
    /// the icons it frames) and this canvas Image is then hidden.
    /// </summary>
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
    /// Nudge of each icon off its cell centre, in world units on the grid plane (local x
    /// right, local y up). Shifts only the icon within its cell; the name label and the
    /// indicator keep their own offsets, so the three can be spaced apart inside the cell.
    /// </summary>
    public float IconOffsetX = 0f;

    public float IconOffsetY = 0.4f;

    /// <summary>
    /// Yaw each icon is turned by after it is squared up to face the camera, in degrees.
    /// Applied to every icon alike (the grid already faces the camera, so this is a shared
    /// local turn), so they all present the same angle wherever they sit in the grid.
    /// </summary>
    public float IconYaw = 13.3f;

    /// <summary>
    /// How far in front of the camera the grid sits, in world units. Must be nearer than the
    /// shop background (drawn at plane distance 32) so the icons show in front of it, and
    /// within the camera's far clip. The grid is placed here in front of the camera each
    /// frame -- the same way VillageScene places its cursor spots.
    /// </summary>
    public float IconDistance = 12f;

    /// <summary>
    /// How far behind the icon plane the dialog's own canvas is pushed, in world units. The
    /// canvas is switched to Screen Space - Camera at IconDistance + this, so its background
    /// frame and page buttons sit behind the world-space icons -- an overlay canvas would
    /// always draw on top of them. Kept well in front of the shop background (plane 32).
    /// </summary>
    public float PanelDistanceBehindIcons = 6f;

    /// <summary>Viewport point (0..1) the whole 3 x 3 grid is centred on; 0.5,0.5 is screen centre.</summary>
    public Vector2 GridViewportCenter = new Vector2(0.5f, 0.46f);

    /// <summary>
    /// Gap across columns between neighbouring cells, in world units. World units (not a
    /// viewport fraction) so the columns stay an equal distance apart whatever the aspect
    /// ratio -- they no longer spread on a wider screen.
    /// </summary>
    public float GridGapX = 8.4f;

    /// <summary>Gap between rows of cells, in world units.</summary>
    public float GridGapY = 1.8f;

    /// <summary>Point size of the name labels (3D TextMeshPro, rendered in world space).</summary>
    public float NameFontSize = 12f;

    /// <summary>
    /// Left edge of a cell's name label, in world units off the cell centre. The label is
    /// left-anchored, so this fixes where the name starts and the text only ever grows to the
    /// right -- the left padding stays put whatever the name's length.
    /// </summary>
    public float NameOffsetX = -1.4f;

    /// <summary>How far below a cell's centre its name label sits, in world units.</summary>
    public float NameOffsetY = -1.6f;

    /// <summary>World-space size of the highlight sprite; scales the lifted indicator sprite.</summary>
    public float IndicatorScale = 2f;

    /// <summary>Vertical nudge of the indicator off the cell centre, in world units.</summary>
    public float IndicatorOffsetY = 0.2f;

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

    /// <summary>
    /// Raised when the nested CreatureInfoDialog opens and, again, when it finally closes back
    /// to the picker (not across an Equip reopen, which stays on the info page). The shop uses
    /// this to pull its money-bar HUD behind the info dialog while it is up -- the bar is a
    /// screen-space overlay that would otherwise draw over the dialog.
    /// </summary>
    public Action OnInfoDialogOpened = null;

    public Action OnInfoDialogClosed = null;

    // Nine cells to a page, a 3 x 3 grid walked row-major.
    private const int Columns = 3;
    private const int Rows = 3;
    private const int SlotsPerPage = Columns * Rows;

    // Hundredths of a second each idle frame is held for -- the same value VillageScene and
    // CreatureIdleSynced run at, so every icon in the game breathes in step.
    private const int IdleAnimationSpeed = 30;

    // How far behind the icon plane the highlight sprite sits, in world units. Behind, so the
    // opaque voxel draws over it and it reads as a glow framing the creature rather than a
    // tint laid over its face.
    private const float IndicatorBehindIcons = 0.2f;

    private GameObject[] slots = null;

    // The whole party slice being shown, ordered by id (001, 002, ...). A page is nine of
    // these; the cell at grid index i on page p stands for creatures[p * 9 + i].
    private List<FDCreature> creatures = null;

    // Which info dialog this picker opens on a confirm.
    private CreatureInfoType infoType = CreatureInfoType.View;

    // Set in the Buy flow: confirming a cell reports the chosen creature here rather than
    // opening the info dialog, so the shop can hand its bought item to that creature. When
    // null (the Sell flow) a confirm opens the info dialog the old way.
    private Action<FDCreature> onCreatureConfirmed = null;

    // Set when a confirmed cell should open the info dialog to pick one of that creature's
    // items -- the Give flow's "whose item?" step and the Equip flow both use this. The chosen
    // creature and item index are reported here; only an actual pick fires it, backing out of
    // the info dialog just returns to the picker. Left null the info dialog's selection is
    // ignored (the Sell flow).
    private Action<FDCreature, int> onItemSelected = null;

    // Set with onItemSelected: when true the info dialog is reopened for the same creature
    // after each pick, so the player stays on the item page and can act again (Equip re-equips
    // in place). When false the pick closes the info dialog back to the picker (Give reads the
    // one item and moves on).
    private bool reopenInfoAfterSelect = false;

    // Per-cell built pieces, rebuilt each time the page turns: the three idle-frame holders
    // whose visibility AnimateIcons cycles, and the world-space name label, both parented
    // under the slot so a cell is one GameObject carrying everything it shows.
    private readonly GameObject[][] slotClips = new GameObject[SlotsPerPage][];
    private readonly TextMeshPro[] nameTexts = new TextMeshPro[SlotsPerPage];
    private readonly bool[] slotFilled = new bool[SlotsPerPage];

    // The one rigid grid the nine slots are fixed local cells of; placed in front of the
    // camera each frame, so the whole cell layout moves and turns as a single unit.
    private Transform gridRoot = null;

    // The camera the grid is placed and squared up against, cached in Init.
    private Camera mainCamera = null;

    // The world-space highlight lifted from the authored canvas Indicator, moved onto the
    // selected cell each frame.
    private SpriteRenderer indicatorRenderer = null;
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
    ///
    /// <paramref name="onCreatureConfirmed"/> switches the picker into the Buy flow's
    /// "pick who gets it" role: a confirmed cell reports that creature here instead of opening
    /// the info dialog, and <paramref name="dialogType"/> is then unused. Left null (the Sell
    /// flow) a confirm opens the info dialog the old way.
    ///
    /// <paramref name="onItemSelected"/> switches the picker into a "pick a creature, then one
    /// of its items" role (the Give flow's "whose item?" step, and the Equip flow): a confirmed
    /// cell opens the info dialog (of <paramref name="dialogType"/>) to pick an item, and the
    /// creature and chosen item index are reported here. It is only consulted when
    /// <paramref name="onCreatureConfirmed"/> is null. With <paramref name="reopenInfoAfterSelect"/>
    /// the info dialog reopens after each pick so the player stays on the item page (Equip);
    /// without it the pick closes back to the picker (Give reads the one item and moves on).
    /// </summary>
    public void Init(GameRecord record, CreatureSelectType creatureType, CreatureInfoType dialogType, Action onClosed, Action<FDCreature> onCreatureConfirmed = null, Action<FDCreature, int> onItemSelected = null, bool reopenInfoAfterSelect = false)
    {
        this.infoType = dialogType;
        this.OnClosed = onClosed;
        this.onCreatureConfirmed = onCreatureConfirmed;
        this.onItemSelected = onItemSelected;
        this.reopenInfoAfterSelect = reopenInfoAfterSelect;

        mainCamera = Camera.main;

        slots = new[] { Creature0, Creature1, Creature2, Creature3, Creature4, Creature5, Creature6, Creature7, Creature8 };

        BuildGrid();
        SetupPanelCanvas();
        SetupIndicator();

        creatures = BuildCreatureList(record, creatureType);

        pageCount = Mathf.Max(1, (creatures.Count + SlotsPerPage - 1) / SlotsPerPage);
        currentPage = 0;
        selectedIndex = 0;

        WireNavButtons();
        RefreshPage();

        // Place everything once here so the first drawn frame is already laid out, rather
        // than flashing at the slots' placeholder positions before the first Update.
        PositionGrid();

        alphaElapsed = 0f;
        firstFrame = true;
        initialized = true;
    }

    /// <summary>
    /// Gathers the nine slot transforms under one Grid node so the cells move as a rigid
    /// unit. The slots keep their serialized references (Creature0..8), only their parent
    /// changes; their local cell positions are set every frame in PositionGrid.
    /// </summary>
    private void BuildGrid()
    {
        GameObject gridObject = new GameObject("Grid");
        gridRoot = gridObject.transform;
        gridRoot.SetParent(this.transform, false);

        for (int i = 0; i < SlotsPerPage; i++)
        {
            if (slots[i] == null)
            {
                continue;
            }

            slots[i].transform.SetParent(gridRoot, false);
        }
    }

    /// <summary>
    /// Draws the dialog's own canvas -- the background frame and the page buttons -- through
    /// the camera on a plane behind the icons, so the world-space voxel icons sit in front of
    /// it. As an overlay canvas (its authored mode) the frame would paint over the icons.
    /// </summary>
    private void SetupPanelCanvas()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null && mainCamera != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            canvas.planeDistance = Mathf.Max(0.1f, IconDistance) + Mathf.Max(0f, PanelDistanceBehindIcons);
        }
    }

    /// <summary>
    /// Lifts the authored highlight sprite off the canvas Indicator into a world-space
    /// SpriteRenderer under the grid, and hides the canvas Indicator. The highlight then
    /// lives in the same 3D space as the icons and can be locked onto the selected cell.
    /// </summary>
    private void SetupIndicator()
    {
        Sprite sprite = null;
        if (Indicator != null)
        {
            Image image = Indicator.GetComponent<Image>();
            if (image != null)
            {
                sprite = image.sprite;
            }

            Indicator.SetActive(false);
        }

        GameObject indicatorObject = new GameObject("SelectIndicatorWorld");
        indicatorObject.transform.SetParent(gridRoot, false);

        indicatorRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        indicatorRenderer.sprite = sprite;

        Color color = Color.white;
        color.a = Mathf.Clamp01(IndicatorAlphaMin);
        indicatorRenderer.color = color;
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

        // While the info dialog is up the whole grid is hidden (its icons and names stand in
        // front of the dialog's plane and would otherwise poke through it), so there is
        // nothing to lay out or animate.
        if (!infoDialogOpen)
        {
            PositionGrid();
            AnimateIcons();
        }
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
    /// which one shows; PositionGrid places and squares the holder up each frame, so the
    /// icon's local position and facing are not set here.
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

    /// <summary>Puts the creature's localized name on the cell's world-space label, in the Chinese font.</summary>
    private void SetSlotName(int slotIndex, int definitionId)
    {
        TextMeshPro text = GetOrCreateNameText(slotIndex);
        if (text == null)
        {
            return;
        }

        text.gameObject.SetActive(true);
        text.text = LocalizationManager.GetCreatureString(definitionId).GetLocalizedString();
        text.ForceMeshUpdate();
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

        if (nameTexts[slotIndex] != null)
        {
            nameTexts[slotIndex].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Shows or hides the whole grid -- icons, names and the highlight. Used to pull it off
    /// screen while the info dialog is open: the cells stand in front of the dialog's plane
    /// and would otherwise show through it.
    /// </summary>
    private void SetGridActive(bool active)
    {
        if (gridRoot != null)
        {
            gridRoot.gameObject.SetActive(active);
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
    /// Places the grid in front of the camera and lays its cells out. The grid is turned to
    /// face the camera, so each cell's local x / y read as screen right / up; the nine slots
    /// take fixed local cell positions (equal world spacing), and each filled cell's icon and
    /// name take their own fixed local offsets. Because all of this is one rigid local layout
    /// under the grid, icon and name never drift apart at a different resolution. Run per
    /// frame so the inspector fields tune it live and the layout follows the camera.
    /// </summary>
    private void PositionGrid()
    {
        if (mainCamera == null || gridRoot == null)
        {
            return;
        }

        // Distance must be positive, else the grid lands behind the camera and vanishes.
        gridRoot.position = mainCamera.ViewportToWorldPoint(
            new Vector3(GridViewportCenter.x, GridViewportCenter.y, Mathf.Max(0.1f, IconDistance)));
        gridRoot.rotation = mainCamera.transform.rotation;

        Quaternion iconRotation = Quaternion.Euler(0f, 180f + IconYaw, 0f);

        for (int i = 0; i < SlotsPerPage; i++)
        {
            if (slots[i] == null)
            {
                continue;
            }

            Transform slot = slots[i].transform;
            slot.localPosition = CellLocalPosition(i);
            slot.localRotation = Quaternion.identity;
            slot.localScale = Vector3.one;

            if (!slotFilled[i])
            {
                continue;
            }

            Transform iconRoot = slot.Find("IconRoot");
            if (iconRoot != null)
            {
                iconRoot.localPosition = new Vector3(IconOffsetX, IconOffsetY, 0f);
                iconRoot.localRotation = iconRotation;

                // Scale must not go negative, which would mirror the mesh (a "negative scale").
                iconRoot.localScale = Vector3.one * Mathf.Max(0f, IconScale);
            }

            if (nameTexts[i] != null)
            {
                Transform nameTransform = nameTexts[i].transform;
                nameTransform.localPosition = new Vector3(NameOffsetX, NameOffsetY, 0f);
                nameTransform.localRotation = Quaternion.identity;
            }
        }

        PositionIndicator();
    }

    /// <summary>The fixed local position of cell <paramref name="index"/> in the 3 x 3 grid.</summary>
    private Vector3 CellLocalPosition(int index)
    {
        int column = index % Columns;
        int row = index / Columns;
        return new Vector3((column - 1) * GridGapX, (1 - row) * GridGapY, 0f);
    }

    /// <summary>
    /// Moves the highlight onto the selected filled cell, a touch behind the icon plane, and
    /// hides it when the selected cell is empty. Its pulse is driven separately in
    /// AnimateIndicatorAlpha.
    /// </summary>
    private void PositionIndicator()
    {
        if (indicatorRenderer == null)
        {
            return;
        }

        bool show = slotFilled[selectedIndex] && slots[selectedIndex] != null;
        indicatorRenderer.enabled = show;
        if (!show)
        {
            return;
        }

        Vector3 cell = CellLocalPosition(selectedIndex);
        Transform indicatorTransform = indicatorRenderer.transform;
        indicatorTransform.localPosition = new Vector3(cell.x, cell.y + IndicatorOffsetY, IndicatorBehindIcons);
        indicatorTransform.localRotation = Quaternion.identity;
        indicatorTransform.localScale = Vector3.one * Mathf.Max(0f, IndicatorScale);
    }

    /// <summary>
    /// Pulses the indicator's opacity IndicatorAlpha -> IndicatorAlphaMin -> IndicatorAlpha
    /// over IndicatorFadePeriod seconds -- the same breathing highlight the record picker
    /// uses, so the two read alike.
    /// </summary>
    private void AnimateIndicatorAlpha()
    {
        if (indicatorRenderer == null)
        {
            return;
        }

        alphaElapsed += Time.deltaTime;

        float period = IndicatorFadePeriod > 0.01f ? IndicatorFadePeriod : 2f;
        float t = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * alphaElapsed / period);

        // Opacity is 0..1; clamp so an out-of-range field cannot blow the highlight out.
        Color color = indicatorRenderer.color;
        color.a = Mathf.Lerp(Mathf.Clamp01(IndicatorAlphaMin), Mathf.Clamp01(IndicatorAlpha), t);
        indicatorRenderer.color = color;
    }

    private void Confirm()
    {
        int creatureIndex = currentPage * SlotsPerPage + selectedIndex;
        if (creatureIndex < 0 || creatureIndex >= creatures.Count)
        {
            return;
        }

        FDCreature creature = creatures[creatureIndex];

        // Buy flow: hand the chosen creature back to the shop, which drops the bought item
        // into its pack. Sell / Give / Equip (no creature callback): open the info dialog --
        // Give and Equip read back which item was picked (onItemSelected), Sell ignores it.
        if (onCreatureConfirmed != null)
        {
            onCreatureConfirmed(creature);
        }
        else
        {
            OpenInfoDialog(creature);
        }
    }

    private void Cancel()
    {
        OnClosed?.Invoke();
    }

    /// <summary>
    /// Opens the shared CreatureInfoDialog for the confirmed creature, under this dialog's
    /// canvas so it renders. No battle map is in play here, so a null map is passed (the
    /// attribute formulas tolerate it) and the close is handled locally: the instance is
    /// destroyed and the picker resumes taking input. The picker's canvas stays visible
    /// behind it; its world-space grid is hidden so the icons do not poke through the dialog.
    /// </summary>
    private void OpenInfoDialog(FDCreature creature)
    {
        if (creatureInfoDialogPrefab == null)
        {
            Debug.LogWarning("ShoppingCreaturesDialog: no CreatureInfoDialog prefab to show.");
            return;
        }

        Canvas canvas = GetComponentInChildren<Canvas>(true);
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
        SetGridActive(false);
        OnInfoDialogOpened?.Invoke();

        // Report the creature and the item it picked (index >= 0) back to the shop. Equip
        // (reopenInfoAfterSelect) equips it and reopens this dialog so the player stays on the
        // item page; Give reads the one item and lets the shop move on; a cancel (index -1)
        // falls through and resumes the picker. Sell (no callback): the selection is ignored.
        bool reopen = false;
        dialog.Init(creature, infoType, selectedItemIndex =>
        {
            if (onItemSelected != null && selectedItemIndex >= 0)
            {
                onItemSelected(creature, selectedItemIndex);
                reopen = reopenInfoAfterSelect;
            }
        }, (FDMap)null, () =>
        {
            if (infoDialogObject != null)
            {
                Destroy(infoDialogObject);
                infoDialogObject = null;
            }

            // Equip: reopen for the same, now-updated creature -- the player stays on the item
            // page rather than falling back to the picker, and the money bar stays behind it.
            if (reopen)
            {
                OpenInfoDialog(creature);
                return;
            }

            infoDialogOpen = false;
            SetGridActive(true);
            OnInfoDialogClosed?.Invoke();
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

    /// <summary>
    /// The cell's world-space name label, a 3D TextMeshPro created under the slot on first
    /// use and then reused, so a cell's icon and name are children of the one GameObject.
    /// </summary>
    private TextMeshPro GetOrCreateNameText(int slotIndex)
    {
        if (nameTexts[slotIndex] != null)
        {
            return nameTexts[slotIndex];
        }

        if (slots[slotIndex] == null)
        {
            return null;
        }

        GameObject textObject = new GameObject("CreatureName", typeof(TextMeshPro));
        textObject.transform.SetParent(slots[slotIndex].transform, false);

        TextMeshPro text = textObject.GetComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Left;
        text.enableAutoSizing = false;
        text.fontSize = NameFontSize;
        text.color = Color.white;

        TMP_FontAsset messageFont = Resources.Load<TMP_FontAsset>(@"Fonts/FontAssets/zh/FZB_Message");
        if (messageFont != null)
        {
            text.font = messageFont;
        }

        // Anchored at its left edge (pivot x = 0, left-aligned) so the name grows only to the
        // right -- a longer name keeps the same left edge, and its left padding does not shrink
        // the way centred text's would. A generous box (TMP point units) leaves room to grow.
        RectTransform rect = text.rectTransform;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(200f, 40f);

        nameTexts[slotIndex] = text;
        return text;
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
}
