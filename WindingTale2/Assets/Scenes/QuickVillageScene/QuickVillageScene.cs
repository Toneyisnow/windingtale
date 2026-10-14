using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WindingTale.Core.Definitions;
using WindingTale.Core.Files;
using WindingTale.UI.Audio;
using WindingTale.UI.Utils;

/// <summary>
/// The stand-in for the village between two chapters whose weapon shop and item shop both
/// sell nothing (chapters 23-25, 28-30 -- see HasNoShops). There is nothing to buy, so the
/// village and its shops are skipped and all that is left is the bar's save: the record
/// slot picker (ShoppingRecordDialog), up on black for the whole visit.
///
/// Above the picker, on the left, a "保存游戏" line says what it is for; on the right an
/// "进入游戏" button asks "确定要进入战场吗？" (the village exit's question) and a Yes goes on
/// to the battlefield. Esc on the picker asks the same question, so the keyboard has a way
/// out too.
///
/// Saving runs as at the bar: an empty slot is written straight away and "记录存储完毕！"
/// shown; an occupied one asks "是否要覆盖存档？" first -- Yes writes it, No goes back to the
/// picker. Every notice and question hides the picker while it stands, as the shop's
/// dialog stack does, and the picker is opened afresh afterwards so it lists the new save.
///
/// It is entered on black (GameMain.EnterVillage, or a load) with the party on
/// GlobalVariables under VillageScene.RecordVariableName, the village's own handover.
/// </summary>
public class QuickVillageScene : MonoBehaviour
{
    /// <summary>This scene's name in the build settings.</summary>
    public const string SceneName = "QuickVillageScene";

    /// <summary>
    /// Whether the village before <paramref name="chapterId"/> has nothing to sell: its
    /// weapon shop (ShopType.Amor) and item shop (ShopType.Item) carry no items in
    /// Data/Shop.txt. Those chapters get this scene in place of the village.
    /// </summary>
    public static bool HasNoShops(int chapterId)
    {
        return IsEmptyShop(chapterId, ShopType.Amor) && IsEmptyShop(chapterId, ShopType.Item);
    }

    private static bool IsEmptyShop(int chapterId, ShopType shopType)
    {
        ShopDefinition shop = DefinitionStore.Instance.GetShopDefinition(chapterId, shopType);
        return shop == null || shop.Items == null || shop.Items.Count == 0;
    }

    /// <summary>
    /// The scene the party goes to between battles, about to play <paramref name="chapterId"/>:
    /// this one when that chapter's village sells nothing, the village otherwise. Every way
    /// into the village (a won battle, a load from the title or the bar) asks here.
    /// </summary>
    public static string VillageSceneNameFor(int chapterId)
    {
        return HasNoShops(chapterId) ? SceneName : "VillageScene";
    }

    /// <summary>The save slot picker -- the bar's and the title's ShoppingRecordDialog prefab.</summary>
    public GameObject recordDialogPrefab = null;

    /// <summary>The one-line notice shown after a save -- the shop's ShoppingMessageDialog prefab.</summary>
    public GameObject messageDialogPrefab = null;

    /// <summary>The yes / no question -- the shop's ShoppingConfirmDialog prefab.</summary>
    public GameObject confirmDialogPrefab = null;

    /// <summary>Time the scene takes to come up out of black, and to go back to it, in seconds.</summary>
    public float fadeDuration = 1.0f;

    /// <summary>The line over the picker's top-left corner.</summary>
    private const string SaveTitleText = "保存游戏";

    /// <summary>The label of the button over the picker's top-right corner.</summary>
    private const string EnterGameButtonText = "进入游戏";

    /// <summary>
    /// Asked before a save is written over an occupied slot -- the bar's literal, drawn in the
    /// record font since the baked message atlases carry no 覆, 盖 or 档.
    /// </summary>
    private const string OverwriteConfirmText = "是否要覆盖存档？";

    /// <summary>The CommonStrings "Message-57" line shown after a save: "记录存储完毕！".</summary>
    private const int SaveDoneMessageId = 57;

