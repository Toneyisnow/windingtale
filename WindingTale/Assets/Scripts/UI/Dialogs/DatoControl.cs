using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Common;

namespace Assets.Scripts.UI.Dialogs
{
    public class DatoControl : MonoBehaviour
    {
        private int animationId;

        private Sprite normalSprite = null;
        private Sprite talking1Sprite = null;
        private Sprite talking2Sprite = null;
        private Sprite blinkingSprite = null;

        private Image image = null;

        public void Initialize(Canvas canvas, int animationId, Vector2 canvasPosition)
        {
            this.transform.parent = canvas.transform;
            this.animationId = animationId;
            this.transform.localPosition = canvasPosition;

            RectTransform rect = this.GetComponent<RectTransform>();
            // rect.SetPositionAndRotation(canvasPosition, Quaternion.Euler(0, 0, 0));
            rect.sizeDelta = new Vector2(80, 80);

            this.transform.localScale = new Vector3(3.0f, 3.0f, 1);
            // rect.si
            
            Texture2D texture = Resources.Load<Texture2D>(@"Datos/Dato_" + StringUtils.Digit3(animationId));
            normalSprite = Sprite.Create(texture, new Rect(0, 80, 80, 80), new Vector2(0.5f, 0.5f));
            talking1Sprite = Sprite.Create(texture, new Rect(80, 80, 80, 80), new Vector2(0.5f, 0.5f));
            talking2Sprite = Sprite.Create(texture, new Rect(0, 0, 80, 80), new Vector2(0.5f, 0.5f));
            blinkingSprite = Sprite.Create(texture, new Rect(80, 0, 80, 80), new Vector2(0.5f, 0.5f));

            image = this.GetComponent<Image>();
            image.sprite = blinkingSprite;


            //gameObject.transform.localScale = new Vector3(10, 10, 0);
            //gameObject.transform.localPosition = new Vector3(0, 1, 0);
            this.gameObject.layer = 5;
        }

        private void Start()
        {
            // spriteRenderer.sprite = normalSprite;
        }

        private void Update()
        {
            if ((int)(Time.time * 10) % 30 == 1)
            {
                image.sprite = blinkingSprite;
                Debug.Log("Blinked!!!");
            }
            else
            {
                image.sprite = normalSprite;
            }
        }


    }
}
