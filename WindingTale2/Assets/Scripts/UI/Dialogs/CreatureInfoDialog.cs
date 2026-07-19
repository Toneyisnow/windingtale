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

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            // ESC / Backspace closes the dialog, exactly as the Cancel button does.
            if (!isClosing && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)))
            {
                onCancel();
            }
        }

        public void Init(FDCreature creature, CreatureInfoType infoType, Action<int> onSelected, GameMain gameMain)
        {
            this.gameMain = gameMain;
            FDMap map = gameMain.gameMap.Map;

            int animationId = creature.Definition.AnimationId;
            string id = StringUtils.Digit3(animationId);
            datoOpenSprite = Resources.Load<Sprite>(string.Format(@"Datos/{0}/Dato_{0}_0", id));
            datoBlinkSprite = Resources.Load<Sprite>(string.Format(@"Datos/{0}/Dato_{0}_3", id));
            startBlink();

            this.creature = creature;
            this.infoType = infoType;
            this.onSelected = onSelected;

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


            if (!isMagic)
            {
                for(int itemIndex = 0; itemIndex < creature.Items.Count; itemIndex ++)
                {
                    int itemId = creature.Items[itemIndex];
                    ItemDefinition item = DefinitionStore.Instance.GetItemDefinition(itemId);
                    
                    GameObject selectable = getSelectableObject(itemIndex);
                    selectable.SetActive(true);
                    
                    var selectableText = selectable.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
                    selectableText.text = item.Name;

                    var selectableAttr = selectable.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>();
                    selectableAttr.text = item.ToAttributeString();

                    TaggedButton taggedButton = selectable.AddComponent<TaggedButton>();
                    taggedButton.Init(itemIndex, (itemIndex) =>
                    {
                        if (isClosing)
                        {
                            return;
                        }

                        closeWithAnimation(() => onSelected(itemIndex));
                    });
                }
                for (int itemIndex = creature.Items.Count; itemIndex < 8; itemIndex++)
                {
                    GameObject selectable = getSelectableObject(itemIndex);
                    selectable.SetActive(false);
                }

            } else
            {
                for (int magicIndex = 0; magicIndex < creature.Magics.Count; magicIndex++)
                {
                    int magicId = creature.Magics[magicIndex];
                    MagicDefinition magic = DefinitionStore.Instance.GetMagicDefinition(magicId);

                    GameObject magicObject = getMagicObject(magicIndex);
                    magicObject.SetActive(true);
                    var selectableText = magicObject.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
                    selectableText.text = magic.Name;

                    TaggedButton taggedButton = magicObject.AddComponent<TaggedButton>();
                    taggedButton.Init(magicIndex, (mIndex) =>
                    {
                        if (isClosing)
                        {
                            return;
                        }

                        closeWithAnimation(() => onSelected(mIndex));
                    });
                }
                for (int magicIndex = creature.Magics.Count; magicIndex < 12; magicIndex++)
                {
                    GameObject selectable = getMagicObject(magicIndex);
                    selectable.SetActive(false);
                }
            }

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
                GameMain.getDefault().gameCanvas.CloseDialog();
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
