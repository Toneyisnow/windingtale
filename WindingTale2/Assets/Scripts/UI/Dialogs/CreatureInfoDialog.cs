using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using WindingTale.Core.Algorithms;
using WindingTale.Core.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Map;
using WindingTale.Core.Objects;
using WindingTale.MapObjects.GameMap;
using WindingTale.Scenes.GameFieldScene;

namespace WindingTale.UI.Dialogs
{
    public enum CreatureInfoType
    {
        SelectEquipItem = 1,
        SelectUseItem = 2,
        SelectAllItem = 3,
        SelectMagic = 4,
        View = 5,
        ViewMagic = 6,
    }

    public class CreatureInfoDialog : MonoBehaviour
    {

        public GameObject datoObj;

        public GameObject nameLabel;
        public GameObject raceLabel;
        public GameObject occupationLabel;

        public GameObject hpCurrentLabel;
        public GameObject hpMaxLabel;
        public GameObject mpCurrentLabel;
        public GameObject mpMaxLabel;

        public GameObject hpBar;
        public GameObject mpBar;

        public GameObject levelLabel;
        public GameObject expLabel;
        public GameObject mvLabel;
        public GameObject apLabel;
        public GameObject dpLabel;
        public GameObject dxLabel;
        public GameObject hitLabel;
        public GameObject evLabel;

        public GameObject itemsContainer;
        public GameObject magicContainer;

        public GameObject selectable_0;
        public GameObject selectable_1;
        public GameObject selectable_2;
        public GameObject selectable_3;
        public GameObject selectable_4;
        public GameObject selectable_5;
        public GameObject selectable_6;
        public GameObject selectable_7;

        public GameObject magic_0;
        public GameObject magic_1;
        public GameObject magic_2;
        public GameObject magic_3;
        public GameObject magic_4;
        public GameObject magic_5;
        public GameObject magic_6;
        public GameObject magic_7;
        public GameObject magic_8;
        public GameObject magic_9;
        public GameObject magic_10;
        public GameObject magic_11;



        private GameMain gameMain = null;

        // The map the attribute formulas read (only to look up terrain, which the panel does
        // not actually use), and the action that tears the dialog down. Both are injected in
        // Init so the dialog can run outside a battle -- the shop's creature picker opens it
        // with a null map and its own close, where there is no GameMain to lean on.
        private FDMap map = null;
        private Action onClose = null;

        private FDCreature creature = null;
        private CreatureInfoType infoType = CreatureInfoType.View;
        private Action<int> onSelected = null;

        // Slide in / out animation: Dato enters from the left, Details from the right and
        // Container from the bottom; closing plays the exact reverse.
        private const float AnimationDuration = 0.15f;

        private RectTransform datoRect = null;
        private RectTransform detailsRect = null;
        private RectTransform containerRect = null;

        // Resting (authored) positions, captured once so a dialog that is reopened while a
        // previous animation left the children off-centre still lands in the right place.
        private Vector2 datoHome = Vector2.zero;
        private Vector2 detailsHome = Vector2.zero;
        private Vector2 containerHome = Vector2.zero;
        private bool layoutCaptured = false;

        // Dato idle blink, the same idiom the TalkDialog portrait uses: frame 0 is the
        // open-eyed portrait, frame 3 the closed-eyed one. Characters without a frame 3
        // simply hold frame 0.
        private const float DatoOpenSeconds = 2.5f;
        private const float DatoBlinkSeconds = 0.5f;

        private Sprite datoOpenSprite = null;
        private Sprite datoBlinkSprite = null;
        private Coroutine blinkCoroutine = null;

        // True from the moment the closing animation starts until the dialog is hidden.
        // Input and further selections are ignored while it runs.
        private bool isClosing = false;
        private Coroutine slideCoroutine = null;

        // Selection grid. Both pages behave the same way - every present entry can be
        // highlighted, but only a "selectable" one (one that makes sense for this dialog's
        // infoType) can actually be confirmed - but they are indexed differently.
        //
        // Items run row-major, two per row:      Magics run column-major, 3 x 4:
        //     0 1                                    0 4  8
        //     2 3                                    1 5  9
        //     4 5                                    2 6 10
        //     6 7                                    3 7 11
        private const int ItemColumns = 2;
        private const int MagicColumns = 3;
        private const int MagicRows = 4;

        private const int MaxItemCount = 8;
        private const int MaxMagicCount = MagicColumns * MagicRows;

