using UnityEngine;

namespace WindingTale.Scenes.GameBattleScene
{
    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            if (Camera.main == null) return;
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}