    /// <summary>CommonStrings "Confirm-08": 确定要进入战场吗？ -- the village exit's question.</summary>
    private const int EnterBattleConfirmId = 8;

    /// <summary>The title line and the button stand this far (canvas units) above the picker's frame.</summary>
    private const float HeaderGap = 6f;

    /// <summary>The title line's and the button label's font size.</summary>
    private const float HeaderFontSize = 26f;

    /// <summary>The button's size, in canvas units.</summary>
    private static readonly Vector2 EnterGameButtonSize = new Vector2(140f, 40f);

    /// <summary>The party, as the won battle (or the loaded save) left them.</summary>
    public GameRecord Record { get; private set; }

    private ScreenFader fader = null;

    /// <summary>The slot picker while it is up, or null.</summary>
    private GameObject recordDialog = null;

    /// <summary>The notice or question standing over the (hidden) picker, or null.</summary>
    private GameObject popupDialog = null;

    /// <summary>The slot the overwrite question is asking about.</summary>
    private int pendingSaveSlot = -1;

    /// <summary>True once the way on has been taken; nothing more is opened.</summary>
    private bool leaving = false;

    void Start()
    {
        Record = GlobalVariables.Take<GameRecord>(VillageScene.RecordVariableName);
        if (Record == null)
        {
            // Opened on its own from the editor rather than won into.
            Debug.LogWarning("No game record to enter the quick village with; using an empty one.");
            Record = new GameRecord() { ChapterId = 23, Friends = new List<CreatureMapRecord>() };
        }

        // A load from the bar may have left the village's "back from the shop" marker behind;
        // it belongs to no village now.
        GlobalVariables.Take<VillageScene.ShopReturnInfo>(VillageScene.ShopReturnVariableName);

        PlayVillageMusic();

        // The picker is put up once the black has cleared, so a key still held from the
        // battle's last line cannot reach it.
        fader = ScreenFader.Create(1.0f);
        fader.FadeTo(0.0f, fadeDuration, OpenRecordDialog);
    }

    /// <summary>
    /// The chapter's Village track, as the village plays it; silent when the chapter has none.
    /// </summary>
    private void PlayVillageMusic()
    {
        ChapterDefinition chapter = DefinitionStore.Instance.LoadChapter(Record.ChapterId);
        string clipName = chapter != null && chapter.BackgroundMusic != null
            ? chapter.BackgroundMusic.Village
            : null;
        string path = string.IsNullOrEmpty(clipName) ? null : "Audios/Musics/" + clipName;
        BackgroundMusic.GetOrCreate().PlayClipByName(path);
    }

    #region The slot picker

    /// <summary>
    /// Puts the slot picker up afresh -- it reads the saves when opened, so a new one is
    /// listed -- with the "保存游戏" line and the "进入游戏" button over its frame.
    /// </summary>
    private void OpenRecordDialog()
    {
        if (leaving)
        {
            return;
        }

        CloseRecordDialog();

        if (recordDialogPrefab == null)
        {
            Debug.LogWarning("Quick village scene has no record dialog prefab to show.");
            return;
        }

        GameObject dialogObject = Instantiate(recordDialogPrefab);
        ShoppingRecordDialog dialog = dialogObject.GetComponent<ShoppingRecordDialog>();
        if (dialog == null)
        {
            Debug.LogWarning("Record dialog prefab has no ShoppingRecordDialog component.");
            Destroy(dialogObject);
            return;
        }

        recordDialog = dialogObject;

        // Saving may pick an empty slot -- a fresh slot to write into.
        dialog.Init(OnRecordSlotSelected, allowEmpty: true);
        BuildHeader(dialogObject);
    }

    private void CloseRecordDialog()
    {
        if (recordDialog != null)
        {
            Destroy(recordDialog);
            recordDialog = null;
        }
    }