        // The item type icon (attack / defend / usable) shown before each item's name, the
        // same face the shop's Buy list uses (see ItemIconHelper). It is built once per item
        // slot and parented after the name / attribute labels, so getChild(0) / (1) stay the
        // name and attribute; the name is slid right by ItemNameShiftX to clear it. Magics
        // keep their plain name / cost layout.
        private const string ItemIconName = "TypeIcon";
        private const float ItemIconSize = 32f;
        private const float ItemIconInset = 0f;
        private const float ItemNameShiftX = 36f;

        private static readonly Color SelectableTextColor = Color.white;

        // Softened rather than pure red: pure red on the dark panel is hard to read at the
        // small size the attribute line is rendered at.
        private static readonly Color UnselectableTextColor = new Color(1f, 0.35f, 0.35f);

        private static readonly Color HighlightColor = new Color(1f, 1f, 1f, 0.3f);
        private static readonly Color TransparentColor = new Color(1f, 1f, 1f, 0f);

        // Which page the grid currently describes, so the index <-> (column, row) mapping
        // and the slot lookup both know which layout they are walking.
        private bool isMagicPage = false;

        private int slotCount = 0;
        private int selectedSlotIndex = -1;
        private readonly bool[] slotSelectable = new bool[MaxMagicCount];

        // Frame the dialog was opened on. Keys are ignored on it, so the Enter that picked
        // the menu entry which opened this dialog does not also confirm the first entry.
        private int openedFrame = -1;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (isClosing)
            {
                return;
            }

            // ESC / Backspace closes the dialog, exactly as the Cancel button does.
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                onCancel();
                return;
            }

