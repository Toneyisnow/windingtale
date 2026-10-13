using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// Builds one column of light hanging from under the board into the void (chapter 24,
    /// see LightShaftDefinition). The 2D art painted each shaft running south down the map
    /// from under the island; here it drops straight down instead. The field camera looks
    /// down at 45 degrees with yaw 180, so a point h below the board lands on screen exactly
    /// where the ground point h further south would -- a column hanging from the shaft's
    /// first painted row covers the same stretch of screen as the painted shaft did, with
    /// the island's own tiles in front of its top just as the cliff was in the art.
    ///
    /// The column is an elliptical cylinder as wide as the painted shaft and at most
    /// MaxDepth deep, open at the bottom. Its colours and the bright / weak flicker are the
    /// Custom/LightShaft shader's; the mesh only supplies uv.x across the silhouette and
    /// uv.y down the length.
    /// </summary>
    public static class LightShaft
    {
        private const string ShaderName = "Custom/LightShaft";

        // Matches the shader's default: five bright / weak cycles a second.
        private const float FlickersPerSecond = 5f;

        // How deep (north-south) a column is at most, in world units. A true cylinder for
        // the narrow shafts; the wide centre one is flattened so its back does not reach
        // out from under the island.
        private const float MaxDepth = 3f;

        // The top sits this far below the board so it never fights the tiles' surface;
        // the column is moved north by the same amount, which at 45 degrees keeps its
        // front edge on screen where the definition says.
        private const float TopBelowBoard = 0.3f;

        // How far past the map's bottom edge (as seen at 45 degrees) the column carries on,
        // fading out, in world units.
        private const float RunOut = 24f;

        private const int Segments = 32;

        // One original 24-pixel tile is 2 world units.
        private const float UnitsPerTile = 2f;
        private const float PixelsPerTile = 24f;

        public static GameObject Create(Transform parent, LightShaftDefinition def, int mapHeight)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("[LightShaft] Shader '" + ShaderName + "' not found.");
                return null;
            }

            float width = def.Width * UnitsPerTile;
            float depth = Mathf.Min(width, MaxDepth);
            float top = -TopBelowBoard;

            // World x is -2X, z is 2Y (MapCoordinate); the camera looks towards -z, so the
            // face it sees is the column's +z side.
            float frontZ = def.Y * UnitsPerTile + top;
            Vector3 centre = new Vector3(-def.X * UnitsPerTile, top, frontZ - depth / 2f);

            // At 45 degrees, dropping h reads as running h south: the painted shaft reached
            // the map's bottom edge, so the solid part does too, then it fades.
            float toMapEdge = (mapHeight + 0.5f) * UnitsPerTile - frontZ;
            float length = Mathf.Max(1f, toMapEdge) + RunOut;

            GameObject obj = new GameObject("LightShaft");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = centre;

            obj.AddComponent<MeshFilter>().sharedMesh = BuildMesh(width / 2f, depth / 2f, length);

            Material material = new Material(shader);
            float edgePixels = def.EdgePixels > 0 ? def.EdgePixels : 6;
            material.SetFloat("_EdgeFraction", edgePixels / (def.Width * PixelsPerTile));
            material.SetFloat("_FlickerHz", FlickersPerSecond);
            material.SetFloat("_FadeStart", Mathf.Clamp01(toMapEdge / length));

            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return obj;
        }

        /// <summary>
        /// An open-bottomed elliptical tube from y = 0 down to y = -length, radii rx (x) and
        /// rz (z), with a cap on top. uv.x is the point's place across the silhouette as seen
        /// from the front (0 at -x, 1 at +x), so the shader's ramp lands on the edges.
        /// </summary>
        private static Mesh BuildMesh(float rx, float rz, float length)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            for (int k = 0; k <= Segments; k++)
            {
                float a = 2f * Mathf.PI * k / Segments;
                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);
                float u = 0.5f + 0.5f * cos;
                vertices.Add(new Vector3(rx * cos, 0f, rz * sin));
                uvs.Add(new Vector2(u, 0f));
                vertices.Add(new Vector3(rx * cos, -length, rz * sin));
                uvs.Add(new Vector2(u, 1f));
            }
            for (int k = 0; k < Segments; k++)
            {
                int t0 = 2 * k, b0 = 2 * k + 1, t1 = 2 * k + 2, b1 = 2 * k + 3;
                // Outward-facing, clockwise as seen from outside (Unity's front face).
                triangles.Add(t0); triangles.Add(t1); triangles.Add(b0);
                triangles.Add(t1); triangles.Add(b1); triangles.Add(b0);
            }

            // Top cap, in the core colour.
            int centreIndex = vertices.Count;
            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0f));
            int rim = vertices.Count;
            for (int k = 0; k <= Segments; k++)
            {
                float a = 2f * Mathf.PI * k / Segments;
                vertices.Add(new Vector3(rx * Mathf.Cos(a), 0f, rz * Mathf.Sin(a)));
                uvs.Add(new Vector2(0.5f, 0f));
            }
            for (int k = 0; k < Segments; k++)
            {
                triangles.Add(centreIndex); triangles.Add(rim + k + 1); triangles.Add(rim + k);
            }

            Mesh mesh = new Mesh();
            mesh.name = "LightShaft";
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
