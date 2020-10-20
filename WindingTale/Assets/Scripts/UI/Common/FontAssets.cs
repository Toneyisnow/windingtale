using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using WindingTale.Common;

namespace Assets.Scripts.UI.Common
{
    public class FontAssets
    {
        public static GameObject ComposeTextMeshObject(string textString)
        {
            GameObject textPro = new GameObject();

            TextMeshPro textComp = textPro.AddComponent<TextMeshPro>();
            textComp.text = textString;

            TMP_FontAsset fontAssetA = Resources.Load<TMP_FontAsset>("Fonts/FontAssets/FZB_Common");
            textComp.font = fontAssetA;

            return textPro;
        }

        public static GameObject ComposeTextMeshObjectForChapter(int chapterId, string textString)
        {
            GameObject textPro = new GameObject();

            TextMeshPro textComp = textPro.AddComponent<TextMeshPro>();
            textComp.text = textString;

            //// textComp.fontSize = 20;
            textComp.overflowMode = TextOverflowModes.ScrollRect;
            textComp.enableWordWrapping = false;


            TMP_FontAsset fontAssetA = Resources.Load<TMP_FontAsset>("Fonts/FontAssets/FZB_Chapter-" + StringUtils.Digit2(chapterId));
            textComp.font = fontAssetA;

            return textPro;
        }

    }
}