    /// <summary>
    /// The picker has reported a slot. Esc (-1) has nothing to back out to here, so it asks
    /// the way on instead, as the button does. An empty slot is saved at once; an occupied
    /// one asks first.
    /// </summary>
    private void OnRecordSlotSelected(int slotIndex)
    {
        if (popupDialog != null || leaving)
        {
            return;
        }

        if (slotIndex < 0)
        {
            OpenEnterBattleConfirm();
            return;
        }

        if (GameRecordManager.LoadFromFile(slotIndex) != null)
        {
            pendingSaveSlot = slotIndex;
            OpenConfirm(dialog => dialog.InitLiteral(OverwriteConfirmText, OnOverwriteConfirmed));
            return;
        }

        SaveToSlot(slotIndex);
    }

    /// <summary>Yes writes over the slot; No goes back to the picker.</summary>
    private void OnOverwriteConfirmed(bool yes)
    {
        ClosePopup();

        if (yes)
        {
            SaveToSlot(pendingSaveSlot);
        }
        else
        {
            ShowRecordDialog();
        }
    }

    /// <summary>Writes the party into the slot and shows "记录存储完毕！"; any key brings the picker back.</summary>
    private void SaveToSlot(int slotIndex)
    {
        GameRecordManager.SaveToFile(slotIndex, Record);

        ShoppingMessageDialog dialog = InstantiatePopup<ShoppingMessageDialog>(messageDialogPrefab);
        if (dialog == null)
        {
            OpenRecordDialog();
            return;
        }

        dialog.Init(SaveDoneMessageId, () =>
        {
            ClosePopup();
            OpenRecordDialog();
        });
    }

    #endregion

    #region The way on

    /// <summary>The "进入游戏" button, or Esc on the picker: "确定要进入战场吗？".</summary>
    private void OpenEnterBattleConfirm()
    {
        if (popupDialog != null || leaving)
        {
            return;
        }

        OpenConfirm(dialog => dialog.Init(EnterBattleConfirmId, OnEnterBattleConfirmed));
    }

    /// <summary>No goes back to the picker; Yes leaves for the battlefield.</summary>
    private void OnEnterBattleConfirmed(bool yes)
    {
        ClosePopup();

        if (yes)
        {
            StartCoroutine(EnterBattlefieldRoutine());
        }
        else
        {
            ShowRecordDialog();
        }
    }

    /// <summary>
    /// Leaves for the battle the way the village does (VillageScene.EnterBattlefieldRoutine):
    /// the picker comes down, the screen fades to black, the music stops, and the field scene
    /// is loaded on that black with the party -- whose record already names the chapter to play.
    /// </summary>
    private IEnumerator EnterBattlefieldRoutine()
    {
        leaving = true;
        CloseRecordDialog();

        GameFiledSceneParams.isContinue = false;
        GlobalVariables.Set(VillageScene.RecordVariableName, Record);

        fader.FadeTo(1.0f, fadeDuration);
        while (fader.IsFading)
        {
            yield return null;
        }

        BackgroundMusic.GetOrCreate().StopMusic();

        SceneManager.LoadScene("GameFieldScene", LoadSceneMode.Single);
    }

    #endregion

    #region Notices and questions

    private void OpenConfirm(System.Action<ShoppingConfirmDialog> init)
    {
        ShoppingConfirmDialog dialog = InstantiatePopup<ShoppingConfirmDialog>(confirmDialogPrefab);
        if (dialog != null)
        {
            init(dialog);
        }
    }

    /// <summary>
    /// Puts a notice or question up over the picker, which is hidden while it stands -- only the
    /// top dialog takes the keyboard, as in the shop's dialog stack.
    /// </summary>
    private T InstantiatePopup<T>(GameObject prefab) where T : Component
    {
        if (prefab == null)
        {
            Debug.LogWarning("Quick village scene is missing a dialog prefab for " + typeof(T).Name + ".");
            return null;
        }

        GameObject dialogObject = Instantiate(prefab);
        T dialog = dialogObject.GetComponent<T>();
        if (dialog == null)
        {
            Debug.LogWarning("Dialog prefab has no " + typeof(T).Name + " component.");
            Destroy(dialogObject);
            return null;
        }

        // Draw it above anything else on screen.
        Canvas canvas = dialogObject.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 100;
        }

        if (recordDialog != null)
        {
            recordDialog.SetActive(false);
        }

