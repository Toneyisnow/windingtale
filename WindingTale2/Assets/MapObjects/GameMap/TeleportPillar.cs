using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// The white column of light 传送术 (501) raises on a tile: a round, open tube standing
    /// on the tile and HeightTiles tall, fading out towards the top (Custom/TeleportPillar).
    /// Its diameter grows from one original pixel to a whole tile over GrowDuration; the
    /// teleport activity (ActivityFactory.CreatureTeleportActivity) waits for HasGrown,
    /// makes the creature vanish or appear, and takes the column down LingerDuration later.
    /// </summary>
    public class TeleportPillar : MonoBehaviour
    {
        private const string ShaderName = "Custom/TeleportPillar";

        public const float GrowDuration = 0.5f;
        public const float LingerDuration = 0.1f;

        // At least ten tiles high, as asked.
        private const float HeightTiles = 12f;

        // One original 24-pixel tile is 2 world units.
        private const float UnitsPerTile = 2f;
        private const float PixelsPerTile = 24f;

        private const float StartDiameter = UnitsPerTile / PixelsPerTile;
        private const float EndDiameter = UnitsPerTile;

        private const int Segments = 32;

        private static Mesh sharedMesh = null;

        private float elapsed = 0f;

        /// <summary>Whether the column has reached its full width.</summary>
        public bool HasGrown => elapsed >= GrowDuration;

        /// <summary>
        /// Raises a column with its foot at the given point of the parent's space. Null when
        /// the shader is missing.
        /// </summary>
        public static TeleportPillar Create(Transform parent, Vector3 localFoot)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("[TeleportPillar] Shader '" + ShaderName + "' not found.");
                return null;
            }

            GameObject obj = new GameObject("TeleportPillar");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localFoot;

            obj.AddComponent<MeshFilter>().sharedMesh = GetMesh();

            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(shader);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            TeleportPillar pillar = obj.AddComponent<TeleportPillar>();
            pillar.ApplyWidth();
            return pillar;
        }

        void Update()
        {
            if (HasGrown)
            {
                return;
            }

            elapsed += Time.deltaTime;
            ApplyWidth();
        }

        void OnDestroy()
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Destroy(renderer.sharedMaterial);
            }
        }

        private void ApplyWidth()
        {
            float t = Mathf.Clamp01(elapsed / GrowDuration);
            float diameter = Mathf.Lerp(StartDiameter, EndDiameter, t);
            transform.localScale = new Vector3(diameter, HeightTiles * UnitsPerTile, diameter);
        }

        /// <summary>
        /// A unit tube: diameter 1, from y = 0 up to y = 1, open at both ends. uv.y runs up
        /// the length, which is what the shader fades along.
        /// </summary>
        private static Mesh GetMesh()
        {
            if (sharedMesh != null)
            {
                return sharedMesh;
            }

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            for (int k = 0; k <= Segments; k++)
            {
                float a = 2f * Mathf.PI * k / Segments;
                float x = 0.5f * Mathf.Cos(a);
                float z = 0.5f * Mathf.Sin(a);
                float u = (float)k / Segments;
                vertices.Add(new Vector3(x, 1f, z));
                uvs.Add(new Vector2(u, 1f));
                vertices.Add(new Vector3(x, 0f, z));
                uvs.Add(new Vector2(u, 0f));
            }
            for (int k = 0; k < Segments; k++)
            {
                int t0 = 2 * k, b0 = 2 * k + 1, t1 = 2 * k + 2, b1 = 2 * k + 3;
                triangles.Add(t0); triangles.Add(t1); triangles.Add(b0);
                triangles.Add(t1); triangles.Add(b1); triangles.Add(b0);
            }

            Mesh mesh = new Mesh();
            mesh.name = "TeleportPillar";
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            sharedMesh = mesh;
            return mesh;
        }
    }
}
