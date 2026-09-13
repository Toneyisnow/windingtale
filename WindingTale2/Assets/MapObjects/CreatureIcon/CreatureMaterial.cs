using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.MapObjects.CreatureIcon
{
    /// <summary>
    /// Re-shades a freshly instantiated creature model with Custom/VoxelCreature.
    ///
    /// The OBJ importer builds its own Standard material out of each model's .mtl,
    /// which lights a voxel face as one flat block. This keeps that material's
    /// palette texture and swaps the shader underneath it; see the shader itself
    /// for what it does differently.
    ///
    /// Call it on every creature model that gets instantiated -- the battle map,
    /// the village cursor, the shop's picker -- so a creature looks the same
    /// wherever it shows up.
    /// </summary>
    public static class CreatureMaterial
    {
        private static Shader shader;

        // One material per palette texture, not per creature: every creature of
        // the same kind (and all three of its animation frames) shares a palette,
        // so this keeps them in one batch instead of spawning a material each.
        private static readonly Dictionary<Texture, Material> materials =
            new Dictionary<Texture, Material>();

        public static void Apply(GameObject icon)
        {
            if (icon == null)
            {
                return;
            }

            if (shader == null)
            {
                shader = Shader.Find("Custom/VoxelCreature");
                if (shader == null)
                {
                    Debug.LogError("[CreatureMaterial] Shader 'Custom/VoxelCreature' not found!");
                    return;
                }
            }

            foreach (MeshRenderer renderer in icon.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material[] originals = renderer.sharedMaterials;
                Material[] shaded = new Material[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    if (originals[i] == null)
                    {
                        continue;
                    }

                    shaded[i] = ForPalette(originals[i].HasProperty("_MainTex")
                        ? originals[i].GetTexture("_MainTex") : null);
                }
                renderer.sharedMaterials = shaded;
            }
        }

        private static Material ForPalette(Texture palette)
        {
            if (palette == null)
            {
                return new Material(shader);
            }

            // A cached material can outlive the scene its texture was loaded for,
            // and a destroyed texture leaves the material rendering nothing -- so
            // a hit is only good while both halves are still alive.
            Material material;
            if (materials.TryGetValue(palette, out material)
                && material != null && material.mainTexture != null)
            {
                return material;
            }

            material = new Material(shader);
            material.mainTexture = palette;
            materials[palette] = material;
            return material;
        }
    }
}
