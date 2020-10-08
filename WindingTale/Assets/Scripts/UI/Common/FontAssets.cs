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
        public enum AssetName
        {
            Common,
            Creature,
            Item,
            Magic,
            Message
        }

        public static GameObject ComposeTextMeshObject(AssetName asset, string textString)
        {
            GameObject textPro = new GameObject();

            TextMeshPro textComp = textPro.AddComponent<TextMeshPro>();
            textComp.text = textString;

            TMP_FontAsset fontAssetA = Resources.Load<TMP_FontAsset>("Fonts/FontAssets/FZB_" + asset.ToString());
            textComp.font = fontAssetA;

            return textPro;
        }

        public static GameObject ComposeTextMeshObjectForChapter(int chapterId, string textString)
        {
            GameObject textPro = new GameObject();

            TextMeshPro textComp = textPro.AddComponent<TextMeshPro>();
            textComp.text = textString;

            TMP_FontAsset fontAssetA = Resources.Load<TMP_FontAsset>("Fonts/FontAssets/FZB_Chapter-" + StringUtils.Digit2(chapterId));
            textComp.font = fontAssetA;

            return textPro;
        }

    }
}
