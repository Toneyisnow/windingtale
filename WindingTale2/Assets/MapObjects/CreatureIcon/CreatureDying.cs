using System;
using System.Collections;
using UnityEngine;


namespace WindingTale.MapObjects.CreatureIcon
{
    public class CreatureDying : MonoBehaviour
    {
        private float initialRotateSpeed = 180.0f;

        private Vector3 rotationDirection = new Vector3(0, 2, 0);

        private DateTime startTime;

        private static float dyingDuration = 2500f;

        private bool rotationFinished = false;

        void Start()
        {
            startTime = DateTime.Now;
        }

        void Update()
        {
            if (rotationFinished) return;

            float elapsed = (float)(DateTime.Now - startTime).TotalMilliseconds;
            float t = Mathf.Clamp01(elapsed / dyingDuration);
            float rotateSpeed = Mathf.Lerp(initialRotateSpeed, initialRotateSpeed * 2f, t);
            transform.Rotate(rotateSpeed * rotationDirection * Time.deltaTime);

            if (elapsed > dyingDuration)
            {
                rotationFinished = true;
                SpawnExplosion();
            }
        }

        private void SpawnExplosion()
        {
            // Measure creature bounds before hiding, then hide renderers
            Bounds creatureBounds = new();
            bool first = true;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (first) { creatureBounds = r.bounds; first = false; }
                else creatureBounds.Encapsulate(r.bounds);
                r.enabled = false;
            }

            const int frameCount = 8;
            Mesh[] frames = new Mesh[frameCount];
            Material[] materials = null;

            for (int i = 0; i < frameCount; i++)
            {
                GameObject model = Resources.Load<GameObject>($"Animations/exploration/explosion_{i:D2}");
                if (model != null)
                {
                    MeshFilter mf = model.GetComponentInChildren<MeshFilter>();
                    if (mf != null) frames[i] = mf.sharedMesh;
                    if (i == 0)
                    {
                        MeshRenderer mr = model.GetComponentInChildren<MeshRenderer>();
                        if (mr != null) materials = mr.sharedMaterials;
                    }
                }
            }

            GameObject explosionGO = new("Explosion");
            explosionGO.transform.position = transform.position;

            // Scale explosion mesh (36 units) to match creature bounding box
            float creatureSize = first ? 2f : Mathf.Max(creatureBounds.size.x, creatureBounds.size.y, creatureBounds.size.z);
            explosionGO.transform.localScale = Vector3.one * (creatureSize / 36f);

            explosionGO.AddComponent<MeshFilter>();
            MeshRenderer explosionMR = explosionGO.AddComponent<MeshRenderer>();
            if (materials != null) explosionMR.sharedMaterials = materials;

            ExplosionPlayer player = explosionGO.AddComponent<ExplosionPlayer>();
            player.frames = frames;
            player.framesPerSecond = 6f;
            player.destroyOnEnd = true;

            StartCoroutine(WaitForExplosionAndDestroy(explosionGO));
        }

        private IEnumerator WaitForExplosionAndDestroy(GameObject explosionGO)
        {
            while (explosionGO != null)
                yield return null;

            Destroy(gameObject);
        }
    }
}