        popupDialog = dialogObject;
        return dialog;
    }

    private void ClosePopup()
    {
        if (popupDialog != null)
        {
            Destroy(popupDialog);
            popupDialog = null;
        }
    }

    /// <summary>Brings the hidden picker back as it was, or opens it again if it is gone.</summary>
    private void ShowRecordDialog()
    {
        if (recordDialog != null)
        {
            recordDialog.SetActive(true);
        }
        else
        {
            OpenRecordDialog();
        }
    }

    #endregion

    #region The header over the picker

    /// <summary>
    /// Hangs the "保存游戏" line off the top-left corner of the picker's frame and the
    /// "进入游戏" button off its top-right corner, both just above it. They are children of
    /// the frame, so they follow it wherever the canvas scaler puts it, and come and go with
    /// the picker.
    /// </summary>
    private void BuildHeader(GameObject dialogObject)
    {
        RectTransform frame = FindFrame(dialogObject);
        if (frame == null)
        {
            Debug.LogWarning("Record dialog has no Background frame to hang the header on.");
            return;
        }

        TMP_FontAsset font = ShoppingRecordDialog.GetRecordFont();

        // "保存游戏", standing on the frame's top edge at its left end.
        RectTransform title = NewRect("SaveTitle", frame, new Vector2(0f, 1f), new Vector2(0f, 0f));
        title.anchoredPosition = new Vector2(0f, HeaderGap);
        title.sizeDelta = new Vector2(200f, HeaderFontSize + 8f);
        TextMeshProUGUI titleText = AddText(title.gameObject, SaveTitleText, font, TextAlignmentOptions.BottomLeft);

        // "进入游戏", standing on the frame's top edge at its right end.
        RectTransform button = NewRect("EnterGameButton", frame, new Vector2(1f, 1f), new Vector2(1f, 0f));
        button.anchoredPosition = new Vector2(0f, HeaderGap);
        button.sizeDelta = EnterGameButtonSize;

        Image buttonImage = button.gameObject.AddComponent<Image>();
        Image frameImage = frame.GetComponent<Image>();
        if (frameImage != null && frameImage.sprite != null)
        {
            // Dressed like the picker's own frame.
            buttonImage.sprite = frameImage.sprite;
            buttonImage.type = frameImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            buttonImage.color = frameImage.color;
        }
        else
        {
            buttonImage.color = new Color(0.1f, 0.1f, 0.3f, 0.85f);
        }

        Button buttonComponent = button.gameObject.AddComponent<Button>();
        buttonComponent.targetGraphic = buttonImage;

        // The picker steers by its own keys; the EventSystem must not walk onto this button
        // and submit it on Space / Enter as well.
        Navigation navigation = buttonComponent.navigation;
        navigation.mode = Navigation.Mode.None;
        buttonComponent.navigation = navigation;
        buttonComponent.onClick.AddListener(() =>
        {
            SoundEffects.Play(SoundEffect.DialogConfirm);
            OpenEnterBattleConfirm();
        });

        RectTransform label = NewRect("Label", button, Vector2.zero, new Vector2(0.5f, 0.5f));
        label.anchorMax = Vector2.one;
        label.offsetMin = Vector2.zero;
        label.offsetMax = Vector2.zero;
        TextMeshProUGUI labelText = AddText(label.gameObject, EnterGameButtonText, font, TextAlignmentOptions.Center);
        labelText.raycastTarget = false;

        titleText.raycastTarget = false;
    }

    /// <summary>The picker's frame: the "Background" image under its canvas.</summary>
    private static RectTransform FindFrame(GameObject dialogObject)
    {
        foreach (RectTransform rect in dialogObject.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == "Background")
            {
                return rect;
            }
        }

        return null;
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pivot)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        return rect;
    }

    private static TextMeshProUGUI AddText(GameObject target, string text, TMP_FontAsset font, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI textMesh = target.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            textMesh.font = font;
        }
        textMesh.fontSize = HeaderFontSize;
        textMesh.color = Color.white;
        textMesh.alignment = alignment;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.text = text;
        return textMesh;
    }

    #endregion
}
