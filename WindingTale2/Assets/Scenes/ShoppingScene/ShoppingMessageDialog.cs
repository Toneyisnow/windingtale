using System;
using TMPro;
using UnityEngine;
using WindingTale.Core.Common;

/// <summary>
/// A one-line notice pushed over the shop's dialog stack -- "记录存储完毕！" after a save,
/// for one. It shows a single CommonStrings "Message-NN" line and waits; any key dismisses
/// it, popping back to whatever pushed it. It reports nothing but "the player moved on",
/// so a bare onClosed callback is all it carries.
///
/// Two lines it shows carry a parameter -- "钱不够！" needs none, but "{StrParam1}带不动了！"
/// (a creature's pack is full) does -- so the FDMessage overload resolves the "Message-NN"
/// line with its arguments filled in, the same way the field dialogs do.
/// </summary>
public class ShoppingMessageDialog : MonoBehaviour
{
    public GameObject MessageText;

    /// <summary>Raised when the player presses any key to dismiss the notice.</summary>
    public Action OnClosed = null;

    private bool initialized = false;

    // Ignore the frame Init ran on: the key that opened this notice (the confirm on the
    // slot picker) is still down this frame, and "any key closes" would eat it at once.
    private bool firstFrame = false;

    /// <summary>
    /// Shows CommonStrings "Message-<paramref name="messageId"/>" and starts waiting for a
    /// key. onClosed fires once, on the first key after this frame.
    /// </summary>
    public void Init(int messageId, Action onClosed)
    {
        this.OnClosed = onClosed;

        ShowText(LocalizationManager.GetMessageString(messageId).GetLocalizedString());

        firstFrame = true;
        initialized = true;
    }

    /// <summary>
    /// Shows a "Message-NN" line with its parameters filled in ({StrParam1} and the rest) and
    /// starts waiting for a key -- for the notices the shop's Buy flow raises, whose text names
    /// the creature whose pack is full. onClosed fires once, on the first key after this frame.
    /// </summary>
    public void Init(FDMessage message, Action onClosed)
    {
        this.OnClosed = onClosed;

        ShowText(LocalizationManager.GetFDMessageString(message).GetLocalizedString());

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
            firstFrame = false;
            return;
        }

        if (Input.anyKeyDown)
        {
            initialized = false; // fire once, even if the callback does not tear us down at once
            if (OnClosed != null)
            {
                OnClosed();
            }
        }
    }

    /// <summary>
    /// Drops the resolved line on, in the Chinese message font. The prefab ships with
    /// LiberationSans (no Chinese glyphs), so the whole FZB_Message font asset is assigned
    /// rather than only its material -- the fix the field dialogs use.
    /// </summary>
    private void ShowText(string text)
    {
        TextMeshProUGUI textMesh = MessageText != null ? MessageText.GetComponent<TextMeshProUGUI>() : null;
        if (textMesh == null)
        {
            return;
        }

        TMP_FontAsset messageFont = Resources.Load<TMP_FontAsset>(@"Fonts/FontAssets/zh/FZB_Message");
        if (messageFont != null)
        {
            textMesh.font = messageFont;
        }

        // '#' is the source line-break marker, same as the field dialogs use.
        textMesh.text = text.Replace("#", "\n");
        textMesh.ForceMeshUpdate();
    }
}
