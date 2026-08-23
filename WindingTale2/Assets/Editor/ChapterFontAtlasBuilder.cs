using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace WindingTale.EditorTools
{
    /// <summary>
    /// Bakes the per-chapter TMP font atlases -- what Window > TextMeshPro > Font Asset
    /// Creator does by hand, with this project's settings baked in so a new chapter cannot
    /// quietly get the wrong ones.
    ///
    /// Each chapter ships its own atlas holding only the glyphs its dialog uses, listed in
    /// Resources/Fonts/CharacterList/CharacterList_Chapter-NN.txt, and TalkDialog swaps the
    /// asset in per chapter. A chapter whose atlas is missing or stale renders as boxes.
    ///
    /// The setting that quietly matters is the 4096x4096 atlas. Sizing is automatic, so a
    /// smaller texture does not fail -- it just lands on a smaller sampling point size, and
    /// the glyphs are then magnified to reach the same on-screen size while the SDF padding
    /// ramp stays a fixed 5 atlas pixels. That is the white halo chapter 02 shipped with
    /// when it was built at 2048 (point size 101 against chapter 01's 199 at 4096).
    /// </summary>
    public static class ChapterFontAtlasBuilder
    {
        private const string CharacterListFolder = "Assets/Resources/Fonts/CharacterList";
        private const string FontAssetFolder = "Assets/Resources/Fonts/FontAssets/zh";
        private const string SourceFontPath = "Assets/Resources/Fonts/FangZhengBlack.TTF";

        private const int AtlasSize = 4096;
        private const int AtlasPadding = 5;
        private const GlyphRenderMode RenderMode = GlyphRenderMode.SDFAA;

        // The bounds the sampling point size is searched between. Chapter 01 lands on 199
        // with 405 glyphs; a shorter chapter goes higher, which is only more resolution.
        private const int MinPointSize = 16;
        private const int MaxPointSize = 512;

        // The point size the font is probed at to find characters it simply does not have.
        // Small, so the probe is about the font's coverage and never about atlas space.
        private const int ProbePointSize = 32;

        private static readonly Regex CharacterListPattern =
            new Regex(@"^CharacterList_Chapter-(\d+)\.txt$");

        [UnityEditor.MenuItem("WindingTale/Localization/Build Missing Chapter Font Atlases")]
        public static void BuildMissing()
        {
            Build(rebuildExisting: false);
        }

        [UnityEditor.MenuItem("WindingTale/Localization/Rebuild All Chapter Font Atlases")]
        public static void RebuildAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild all chapter font atlases?",
                    "Every FZB_Chapter-NN.asset that has a character list will be baked again. " +
                    "This takes a while and rewrites tens of megabytes.",
                    "Rebuild", "Cancel"))
            {
                return;
            }

            Build(rebuildExisting: true);
        }

        [UnityEditor.MenuItem("Assets/WindingTale/Build Chapter Font Atlas", true)]
        private static bool BuildSelectedValidate()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && CharacterListPattern.IsMatch(Path.GetFileName(path));
        }

        [UnityEditor.MenuItem("Assets/WindingTale/Build Chapter Font Atlas")]
        private static void BuildSelected()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            Match match = CharacterListPattern.Match(Path.GetFileName(path));

            Font font = LoadSourceFont();
            if (font == null)
            {
                return;
            }

            BuildChapter(font, int.Parse(match.Groups[1].Value), path);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Build(bool rebuildExisting)
        {
            Font font = LoadSourceFont();
            if (font == null)
            {
                return;
            }

            int built = 0;

            foreach (string path in Directory.GetFiles(CharacterListFolder, "CharacterList_Chapter-*.txt")
                                             .OrderBy(path => path))
            {
                Match match = CharacterListPattern.Match(Path.GetFileName(path));
                if (!match.Success)
                {
                    continue;
                }

                int chapter = int.Parse(match.Groups[1].Value);
                if (!rebuildExisting && File.Exists(FontAssetPath(chapter)))
                {
                    continue;
                }

                if (BuildChapter(font, chapter, path.Replace('\\', '/')))
                {
                    built++;
                }
            }

            EditorUtility.ClearProgressBar();

            if (built == 0)
            {
                Debug.Log("Chapter font atlases: nothing to build.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool BuildChapter(Font font, int chapter, string characterListPath)
        {
            string assetName = string.Format("FZB_Chapter-{0:D2}", chapter);
            string characterSequence = File.ReadAllText(characterListPath);

            // Newlines are structure in the list file, not glyphs the dialog needs; the
            // space is, so it stays.
            string characters = new string(characterSequence
                .Where(c => c != '\r' && c != '\n')
                .Distinct()
                .ToArray());

            if (characters.Length == 0)
            {
                Debug.LogWarning(assetName + ": " + characterListPath + " has no characters, skipped.");
                return false;
            }

            EditorUtility.DisplayProgressBar("Building chapter font atlases", assetName, 0f);

            characters = DropCharactersTheFontLacks(font, assetName, characters);
            if (characters.Length == 0)
            {
                EditorUtility.ClearProgressBar();
                return false;
            }

            TMP_FontAsset fontAsset = BakeAtLargestFittingPointSize(font, assetName, characters);
            EditorUtility.ClearProgressBar();

            if (fontAsset == null)
            {
                Debug.LogError(string.Format(
                    "{0}: {1} glyphs do not fit a {2}x{2} atlas even at point size {3}.",
                    assetName, characters.Length, AtlasSize, MinPointSize));
                return false;
            }

            Save(fontAsset, assetName, chapter, characterListPath, characterSequence);

            Debug.Log(string.Format("{0}: {1} glyphs at point size {2}, {3}x{3} atlas.",
                assetName, characters.Length, fontAsset.faceInfo.pointSize, AtlasSize));

            return true;
        }

        /// <summary>
        /// The characters the font has no glyph for, warned about once and then left out.
        /// Without this the point-size search below reads "the font cannot draw this" as
        /// "the atlas is too small" and walks all the way down to its floor, failing with a
        /// misleading message.
        /// </summary>
        private static string DropCharactersTheFontLacks(Font font, string assetName, string characters)
        {
            string missing;
            TMP_FontAsset probe = Bake(font, ProbePointSize, characters, out missing);

            try
            {
                if (probe == null)
                {
                    // Could not even load the face -- Bake has logged why.
                    return string.Empty;
                }

                if (string.IsNullOrEmpty(missing))
                {
                    return characters;
                }

                Debug.LogWarning(string.Format(
                    "{0}: FangZhengBlack has no glyph for {1} character(s), left out of the atlas: {2}",
                    assetName, missing.Length, missing));

                HashSet<char> absent = new HashSet<char>(missing);
                return new string(characters.Where(c => !absent.Contains(c)).ToArray());
            }
            finally
            {
                Discard(probe);
            }
        }

        /// <summary>
        /// Auto Sizing: the largest sampling point size at which every glyph still fits one
        /// atlas texture, found the way the Font Asset Creator finds it. The winning bake is
        /// kept rather than repeated, so the search costs one render per step and no more.
        /// </summary>
        private static TMP_FontAsset BakeAtLargestFittingPointSize(Font font, string assetName, string characters)
        {
            int low = MinPointSize;
            int high = MaxPointSize;
            TMP_FontAsset best = null;

            while (low <= high)
            {
                int pointSize = (low + high) / 2;

                EditorUtility.DisplayProgressBar("Building chapter font atlases",
                    string.Format("{0} -- trying point size {1}", assetName, pointSize),
                    Mathf.InverseLerp(MaxPointSize, MinPointSize, high - low));

                string missing;
                TMP_FontAsset candidate = Bake(font, pointSize, characters, out missing);
                bool fits = candidate != null && string.IsNullOrEmpty(missing);

                if (fits)
                {
                    Discard(best);
                    best = candidate;
                    low = pointSize + 1;
                }
                else
                {
                    Discard(candidate);
                    high = pointSize - 1;
                }
            }

            return best;
        }

        /// <summary>
        /// One bake attempt. Multi-atlas support is off on purpose: the chapter atlases are
        /// single-texture, and with it on a set that does not fit would silently spill into
        /// a second texture instead of telling the search to step down.
        /// </summary>
        private static TMP_FontAsset Bake(Font font, int pointSize, string characters, out string missingCharacters)
        {
            missingCharacters = string.Empty;

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font, pointSize, AtlasPadding, RenderMode, AtlasSize, AtlasSize,
                AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);

            if (fontAsset == null)
            {
                return null;
            }

            fontAsset.TryAddCharacters(characters, out missingCharacters, includeFontFeatures: false);

            return fontAsset;
        }

        private static void Save(TMP_FontAsset fontAsset, string assetName, int chapter,
                                 string characterListPath, string characterSequence)
        {
            // Static is what ships: the atlas is baked, and the source Font is dropped so
            // the build does not carry the whole TTF as well. The property setter is what
            // clears that reference, so it has to be set rather than the field written.
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            fontAsset.name = assetName;

            fontAsset.creationSettings = new FontAssetCreationSettings
            {
                sourceFontFileName = string.Empty,
                sourceFontFileGUID = AssetDatabase.AssetPathToGUID(SourceFontPath),
                faceIndex = 0,
                pointSizeSamplingMode = 0,               // Auto Sizing
                pointSize = (int)fontAsset.faceInfo.pointSize,
                padding = AtlasPadding,
                paddingMode = 2,
                packingMode = 0,                         // Fast
                atlasWidth = AtlasSize,
                atlasHeight = AtlasSize,
                characterSetSelectionMode = 8,           // Characters from File
                characterSequence = characterSequence,
                referencedFontAssetGUID = string.Empty,
                referencedTextAssetGUID = AssetDatabase.AssetPathToGUID(characterListPath),
                fontStyle = 0,
                fontStyleModifier = 0,
                renderMode = (int)RenderMode,
                includeFontFeatures = false,
            };

            string path = FontAssetPath(chapter);

            // A rebuild replaces the file outright. Editing the existing asset in place
            // would mean reconciling the old atlas texture and material sub-assets, and
            // every reference to the font asset is by guid, which the .meta keeps.
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(fontAsset, path);

            Texture2D atlas = fontAsset.atlasTextures[0];
            atlas.name = assetName + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);

            fontAsset.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            EditorUtility.SetDirty(fontAsset);
        }

        private static string FontAssetPath(int chapter)
        {
            return string.Format("{0}/FZB_Chapter-{1:D2}.asset", FontAssetFolder, chapter);
        }

        private static Font LoadSourceFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (font == null)
            {
                Debug.LogError("Source font not found at " + SourceFontPath);
            }

            return font;
        }

        /// <summary>
        /// Throws away a bake that lost the search. The atlas texture and the material are
        /// plain objects the font asset created and nothing else owns yet, so they have to
        /// go with it or each discarded attempt leaks a 4096x4096 texture.
        /// </summary>
        private static void Discard(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return;
            }

            if (fontAsset.atlasTextures != null)
            {
                foreach (Texture2D texture in fontAsset.atlasTextures)
                {
                    if (texture != null)
                    {
                        Object.DestroyImmediate(texture);
                    }
                }
            }

            if (fontAsset.material != null)
            {
                Object.DestroyImmediate(fontAsset.material);
            }

            Object.DestroyImmediate(fontAsset);
        }
    }
}
