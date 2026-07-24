using System;
using TMPro;
using UnityEngine;

/// <summary>
/// A one-line notice pushed over the shop's dialog stack -- "记录存储完毕！" after a save,
/// for one. It shows a single CommonStrings "Message-NN" line and waits; any key dismisses
/// it, popping back to whatever pushed it. It reports nothing but "the player moved on",
/// so a bare onClosed callback is all it carries.
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

        ShowMessage(messageId);

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
    /// Resolves the "Message-NN" line and drops it on, in the Chinese message font. The
    /// prefab ships with LiberationSans (no Chinese glyphs), so the whole FZB_Message font
    /// asset is assigned rather than only its material -- the fix the field dialogs use.
    /// </summary>
    private void ShowMessage(int messageId)
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
        string text = LocalizationManager.GetMessageString(messageId).GetLocalizedString().Replace("#", "\n");
        textMesh.text = text;
        textMesh.ForceMeshUpdate();
    }
}
