using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.ObjectModels;

namespace WindingTale.UI.Controls
{
    public class CreatureInfoBar : MonoBehaviour
    {
        public GameObject hpBar;
        public GameObject mpBar;

        private FDCreature creature = null;

        private float baseScale = 0;


        public void SetHP(int value)
        {
            if (this.creature == null || creature.Data.HpMax <= 0)
            {
                return;
            }

            Debug.Log("Set HP to value: " + value);

            float percentage = (float)value / creature.Data.HpMax;
            float scaleX = baseScale * percentage;

            Vector3 currentScale = this.hpBar.transform.localScale;
            currentScale.x = scaleX;

            this.hpBar.transform.localScale = currentScale;
        }

        public void SetMP(int value)
        {

        }

        public void Initialize(FDCreature creature)
        {
            this.creature = creature;

            // SpriteRenderer renderer = this.gameObject.AddComponent<SpriteRenderer>();
            // renderer.sprite = Resources.Load<Sprite>("OthersLegacy/CreatureInfoBarBase");
            this.mpBar.SetActive(this.creature.Data.MpMax > 0);
        }

        // Start is called before the first frame update
        void Start()
        {
            baseScale = this.hpBar.transform.localScale.x;
            Debug.Log("BaseScale = " + baseScale);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}