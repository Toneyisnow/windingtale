using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WindingTale.Common;

namespace Assets.Scripts.UI.Dialogs
{
    public class DatoControl : MonoBehaviour
    {
        private int animationId;

        private bool startTalking = false;

        private SpriteRenderer spriteRenderer = null;

        private Sprite normalSprite = null;

        private Sprite talking1Sprite = null;
        private Sprite talking2Sprite = null;
        private Sprite blinkingSprite = null;


        public void Initialize(int animationId, bool talking)
        {
            this.animationId = animationId;
            this.startTalking = talking;

            Texture2D texture = Resources.Load<Texture2D>(@"Datos/Dato_" + StringUtils.Digit3(animationId));
            normalSprite = Sprite.Create(texture, new Rect(0, 80, 80, 80), new Vector2(0.5f, 0.5f));
            talking1Sprite = Sprite.Create(texture, new Rect(80, 80, 80, 80), new Vector2(0.5f, 0.5f));
            talking2Sprite = Sprite.Create(texture, new Rect(0, 0, 80, 80), new Vector2(0.5f, 0.5f));
            blinkingSprite = Sprite.Create(texture, new Rect(80, 0, 80, 80), new Vector2(0.5f, 0.5f));

            spriteRenderer = this.gameObject.AddComponent<SpriteRenderer>();

            gameObject.transform.localScale = new Vector3(10, 10, 0);
            gameObject.transform.localPosition = new Vector3(0, 1, 0);
            gameObject.layer = 5;
        }

        private void Start()
        {
            spriteRenderer.sprite = normalSprite;
        }

        private void Update()
        {
            
        }


    }
}
