using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Scenes.GameBattleScene
{
    /// <summary>
    /// One sub-animation of an original FlameDragon magic (e.g. a single fire column),
    /// packed by Tools/FDMagic/build_magic_strip.py into a strip of equal cells: cell i's
    /// top-left pixel is (1 + i * (CellWidth + 2), 1), so every frame is the same rect and
    /// the per-frame offsets from the magic's .txt are already baked in.
    /// </summary>
    public class MagicStrip
    {
        /// <summary>Resources path of the strip texture, without extension.</summary>
        public string TexturePath;

        public int CellWidth;
        public int CellHeight;
        public int FrameCount;

        /// <summary>
        /// The frame (0-based) that lands the hit, or -1 when this strip does not hit.
        /// It is also the frame the flame erupts on, so embers are thrown off there.
        /// </summary>
        public int HitFrame = -1;

        /// <summary>
        /// Per frame: the painted pixels' top and height inside the cell, from the .txt
        /// offsets and the source PNG sizes. The glow and light follow this, since the
        /// texture itself is not readable at runtime.
        /// </summary>
        public int[] PaintedTop;
        public int[] PaintedHeight;

        /// <summary>Per frame 0..1: how hot this frame burns (glow alpha, light share).</summary>
        public float[] Heat;

        /// <summary>The glow behind this strip's frames, and its share of the light's colour.</summary>
        public Color GlowColor = new Color(1f, 0.5f, 0.15f);

        /// <summary>Embers thrown off this strip: tinted by EmberColor, cooling hot -> mid -> cool.</summary>
        public Color EmberColor = new Color(1f, 0.6f, 0.1f);
        public Color EmberHotColor = new Color(1f, 1f, 0.85f);
        public Color EmberMidColor = new Color(1f, 0.55f, 0.15f);
        public Color EmberCoolColor = new Color(0.75f, 0.12f, 0.04f);

        /// <summary>
        /// A frame painted all the way to the top of its cell is a beam the original screen
        /// cut off; MagicEffect carries its top row on upwards so it never ends in mid-air.
        /// </summary>
        public bool IsBeam(int frame)
        {
            return PaintedTop[frame] == 0 && PaintedHeight[frame] >= CellHeight;
        }
    }

    /// <summary>One placement of a strip: when it starts and where it stands on screen.</summary>
    public class MagicSpawn
    {
        public MagicStrip Strip;
        public int StartFrame;

        /// <summary>
        /// Top-left of the cell in the original 320 x 200 battle screen, as the .txt gives
        /// it ("相对屏幕左上角的坐标"). Those coordinates are for an enemy target standing on
        /// the left; MagicEffect mirrors them for a friend on the right.
        /// </summary>
        public Vector2Int ScreenPosition;
    }

    /// <summary>
    /// Everything MagicEffect needs to replay one original magic on a battle target.
    /// Transcribed from Tools/FDMagic/FSmagic/NN-name/name.txt; never invented.
    /// </summary>
    public class MagicEffectDefinition
    {
        public int MagicId;

        /// <summary>Seconds per original frame (the reference GIFs run at 60ms), both phases.</summary>
        public float FrameDuration = 0.06f;

        public List<MagicSpawn> Spawns = new List<MagicSpawn>();

        /// <summary>
        /// Phase 1, before the magic's own frames: the strength of the red ring round the
        /// screen edge on each frame. Every magic opens with it. Copied from the reference
        /// GIF (19-火焰术 frames #8-#19): five flashes -- the second held two frames -- then
        /// a 240ms hold before the magic starts.
        /// </summary>
        public float[] ScreenFlash = { 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0 };
        public Color ScreenFlashColor = new Color(1f, 0f, 0f, 0.85f);

        /// <summary>Frames of the magic itself (phase 2), not counting ScreenFlash.</summary>
        public int TotalFrames
        {
            get
            {
                int total = 0;
                foreach (MagicSpawn spawn in Spawns)
                {
                    total = Mathf.Max(total, spawn.StartFrame + spawn.Strip.FrameCount);
                }
                return total;
            }
        }

        /// <summary>
        /// Hit frames, counted once however many flames hit on the same frame (烈焰术's red
        /// and blue columns land together).
        /// </summary>
        public int HitCount
        {
            get
            {
                HashSet<int> hitFrames = new HashSet<int>();
                foreach (MagicSpawn spawn in Spawns)
                {
                    if (spawn.Strip.HitFrame >= 0)
                    {
                        hitFrames.Add(spawn.StartFrame + spawn.Strip.HitFrame);
                    }
                }
                return hitFrames.Count;
            }
        }

        private static Dictionary<int, MagicEffectDefinition> definitions = null;

        /// <summary>The effect for a magic, or null when that magic has none yet.</summary>
        public static MagicEffectDefinition Get(int magicId)
        {
            if (definitions == null)
            {
                definitions = new Dictionary<int, MagicEffectDefinition>();
                Add(FireMagic());
                Add(BlazeMagic());
            }

            MagicEffectDefinition definition;
            return definitions.TryGetValue(magicId, out definition) ? definition : null;
        }

        private static void Add(MagicEffectDefinition definition)
        {
            definitions[definition.MagicId] = definition;
        }

        /// <summary>
        /// 火焰术 (Tools/FDMagic/FSmagic/19-火焰术): seven fire columns, one starting every
        /// second frame, 28 frames in all. Each column is a thin streak that strikes the
        /// ground (frames 0-4), a scorch that spreads (5-8), the column erupting on frame 9
        /// -- the hit -- and burning out upwards (10-15).
        /// </summary>
        private static MagicEffectDefinition FireMagic()
        {
            MagicStrip column = new MagicStrip
            {
                TexturePath = "Magics/101/FireColumn",
                CellWidth = 22,
                CellHeight = 154,
                FrameCount = 16,
                HitFrame = 9,
                PaintedTop = new int[] { 0, 5, 110, 134, 148, 147, 145, 144, 144, 70, 60, 67, 73, 26, 0, 0 },
                PaintedHeight = new int[] { 12, 149, 44, 19, 5, 4, 9, 10, 10, 84, 94, 87, 81, 79, 62, 45 },
                Heat = new float[] { 0.25f, 0.4f, 0.3f, 0.25f, 0.3f, 0.35f, 0.45f, 0.5f, 0.55f, 1f, 1f, 0.95f, 0.9f, 0.6f, 0.35f, 0.15f },
            };

            int[] startFrames = { 0, 2, 4, 6, 8, 10, 12 };
            Vector2Int[] positions =
            {
                new Vector2Int(40, 0), new Vector2Int(70, -10), new Vector2Int(120, -20), new Vector2Int(80, 0),
                new Vector2Int(50, -15), new Vector2Int(100, -5), new Vector2Int(70, 0),
            };

            MagicEffectDefinition definition = new MagicEffectDefinition { MagicId = 101 };
            for (int i = 0; i < startFrames.Length; i++)
            {
                definition.Spawns.Add(new MagicSpawn { Strip = column, StartFrame = startFrames[i], ScreenPosition = positions[i] });
            }
            return definition;
        }

        /// <summary>
        /// 烈焰术 (Tools/FDMagic/FSmagic/20-烈焰术): eight pairs of columns, one blue and one
        /// red, a pair starting every second frame, 29 frames in all. The two in a pair start
        /// on the same frame and hit together, so 16 columns make 8 hits. Each column is the
        /// 火焰术 streak and scorch (frames 0-8), then rises on frame 9 -- the hit -- into a
        /// beam as tall as the screen (10-12) and burns out at the top (13-14). Both colours
        /// share the frame offsets.
        /// </summary>
        private static MagicEffectDefinition BlazeMagic()
        {
            int[] paintedTop = { 0, 5, 110, 134, 145, 147, 145, 144, 144, 88, 0, 0, 0, 0, 0 };
            int[] paintedHeight = { 12, 149, 44, 19, 8, 4, 9, 10, 10, 66, 154, 154, 154, 45, 23 };
            float[] heat = { 0.25f, 0.4f, 0.3f, 0.25f, 0.3f, 0.35f, 0.45f, 0.5f, 0.55f, 0.9f, 1f, 1f, 1f, 0.5f, 0.25f };

            MagicStrip red = new MagicStrip
            {
                TexturePath = "Magics/102/RedColumn",
                CellWidth = 22,
                CellHeight = 154,
                FrameCount = 15,
                HitFrame = 9,
                PaintedTop = paintedTop,
                PaintedHeight = paintedHeight,
                Heat = heat,
            };

            MagicStrip blue = new MagicStrip
            {
                TexturePath = "Magics/102/BlueColumn",
                CellWidth = 22,
                CellHeight = 154,
                FrameCount = 15,
                HitFrame = 9,
                PaintedTop = paintedTop,
                PaintedHeight = paintedHeight,
                Heat = heat,
                GlowColor = new Color(0.3f, 0.5f, 1f),
                EmberColor = new Color(0.6f, 0.8f, 1f),
                EmberHotColor = new Color(0.9f, 0.95f, 1f),
                EmberMidColor = new Color(0.4f, 0.6f, 1f),
                EmberCoolColor = new Color(0.1f, 0.15f, 0.6f),
            };

            int[] startFrames = { 0, 2, 4, 6, 8, 10, 12, 14 };
            Vector2Int[] bluePositions =
            {
                new Vector2Int(20, -10), new Vector2Int(40, -25), new Vector2Int(80, -30), new Vector2Int(120, -25),
                new Vector2Int(135, -10), new Vector2Int(120, 5), new Vector2Int(80, 10), new Vector2Int(40, 5),
            };
            Vector2Int[] redPositions =
            {
                new Vector2Int(135, -10), new Vector2Int(120, 5), new Vector2Int(80, 10), new Vector2Int(40, 5),
                new Vector2Int(20, -10), new Vector2Int(40, -25), new Vector2Int(80, -30), new Vector2Int(120, -25),
            };

            MagicEffectDefinition definition = new MagicEffectDefinition { MagicId = 102 };
            for (int i = 0; i < startFrames.Length; i++)
            {
                definition.Spawns.Add(new MagicSpawn { Strip = blue, StartFrame = startFrames[i], ScreenPosition = bluePositions[i] });
                definition.Spawns.Add(new MagicSpawn { Strip = red, StartFrame = startFrames[i], ScreenPosition = redPositions[i] });
            }
            return definition;
        }
    }
}
