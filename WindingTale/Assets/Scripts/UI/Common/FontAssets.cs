using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using WindingTale.Common;

namespace WindingTale.UI.Common
{
    public class FontAssets
    {
        public enum FontSizeType
        {
            Digit = 32,
            Normal = 48,
        }

        public static TextMeshPro ComposeTextMeshObject(string textString, FontSizeType sizeType = FontSizeType.Normal)
        {
            GameObject textPro = new GameObject();

            TextMeshPro textComp = textPro.AddComponent<TextMeshPro>();
            textComp.text = textString;
            textComp.rectTransform.sizeDelta = new Vector2(80, 25);
            textComp.fontSize = sizeType.GetHashCode();

            textComp.overflowMode = TextOverflowModes.ScrollRect;
            textComp.enableWordWrapping = true;

            TMP_FontAsset fontAssetA = Resources.Load<TMP_FontAsset>("Fonts/FontAssets/FZB_Common");
            textComp.font = fontAssetA;

            return textComp;
        }

        public static TextMeshPro ComposeTextMeshObjectForChapter(int chapterId, string textString)
        {
            GameObject textPro = new GameObject();

            TextMeshPro textComp = textPro.AddComponent<TextMeshPro>();
            textComp.text = textString;
            textComp.rectTransform.sizeDelta = new Vector2(80, 25);
            textComp.fontSize = 48;

            textComp.overflowMode = TextOverflowModes.ScrollRect;
            textComp.enableWordWrapping = true;


            TMP_FontAsset fontAssetA = Resources.Load<TMP_FontAsset>("Fonts/FontAssets/FZB_Chapter-" + StringUtils.Digit2(chapterId));
            textComp.font = fontAssetA;

            return textComp;
        }

    }
}
