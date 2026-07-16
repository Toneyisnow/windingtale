using System.Collections;
using UnityEngine;

namespace WindingTale.MapObjects.Menus
{
    /// <summary>
    /// While a menu item is the selected one, cycles its mesh between two frames
    /// (Menu_XXX_1 and Menu_XXX_2) to give it a simple 2-frame animation. Remove the
    /// component to stop; the caller restores the item's static mesh afterwards.
    /// </summary>
    public class MenuItemBlink : MonoBehaviour
    {
        // Seconds each frame is held before switching to the other one.
        public static float FrameInterval = 0.3f;

        private MeshFilter target = null;
        private Mesh frame1 = null;
        private Mesh frame2 = null;

        public void Init(MeshFilter target, Mesh frame1, Mesh frame2)
        {
            this.target = target;
            this.frame1 = frame1;
            this.frame2 = frame2;

            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            bool showSecond = false;
            while (true)
            {
                if (target != null)
                {
                    target.mesh = showSecond ? frame2 : frame1;
                }
                showSecond = !showSecond;
                yield return new WaitForSeconds(FrameInterval);
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}