            handleSelectionKeys();
        }

        /// <summary>
        /// Arrow keys walk the grid, Enter / Space confirms. Only meaningful while something
        /// is highlighted, which a view-only dialog never is.
        /// </summary>
        private void handleSelectionKeys()
        {
            if (slotCount <= 0 || selectedSlotIndex < 0 || Time.frameCount == openedFrame)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                moveSelection(0, -1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                moveSelection(0, 1);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                moveSelection(-1, 0);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                moveSelection(1, 0);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                onSlotActivated(selectedSlotIndex, false);
            }
        }

        /// <summary>
        /// Moves the highlight by one grid step. A step that would leave the grid, or land on
        /// a slot with nothing in it, is ignored - the highlight simply stays where it is.
        /// </summary>
        private void moveSelection(int deltaColumn, int deltaRow)
        {
            int columns = isMagicPage ? MagicColumns : ItemColumns;
            int column = getSlotColumn(selectedSlotIndex) + deltaColumn;
            int row = getSlotRow(selectedSlotIndex) + deltaRow;

            // The item grid grows downwards with the item count, so only its columns are
            // bounded here; the magic grid is a fixed 3 x 4.
            if (column < 0 || column >= columns || row < 0)
            {
                return;
            }
            if (isMagicPage && row >= MagicRows)
            {
                return;
            }

            int target = isMagicPage ? column * MagicRows + row : row * ItemColumns + column;
            if (target >= slotCount)
            {
                return;
            }

            selectedSlotIndex = target;
            refreshHighlight();
        }

        private int getSlotColumn(int index)
        {
            return isMagicPage ? index / MagicRows : index % ItemColumns;
        }

        private int getSlotRow(int index)
        {
            return isMagicPage ? index % MagicRows : index / ItemColumns;
        }

        /// <summary>
        /// Confirms the entry at <paramref name="index"/>. A click also moves the highlight
        /// there first, so clicking an unselectable entry leaves it highlighted but does
        /// nothing else - the same outcome as walking onto it with the arrow keys.
        /// </summary>
        private void onSlotActivated(int index, bool moveHighlight)
        {
            if (isClosing || isViewOnly() || index < 0 || index >= slotCount)
            {
                return;
            }

            if (moveHighlight)
            {
                selectedSlotIndex = index;
                refreshHighlight();
            }

            if (!slotSelectable[index])
            {
                return;
            }

            Action<int> callback = this.onSelected;
            this.onSelected = null;

            closeWithAnimation(() => callback?.Invoke(index));
        }

        /// <summary>
        /// A pure inspection dialog: the list is shown, but nothing is highlighted and
        /// neither the keyboard nor a click can pick anything out of it.
        /// </summary>
        private bool isViewOnly()
        {
            return infoType == CreatureInfoType.View || infoType == CreatureInfoType.ViewMagic;
        }

        /// <summary>
        /// Whether the item at <paramref name="itemIndex"/> can be confirmed in this dialog:
        /// a use dialog only accepts usable items, an equip dialog only accepts equipment
        /// that is not already worn.
        /// </summary>
        private bool isItemSelectable(int itemIndex, ItemDefinition item)
        {
            if (item == null)
            {
                return false;
            }

            switch (infoType)
            {
                case CreatureInfoType.SelectUseItem:
                    return item.IsUsable();

                case CreatureInfoType.SelectEquipItem:
                    return item.IsEquipment()
                        && itemIndex != creature.AttackItemIndex
                        && itemIndex != creature.DefendItemIndex;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Whether <paramref name="magic"/> can be confirmed: the creature has to be able to
        /// pay for it.
        /// </summary>
        private bool isMagicSelectable(MagicDefinition magic)
        {
            if (magic == null)
            {
                return false;
            }

            if (infoType == CreatureInfoType.SelectMagic)
            {
                return creature.Mp >= magic.MpCost;
            }

            return true;
        }

        /// <summary>
        /// The entry to open on: the first one that can actually be picked, falling back to
        /// the first entry so something is always highlighted. View-only dialogs pass -1 to
        /// leave the grid unhighlighted instead of calling this.
        /// </summary>
        private int getFirstSelectableIndex()
        {
            if (slotCount <= 0)
            {
                return -1;
            }

            for (int index = 0; index < slotCount; index++)
            {
                if (slotSelectable[index])
                {
                    return index;
                }
            }

            return 0;
        }

        /// <summary>
        /// Repaints the slot backgrounds so exactly the highlighted one is lit.
        /// </summary>
        private void refreshHighlight()
        {
            int maxIndex = isMagicPage ? MaxMagicCount : MaxItemCount;

            for (int index = 0; index < maxIndex; index++)
            {
                GameObject slot = getSlotObject(index);
                if (slot == null)
                {
                    continue;
                }

                Image background = slot.GetComponent<Image>();
                if (background == null)
                {
                    continue;
                }

                background.color = index == selectedSlotIndex ? HighlightColor : TransparentColor;
            }
        }

        private GameObject getSlotObject(int index)
        {
            return isMagicPage ? getMagicObject(index) : getSelectableObject(index);
        }

        /// <summary>
        /// Wires one grid slot: colours its labels by selectability and points its button at
        /// this open's callback. The dialog is a scene object that is reused rather than
        /// re-instantiated, so the button is only added once and re-pointed on each open;
        /// adding a second TaggedButton would fire onSelected twice.
        /// </summary>
        private void setupSlot(GameObject slot, int index, bool selectable, params TextMeshProUGUI[] labels)
        {
            slotSelectable[index] = selectable;

            Color textColor = selectable ? SelectableTextColor : UnselectableTextColor;
            foreach (TextMeshProUGUI label in labels)
            {
                if (label != null)
                {
                    label.color = textColor;
                }
            }

            TaggedButton taggedButton = slot.GetComponent<TaggedButton>();
            if (taggedButton == null)
            {
                taggedButton = slot.AddComponent<TaggedButton>();
            }

            taggedButton.Init(index, clickedIndex => onSlotActivated(clickedIndex, true));

            // Unity's own colour tint would repaint the background on hover / click and
            // fight the highlight this dialog draws itself.
            Button button = slot.GetComponent<Button>();
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
            }
        }

        /// <summary>
        /// Shows the item's type icon (attack / defend / usable) before its name, the same face
        /// the shop's Buy list uses (see ItemIconHelper). The icon is created once per slot and
        /// reused -- the dialog is a scene object reused across opens, so a fresh one each time
        /// would stack -- and appended after the name / attribute labels so getChild(0) / (1)
        /// keep pointing at them.
        /// </summary>
        private void setupItemIcon(GameObject slot, ItemDefinition item)
        {
            Image icon = ensureItemIcon(slot);
            if (icon == null)
            {
                return;
            }

            Sprite sprite = ItemIconHelper.LoadIcon(item);
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.gameObject.SetActive(true);
        }

        /// <summary>
        /// The slot's type-icon Image: built on first use at the slot's left edge, with the
        /// name label (getChild(0)) slid right past it once so the two do not overlap, and then
        /// found and reused on later opens. The attribute label keeps its authored place.
        /// </summary>
        private Image ensureItemIcon(GameObject slot)
        {
            Transform existing = slot.transform.Find(ItemIconName);
            if (existing != null)
            {
                return existing.GetComponent<Image>();
            }

            GameObject iconObject = new GameObject(ItemIconName, typeof(RectTransform), typeof(Image));
            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.SetParent(slot.transform, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(ItemIconSize, ItemIconSize);
            rect.anchoredPosition = new Vector2(ItemIconInset, 0f);

            Image image = iconObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;

            RectTransform nameRect = slot.transform.GetChild(0) as RectTransform;
            if (nameRect != null)
            {
                Vector2 pos = nameRect.anchoredPosition;
                pos.x = ItemNameShiftX;
                nameRect.anchoredPosition = pos;
            }

            return image;
        }

        /// <summary>
        /// The battlefield entry point: closes through the shared GameCanvas and reads the
        /// live battle map. A thin wrapper over the scene-independent overload below so the
        /// two paths stay in step.
        /// </summary>
        public void Init(FDCreature creature, CreatureInfoType infoType, Action<int> onSelected, GameMain gameMain)
        {
            this.gameMain = gameMain;
            Init(creature, infoType, onSelected, gameMain.gameMap.Map,
                () => GameMain.getDefault().gameCanvas.CloseDialog());
        }

        /// <summary>
        /// The scene-independent entry point. <paramref name="map"/> may be null when the
        /// creature is shown outside a battle (the attribute formulas tolerate it), and
        /// <paramref name="onClose"/> is what takes the dialog down once its closing
        /// animation has finished -- the caller owns whether that hides or destroys it.
        /// </summary>
        public void Init(FDCreature creature, CreatureInfoType infoType, Action<int> onSelected, FDMap map, Action onClose)
        {
            this.map = map;
            this.onClose = onClose;

            int animationId = creature.Definition.AnimationId;
            string id = StringUtils.Digit3(animationId);
            datoOpenSprite = Resources.Load<Sprite>(string.Format(@"Datos/{0}/Dato_{0}_0", id));
            datoBlinkSprite = Resources.Load<Sprite>(string.Format(@"Datos/{0}/Dato_{0}_3", id));
            startBlink();

            this.creature = creature;
            this.infoType = infoType;
            this.onSelected = onSelected;
            this.openedFrame = Time.frameCount;

            bool isMagic = infoType == CreatureInfoType.SelectMagic || infoType == CreatureInfoType.ViewMagic;
            itemsContainer.SetActive(!isMagic);
            magicContainer.SetActive(isMagic);

            // Name
            this.nameLabel.GetComponent<TextMeshProUGUI>().text = creature.Definition.Name;

            // Race
            int raceId = creature.Definition.Race;
            this.raceLabel.GetComponent<LocalizeStringEvent>().StringReference = LocalizationManager.GetRaceString(creature.Definition.Race);

            int occupationId = creature.Definition.Occupation;
            OccupationDefinition occupation = DefinitionStore.Instance.GetOccupationDefinition(occupationId);
            this.occupationLabel.GetComponent<TextMeshProUGUI>().text = occupation.Name;

            this.hpCurrentLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit3(creature.Hp);
            this.hpMaxLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit3(creature.HpMax);
            this.hpBar.transform.localScale = new Vector3((float)creature.Hp / creature.HpMax, 1, 1);

            this.mpCurrentLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit3(creature.Mp);
            this.mpMaxLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit3(creature.MpMax);
            this.mpBar.transform.localScale = new Vector3(creature.MpMax > 0 ? (float)creature.Mp / creature.MpMax : 0, 1, 1);

            // Details information

            int creatureAp = CreatureFormula.GetCalculatedAp(creature, map);
            int creatureDp = CreatureFormula.GetCalculatedDp(creature, map);
            int creatureDx = CreatureFormula.GetCalculatedDx(creature, map);
            int creatureHit = CreatureFormula.GetCalculatedHit(creature, map);
            int creatureEv = CreatureFormula.GetCalculatedEv(creature, map);

            this.levelLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit2(creature.Level);
            this.expLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit3(creature.Exp);
            this.mvLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit2(creature.Mv);
            this.apLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit2(creatureAp);
            this.dpLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit2(creatureDp);
            this.dxLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit2(creatureDx);
            this.hitLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit2(creatureHit);
            this.evLabel.GetComponent<TextMeshProUGUI>().text = StringUtils.Digit2(creatureEv);


            isMagicPage = isMagic;

            if (!isMagic)
            {
                slotCount = Math.Min(creature.Items.Count, MaxItemCount);

                for(int itemIndex = 0; itemIndex < slotCount; itemIndex ++)
                {
                    int itemId = creature.Items[itemIndex];
                    ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);

                    GameObject selectable = getSelectableObject(itemIndex);
                    selectable.SetActive(true);

                    var selectableText = selectable.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
                    selectableText.text = item.Name;

                    var selectableAttr = selectable.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                    selectableAttr.text = item.ToAttributeString();

                    // The type icon (attack / defend / usable) before the name, the same face
                    // the shop's Buy list shows.
                    setupItemIcon(selectable, item);

                    setupSlot(selectable, itemIndex, isItemSelectable(itemIndex, item), selectableText, selectableAttr);
                }
                for (int itemIndex = slotCount; itemIndex < MaxItemCount; itemIndex++)
                {
                    GameObject selectable = getSelectableObject(itemIndex);
                    selectable.SetActive(false);
                    slotSelectable[itemIndex] = false;
                }

            } else
            {
                slotCount = Math.Min(creature.Magics.Count, MaxMagicCount);

                for (int magicIndex = 0; magicIndex < slotCount; magicIndex++)
                {
                    int magicId = creature.Magics[magicIndex];
                    MagicDefinition magic = DefinitionStore.Instance.GetMagicDefinition(magicId);

                    GameObject magicObject = getMagicObject(magicIndex);
                    magicObject.SetActive(true);

                    var selectableText = magicObject.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
                    selectableText.text = magic.Name;

                    var selectableAttr = magicObject.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                    selectableAttr.text = string.Format("MP-{0}", magic.MpCost);

                    setupSlot(magicObject, magicIndex, isMagicSelectable(magic), selectableText, selectableAttr);
                }
                for (int magicIndex = slotCount; magicIndex < MaxMagicCount; magicIndex++)
                {
                    GameObject magicObject = getMagicObject(magicIndex);
                    magicObject.SetActive(false);
                    slotSelectable[magicIndex] = false;
                }
            }

            // Open on the first entry that can actually be picked, falling back to the first
            // entry so something is always highlighted. A view-only dialog shows no highlight
            // at all: -1 also disables the keyboard and click paths.
            selectedSlotIndex = isViewOnly() ? -1 : getFirstSelectableIndex();
            refreshHighlight();

            playEnterAnimation();
        }


        /// <summary>
        /// Cancelled: report index -1 (every caller reads a negative index as "cancelled")
        /// and close. Wired to the Cancel button and to ESC / Backspace.
        /// </summary>
        public void onCancel()
        {
            // A cancel that arrives while the closing animation is already running (button
            // click plus key press) is ignored.
            if (isClosing)
            {
                return;
            }

            // The callback is cleared first: closing deactivates the GameObject, but a
            // second onCancel in the same frame (button click plus key press) would
            // otherwise report the cancellation twice.
            Action<int> callback = this.onSelected;
            this.onSelected = null;

            closeWithAnimation(() => callback?.Invoke(-1));
        }


        /// <summary>
        /// Slides the three panels back out, then reports the result and hides the dialog.
        /// The callback runs after the animation so the dialog is still on screen while it
        /// leaves, and the caller's follow-up (which may open another dialog) only happens
        /// once this one is done.
        /// </summary>
        private void closeWithAnimation(Action onClosed)
        {
            isClosing = true;

            startSlide(false, () =>
            {
                onClosed?.Invoke();
                this.onClose?.Invoke();
            });
        }

        /// <summary>
        /// (Re)starts the portrait's blink loop. The dialog is reused across creatures, so a
        /// blink left running for the previous one has to be stopped before the next starts,
        /// or the two would fight over the Image.
        /// </summary>
        private void startBlink()
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }

            setDatoSprite(datoOpenSprite);

            // Nothing to alternate with: leave the portrait open-eyed.
            if (datoBlinkSprite == null)
            {
                return;
            }

            blinkCoroutine = StartCoroutine(blinkRoutine());
        }

        private IEnumerator blinkRoutine()
        {
            while (true)
            {
                setDatoSprite(datoOpenSprite);
                yield return new WaitForSeconds(DatoOpenSeconds);

                setDatoSprite(datoBlinkSprite);
                yield return new WaitForSeconds(DatoBlinkSeconds);
            }
        }

        private void setDatoSprite(Sprite sprite)
        {
            if (sprite == null || datoObj == null)
            {
                return;
            }

            datoObj.GetComponent<Image>().sprite = sprite;
        }

        private void playEnterAnimation()
        {
            isClosing = false;
            startSlide(true, null);
        }

        private void startSlide(bool isEntering, Action onComplete)
        {
            captureLayout();

            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
            }

            slideCoroutine = StartCoroutine(slideRoutine(isEntering, onComplete));
        }

        /// <summary>
        /// Caches the three animated children and their authored positions. Done once: the
        /// positions must come from a state where nothing has been offset yet.
        /// </summary>
        private void captureLayout()
        {
            if (layoutCaptured)
            {
                return;
            }

            datoRect = this.transform.Find("Dato") as RectTransform;
            detailsRect = this.transform.Find("Details") as RectTransform;
            containerRect = this.transform.Find("Container") as RectTransform;

            if (datoRect != null)
            {
                datoHome = datoRect.anchoredPosition;
            }
            if (detailsRect != null)
            {
                detailsHome = detailsRect.anchoredPosition;
            }
            if (containerRect != null)
            {
                containerHome = containerRect.anchoredPosition;
            }

            layoutCaptured = true;
        }

        private IEnumerator slideRoutine(bool isEntering, Action onComplete)
        {
            Vector2 datoOffset = getOffscreenOffset(datoRect, Vector2.left);
            Vector2 detailsOffset = getOffscreenOffset(detailsRect, Vector2.right);
            Vector2 containerOffset = getOffscreenOffset(containerRect, Vector2.down);

            float elapsed = 0f;
            while (elapsed < AnimationDuration)
            {
                // Unscaled: the dialog must animate even when the field is paused.
                elapsed += Time.unscaledDeltaTime;

                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / AnimationDuration));

                // Fraction of the off-screen offset still applied: entering counts it down
                // to zero, leaving counts it back up, which makes the two exact mirrors.
                applyOffset(isEntering ? 1f - eased : eased, datoOffset, detailsOffset, containerOffset);

                yield return null;
            }

            applyOffset(isEntering ? 0f : 1f, datoOffset, detailsOffset, containerOffset);

            slideCoroutine = null;
            onComplete?.Invoke();
        }

        private void applyOffset(float amount, Vector2 datoOffset, Vector2 detailsOffset, Vector2 containerOffset)
        {
            if (datoRect != null)
            {
                datoRect.anchoredPosition = datoHome + datoOffset * amount;
            }
            if (detailsRect != null)
            {
                detailsRect.anchoredPosition = detailsHome + detailsOffset * amount;
            }
            if (containerRect != null)
            {
                containerRect.anchoredPosition = containerHome + containerOffset * amount;
            }
        }

        /// <summary>
        /// Distance along <paramref name="direction"/> that puts the panel fully outside the
        /// canvas, so it is never visible at the far end of the slide.
        /// </summary>
        private Vector2 getOffscreenOffset(RectTransform rect, Vector2 direction)
        {
            if (rect == null)
            {
                return Vector2.zero;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;

            float canvasWidth = canvasRect != null ? canvasRect.rect.width : Screen.width;
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : Screen.height;

            return new Vector2(
                direction.x * (canvasWidth / 2f + rect.rect.width),
                direction.y * (canvasHeight / 2f + rect.rect.height)
            );
        }


        private GameObject getSelectableObject(int index)
        {
            switch (index)
            {
                case 0:
                    return selectable_0;
                case 1:
                    return selectable_1;
                case 2:
                    return selectable_2;
                case 3:
                    return selectable_3;
                case 4:
                    return selectable_4;
                case 5:
                    return selectable_5;
                case 6:
                    return selectable_6;
                case 7:
                    return selectable_7;
            }
            return null;
        }

        private GameObject getMagicObject(int index)
        {
            switch (index)
            {
                case 0:
                    return magic_0;
                case 1:
                    return magic_1;
                case 2:
                    return magic_2;
                    case 3:
                    return magic_3;
                    case 4:
                    return magic_4;
                    case 5:
                    return magic_5;
                    case 6:
                    return magic_6;
                    case 7:
                    return magic_7;
                    case 8:
                    return magic_8;
                    case 9:
                    return magic_9;
                    case 10:
                    return magic_10;
                    case 11:
                    return magic_11;

            }
            return null;
        }

    }
}
