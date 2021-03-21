using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WindingTale.Common;
using WindingTale.Core.Definitions;
using WindingTale.Core.Definitions.Items;
using WindingTale.UI.Common;

namespace WindingTale.UI.CanvasControls
{
    public class CanvasControl : MonoBehaviour
    {

        protected TextMeshPro RenderText(string content, string anchorName, FontAssets.FontSizeType sizeType)
        {
            TextMeshPro textObj = FontAssets.ComposeTextMeshObject(content, sizeType);

            textObj.transform.parent = this.transform.Find(anchorName);
            textObj.rectTransform.pivot = new Vector2(0, 1);
            textObj.transform.localPosition = new Vector3(0, 0, 0);
            textObj.transform.localScale = new Vector3(5, 5, 1);
            textObj.gameObject.layer = 5;

            return textObj;
        }

        protected string GetItemDescription(ItemDefinition item)
        {
            if (item == null)
            {
                return null;
            }

            string description = string.Empty;
            if (item is AttackItemDefinition)
            {
                AttackItemDefinition attackItem = item as AttackItemDefinition;

                description = string.Format(@"+AP {0}", StringUtils.Digit2(attackItem.Ap));
                if (attackItem.Dp > 0)
                {
                    description += string.Format(@" +DP {0}", StringUtils.Digit2(attackItem.Dp));
                }
                if (attackItem.Dp < 0)
                {
                    description += string.Format(@" -DP {0}", StringUtils.Digit2(-attackItem.Dp));
                }
                return description;
            }
            if (item is DefendItemDefinition)
            {
                DefendItemDefinition defendItem = item as DefendItemDefinition;
                description = string.Format(@"+DP {0}", StringUtils.Digit2(defendItem.Dp));
                return description;
            }
            if (item is ConsumableItemDefinition)
            {
                ConsumableItemDefinition consumableItem = item as ConsumableItemDefinition;
                string prefix = string.Empty;
                switch(consumableItem.UseType)
                {
                    case ItemUseType.Hp: prefix = "HP"; break;
                    case ItemUseType.Mp: prefix = "MP"; break;
                    case ItemUseType.HpMax: prefix = "HP"; break;
                    case ItemUseType.MpMax: prefix = "MP"; break;
                    case ItemUseType.Dx: prefix = "DX"; break;
                    case ItemUseType.Mv: prefix = "MV"; break;
                    default: break;
                }

                description = string.Format(@"+{0}{1}", prefix, consumableItem.Quantity);
                return description;
            }

            return string.Empty;
        }

        protected string GetMagicDescription(MagicDefinition magic)
        {
            return string.Format(@"-MP{0}", StringUtils.Digit2(magic.MpCost));
        }

        protected static Sprite LoadSprite(string resourcePath, float width, float height)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}