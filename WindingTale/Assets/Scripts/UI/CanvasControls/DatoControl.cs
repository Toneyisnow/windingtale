using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WindingTale.Common;

namespace WindingTale.UI.CanvasControls
{
    public class DatoControl : MonoBehaviour
    {
        private int animationId;

        private Sprite normalSprite = null;
        private Sprite talking1Sprite = null;
        private Sprite talking2Sprite = null;
        private Sprite blinkingSprite = null;

        private Image image = null;
        private float scale = 1.5f;

        private bool isTalking = false;

        public void Initialize(Transform parent, int animationId, Vector2 position, bool isFlipped = false)
        {
            this.transform.parent = parent;
            this.animationId = animationId;
            this.transform.localPosition = position;

            RectTransform rect = this.GetComponent<RectTransform>();
            // rect.SetPositionAndRotation(canvasPosition, Quaternion.Euler(0, 0, 0));
            rect.sizeDelta = new Vector2(80, 80);

            if (isFlipped)
            {
                this.transform.localScale = new Vector3(-scale, scale, 1);
            }
            else
            {
                this.transform.localScale = new Vector3(scale, scale, 1);
            }

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

        public void SetTalking(bool talking)
        {
            this.isTalking = talking;
        }

        private void Start()
        {
            // spriteRenderer.sprite = normalSprite;
        }

        private void Update()
        {
            if (isTalking)
            {
                UpdateTalking();
            }
            else
            {
                UpdateIdle();
            }
        }

        private void UpdateTalking()
        {
            int seed = (int)(Time.time * 5) % 3;
            if (seed == 0)
            {
                image.sprite = talking1Sprite;
            }
            else if (seed == 1)
            {
                image.sprite = talking2Sprite;
            }
            else
            {
                image.sprite = normalSprite;
            }
        }

        private void UpdateIdle()
        {
            if ((int)(Time.time * 10) % 30 == 1)
            {
                image.sprite = blinkingSprite;
            }
            else
            {
                image.sprite = normalSprite;
            }
        }
    }
}
