using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.Scenes.GameBattleScene
{
    /// <summary>
    /// One sub-animation of an original FlameDragon magic (e.g. a single fire column),
    /// packed by Tools/FDMagic/build_magic_strip.py into a strip of equal cells (laid out in
    /// rows, see MagicEffect.CreateStripAssets), so every frame is the same rect and the
    /// per-frame offsets from the magic's .txt are already baked in.
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

        /// <summary>
        /// Per frame: the painted pixels' left and width inside the cell, for strips whose
        /// frames do not fill the cell's width (炎龙术's head). Null means the whole width.
        /// </summary>
        public int[] PaintedLeft;
        public int[] PaintedWidth;

        /// <summary>Scales the glow behind the frames; big strips want a smaller halo round them.</summary>
        public float GlowSize = 1f;

        /// <summary>Whether beam frames (see IsBeam) are carried on upwards.</summary>
        public bool CarryBeamsUp = true;

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
        /// A frame painted from the very top of its cell down to (within a few pixels of) its
        /// bottom is a beam from the sky the original screen cut off; MagicEffect carries its
        /// top row on upwards so it never ends in mid-air. Sparks and flickers along the top
        /// edge are not beams.
        /// </summary>
        public bool IsBeam(int frame)
        {
            return CarryBeamsUp && PaintedTop[frame] == 0 && PaintedHeight[frame] >= CellHeight - BeamBottomTolerance;
        }

        private const int BeamBottomTolerance = 8;

        public float PaintedCentreX(int frame)
        {
            return PaintedLeft == null ? CellWidth * 0.5f : PaintedLeft[frame] + PaintedWidth[frame] * 0.5f;
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

        /// <summary>
        /// The strip frames to play, one per effect frame, when not simply 0..FrameCount-1 --
        /// to hold a frame, or loop a few (炎龙术 breathes its 2 fire frames 5 times).
        /// </summary>
        public int[] FrameSequence;

        /// <summary>
        /// Whether this spawn's hit frames land hits. Off for the spawns that only look like
        /// hits (轰雷术's last five thunder bombs: the original judges only the first five).
        /// </summary>
        public bool Hits = true;

        /// <summary>How many effect frames this spawn plays for.</summary>
        public int Length
        {
            get { return FrameSequence != null ? FrameSequence.Length : Strip.FrameCount; }
        }

        /// <summary>The strip frame shown on the spawn's step-th frame, or -1 outside its run.</summary>
        public int StripFrameAt(int step)
        {
            if (step < 0 || step >= Length)
            {
                return -1;
            }
            return FrameSequence != null ? FrameSequence[step] : step;
        }
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

        /// <summary>
        /// The colour the target flashes on each landed hit, or null for the battle scene's
        /// own hit colour (red). A magic that changes it usually matches ScreenFlashColor.
        /// </summary>
        public Color? HitColor = null;

        // How hard the magic sparks and lights up the scene. The defaults are the plain look
        // (圣光弹 keeps it); UseFireLook() and UseLightningLook() turn them up.

        /// <summary>Scales how many embers each eruption and its aftermath throw off.</summary>
        public float EmberAmount = 1f;

        /// <summary>Scales the embers' size and how long they live.</summary>
        public float EmberScale = 1f;
        public float EmberLife = 1f;

        /// <summary>Scales how fast embers fly up, and how far they fly out sideways.</summary>
        public float EmberSpeed = 1f;
        public float EmberSpread = 1f;

        /// <summary>
        /// Embers every burning frame throws off on top of the eruptions, per flame, at full
        /// heat (scaled down by heat); 0 for none.
        /// </summary>
        public float BurningEmbersPerFrame = 0f;

        /// <summary>Big soft licks of flame rising off every burning frame, per flame at full heat.</summary>
        public float FlameWispsPerFrame = 0f;

        /// <summary>Scales those licks' size.</summary>
        public float WispScale = 1f;

        /// <summary>Scales the additive glow behind every frame.</summary>
        public float GlowStrength = 1f;

        /// <summary>The point light: its strength at full heat, its reach, and how much it flickers (0..1).</summary>
        public float LightIntensity = 3f;
        public float LightRange = 70f;
        public float LightFlicker = 0.4f;

        /// <summary>Extra light intensity flared up on every hit, dying away within a few frames.</summary>
        public float HitLightFlare = 0f;

        /// <summary>Whether the light casts (soft) shadows off the backdrop.</summary>
        public bool LightCastsShadows = false;

        /// <summary>
        /// The fire magics' look: a shower of embers and licks of flame off everything that
        /// burns, a brighter glow, and a strong, wide, flickering light that flares on every
        /// hit and throws moving shadows across the backdrop.
        /// </summary>
        public MagicEffectDefinition UseFireLook()
        {
            EmberAmount = 2.5f;
            EmberScale = 3f;
            EmberLife = 1.4f;
            BurningEmbersPerFrame = 3f;
            FlameWispsPerFrame = 1.5f;
            WispScale = 1.5f;
            GlowStrength = 1.7f;
            LightIntensity = 6f;
            LightRange = 110f;
            LightFlicker = 0.9f;
            HitLightFlare = 4f;
            LightCastsShadows = true;
            return this;
        }

        /// <summary>
        /// The lightning magics' look, the fire look's electric cousin: big sparks shot out fast
        /// and wide that die quickly, a faint crackling haze off every bolt, a hard glow, and a
        /// strong light that strobes, flares white-hot on every hit and throws shadows.
        /// </summary>
        public MagicEffectDefinition UseLightningLook()
        {
            EmberAmount = 2.5f;
            EmberScale = 2.5f;
            EmberLife = 0.8f;
            EmberSpeed = 1.8f;
            EmberSpread = 3f;
            BurningEmbersPerFrame = 3f;
            FlameWispsPerFrame = 0.8f;
            WispScale = 1.2f;
            GlowStrength = 1.8f;
            LightIntensity = 7f;
            LightRange = 120f;
            LightFlicker = 1f;
            HitLightFlare = 6f;
            LightCastsShadows = true;
            return this;
        }

        /// <summary>Frames of the magic itself (phase 2), not counting ScreenFlash.</summary>
        public int TotalFrames
        {
            get
            {
                int total = 0;
                foreach (MagicSpawn spawn in Spawns)
                {
                    total = Mathf.Max(total, spawn.StartFrame + spawn.Length);
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
                    for (int step = 0; step < spawn.Length; step++)
                    {
                        if (spawn.Hits && spawn.Strip.HitFrame >= 0 && spawn.StripFrameAt(step) == spawn.Strip.HitFrame)
                        {
                            hitFrames.Add(spawn.StartFrame + step);
                        }
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
                Add(DragonMagic());
                Add(SkyFireMagic());
                Add(ShockMagic());
                Add(ThunderfallMagic());
                Add(ThunderstormMagic());
                Add(HolyThunderMagic());
                Add(HolyLightMagic());
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

            MagicEffectDefinition definition = new MagicEffectDefinition { MagicId = 101 }.UseFireLook();
            for (int i = 0; i < startFrames.Length; i++)
            {
                definition.Spawns.Add(new MagicSpawn { Strip = column, StartFrame = startFrames[i], ScreenPosition = positions[i] });
            }
            return definition;
        }

        /// <summary>
        /// 炎龙术 (Tools/FDMagic/FSmagic/27-炎龙术): a fire dragon's head reaches out of the top
        /// right corner of the screen until its jaws gape (11 frames), breathes fire on the
        /// target -- 2 frames, 5 times over, a hit on the second of each, with the gaping head
        /// held behind the fire -- and pulls back (5 frames). 26 frames, 5 hits.
        ///
        /// The original screen's edge cuts the head off, and in Unity that edge is in the
        /// middle of the view, so the head frames were packed fading out towards it: the
        /// dragon comes out of thin air. Fire drawn over the head, as in the GIF.
        /// </summary>
        private static MagicEffectDefinition DragonMagic()
        {
            MagicStrip prepare = new MagicStrip
            {
                TexturePath = "Magics/103/DragonPrepare",
                CellWidth = 205,
                CellHeight = 142,
                FrameCount = 11,
                PaintedLeft = new int[] { 176, 152, 110, 63, 30, 22, 34, 59, 60, 20, 0 },
                PaintedWidth = new int[] { 29, 53, 95, 142, 175, 183, 171, 146, 145, 185, 204 },
                PaintedTop = new int[] { 34, 31, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                PaintedHeight = new int[] { 60, 97, 140, 142, 129, 112, 85, 59, 58, 106, 138 },
                Heat = new float[] { 0.2f, 0.3f, 0.4f, 0.5f, 0.55f, 0.6f, 0.6f, 0.55f, 0.55f, 0.7f, 0.8f },
                GlowSize = 0.45f,
                CarryBeamsUp = false,
            };

            MagicStrip burn = new MagicStrip
            {
                TexturePath = "Magics/103/DragonBurn",
                CellWidth = 191,
                CellHeight = 122,
                FrameCount = 2,
                HitFrame = 1,
                PaintedTop = new int[] { 3, 0 },
                PaintedHeight = new int[] { 119, 122 },
                Heat = new float[] { 1f, 1f },
                GlowSize = 0.6f,
                CarryBeamsUp = false,
            };

            MagicStrip end = new MagicStrip
            {
                TexturePath = "Magics/103/DragonEnd",
                CellWidth = 214,
                CellHeight = 118,
                FrameCount = 5,
                PaintedLeft = new int[] { 3, 0, 12, 39, 71 },
                PaintedWidth = new int[] { 211, 214, 175, 175, 136 },
                PaintedTop = new int[] { 0, 0, 0, 0, 0 },
                PaintedHeight = new int[] { 113, 118, 89, 61, 35 },
                Heat = new float[] { 0.6f, 0.5f, 0.4f, 0.3f, 0.2f },
                GlowSize = 0.45f,
                CarryBeamsUp = false,
            };

            MagicEffectDefinition definition = new MagicEffectDefinition { MagicId = 103 }.UseFireLook();

            // The gaping head (prepare frame 10) is drawn on through all 10 fire frames.
            int[] reachAndHold = new int[21];
            for (int step = 0; step < reachAndHold.Length; step++)
            {
                reachAndHold[step] = Mathf.Min(step, 10);
            }
            definition.Spawns.Add(new MagicSpawn { Strip = prepare, StartFrame = 0, ScreenPosition = new Vector2Int(115, 0), FrameSequence = reachAndHold });
            definition.Spawns.Add(new MagicSpawn { Strip = burn, StartFrame = 11, ScreenPosition = new Vector2Int(1, 40), FrameSequence = new int[] { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 } });
            definition.Spawns.Add(new MagicSpawn { Strip = end, StartFrame = 21, ScreenPosition = new Vector2Int(106, 0) });
            return definition;
        }

        /// <summary>
        /// 天火术 (Tools/FDMagic/FSmagic/40-天火术, the simplified version its .txt describes --
        /// the original ran to 60-odd frames, many of them only shaking): twelve fire bombs,
        /// one starting every second frame, each falling from the top right and bursting on
        /// its 4th frame -- the hit -- then smouldering away (11 frames); and under them four
        /// beams from the sky slicing down to the bottom left, two red and two blue, which
        /// do not hit. 33 frames, 12 hits.
        ///
        /// The beams are cut off at the top of the original screen (and the blue ones, at
        /// x -30, at its left edge), so they were packed fading out towards those edges.
        /// </summary>
        private static MagicEffectDefinition SkyFireMagic()
        {
            MagicStrip bomb = new MagicStrip
            {
                TexturePath = "Magics/104/FireBomb",
                CellWidth = 178,
                CellHeight = 156,
                FrameCount = 11,
                HitFrame = 3,
                PaintedLeft = new int[] { 142, 68, 22, 11, 6, 2, 1, 1, 1, 1, 1 },
                PaintedWidth = new int[] { 36, 86, 86, 50, 60, 68, 70, 70, 70, 70, 70 },
                PaintedTop = new int[] { 0, 22, 68, 95, 90, 87, 86, 85, 85, 85, 85 },
                PaintedHeight = new int[] { 16, 67, 67, 50, 60, 66, 68, 70, 70, 70, 70 },
                Heat = new float[] { 0.3f, 0.5f, 0.6f, 1f, 0.95f, 0.8f, 0.6f, 0.4f, 0.3f, 0.2f, 0.1f },
                GlowSize = 0.3f,
            };

            int[] beamLeft = { 127, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int[] beamWidth = { 44, 171, 171, 171, 171, 171, 171, 171, 119, 87, 80 };
            int[] beamTop = { 0, 0, 0, 0, 0, 0, 0, 0, 42, 83, 91 };
            int[] beamHeight = { 19, 146, 146, 146, 146, 146, 146, 146, 104, 63, 55 };
            float[] beamHeat = { 0.4f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0.7f, 0.5f, 0.3f };

            MagicStrip redBeam = new MagicStrip
            {
                TexturePath = "Magics/104/RedBeam",
                CellWidth = 171,
                CellHeight = 146,
                FrameCount = 11,
                PaintedLeft = beamLeft,
                PaintedWidth = beamWidth,
                PaintedTop = beamTop,
                PaintedHeight = beamHeight,
                Heat = beamHeat,
                GlowSize = 0.35f,
                // Diagonal beams: fading out at the screen edge, not carried straight up.
                CarryBeamsUp = false,
            };

            MagicStrip blueBeam = new MagicStrip
            {
                TexturePath = "Magics/104/BlueBeam",
                CellWidth = 171,
                CellHeight = 146,
                FrameCount = 11,
                PaintedLeft = beamLeft,
                PaintedWidth = beamWidth,
                PaintedTop = beamTop,
                PaintedHeight = beamHeight,
                Heat = beamHeat,
                GlowSize = 0.35f,
                CarryBeamsUp = false,
                GlowColor = new Color(0.35f, 0.55f, 1f),
            };

            MagicEffectDefinition definition = new MagicEffectDefinition { MagicId = 104 }.UseFireLook();

            // Beams first, so the bombs burst over them. The blue beams' x -30 is baked into
            // their strip's fade, so both must stay there.
            definition.Spawns.Add(new MagicSpawn { Strip = redBeam, StartFrame = 0, ScreenPosition = new Vector2Int(30, 0) });
            definition.Spawns.Add(new MagicSpawn { Strip = blueBeam, StartFrame = 12, ScreenPosition = new Vector2Int(-30, 0) });
            definition.Spawns.Add(new MagicSpawn { Strip = redBeam, StartFrame = 17, ScreenPosition = new Vector2Int(110, 0) });
            definition.Spawns.Add(new MagicSpawn { Strip = blueBeam, StartFrame = 22, ScreenPosition = new Vector2Int(-30, 0) });

            Vector2Int[] bombPositions =
            {
                new Vector2Int(2, -10), new Vector2Int(72, 0), new Vector2Int(42, -20), new Vector2Int(130, -6),
                new Vector2Int(72, -20), new Vector2Int(2, -10), new Vector2Int(72, 0), new Vector2Int(32, -10),
                new Vector2Int(42, -20), new Vector2Int(130, -6), new Vector2Int(82, -18), new Vector2Int(72, -20),
            };
            for (int i = 0; i < bombPositions.Length; i++)
            {
                definition.Spawns.Add(new MagicSpawn { Strip = bomb, StartFrame = 2 * i, ScreenPosition = bombPositions[i] });
            }
            return definition;
        }

        /// <summary>
        /// 电击术 (Tools/FDMagic/FSmagic/23-电击术): ten lightning strikes of two kinds, 19
        /// frames in all. Each strike is a flicker at the top of the screen (frames 0-1; kind A
        /// throws its whole zig-zag bolt down on frame 1), a white column landing on frame 2 --
        /// the hit -- and sparks leaping off it (3-5). Eight hit frames, since two A strikes
        /// land on the same frames as two B strikes.
        ///
        /// Not quite the .txt: its A/B lists put the fourth A strike (frame 13, x 33) among the
        /// B ones (the header counts 4 A and 6 B), and its per-frame x offsets and 43px cell do
        /// not match its own GIF. The strips were rebuilt from offsets measured off the GIF (see
        /// build_magic_strip.py) into 47px cells, and the cell positions below are the .txt's
        /// strike positions moved to match -- all verified frame by frame against the GIF.
        /// Blue-violet edge flash and hit flash in place of the red.
        /// </summary>
        private static MagicEffectDefinition ShockMagic()
        {
            Color boltGlow = new Color(0.5f, 0.55f, 1f);

            MagicStrip a = new MagicStrip
            {
                TexturePath = "Magics/105/LightningA",
                CellWidth = 47,
                CellHeight = 150,
                FrameCount = 6,
                HitFrame = 2,
                PaintedTop = new int[] { 0, 0, 17, 65, 32, 0 },
                PaintedHeight = new int[] { 30, 150, 133, 78, 50, 51 },
                Heat = new float[] { 0.3f, 0.8f, 1f, 0.6f, 0.4f, 0.2f },
                GlowColor = boltGlow,
                EmberColor = new Color(0.8f, 0.85f, 1f),
                EmberHotColor = Color.white,
                EmberMidColor = new Color(0.55f, 0.6f, 1f),
                EmberCoolColor = new Color(0.35f, 0.2f, 0.85f),
            };

            MagicStrip b = new MagicStrip
            {
                TexturePath = "Magics/105/LightningB",
                CellWidth = 47,
                CellHeight = 150,
                FrameCount = 6,
                HitFrame = 2,
                PaintedTop = new int[] { 0, 0, 17, 65, 32, 0 },
                PaintedHeight = new int[] { 10, 30, 133, 78, 50, 51 },
                Heat = new float[] { 0.2f, 0.3f, 1f, 0.6f, 0.4f, 0.2f },
                GlowColor = boltGlow,
                EmberColor = a.EmberColor,
                EmberHotColor = a.EmberHotColor,
                EmberMidColor = a.EmberMidColor,
                EmberCoolColor = a.EmberCoolColor,
            };

            MagicEffectDefinition definition = new MagicEffectDefinition
            {
                MagicId = 105,
                ScreenFlashColor = new Color(0.5f, 0.3f, 1f, 0.85f),
                HitColor = new Color(0.5f, 0.3f, 1f),
            }.UseLightningLook();

            int[] aStarts = { 2, 7, 9, 13 };
            int[] aCellX = { 62, 92, 112, 42 };
            for (int i = 0; i < aStarts.Length; i++)
            {
                definition.Spawns.Add(new MagicSpawn { Strip = a, StartFrame = aStarts[i], ScreenPosition = new Vector2Int(aCellX[i], 0) });
            }

            int[] bStarts = { 0, 3, 5, 7, 9, 11 };
            int[] bCellX = { 42, 82, 52, 42, 72, 102 };
            for (int i = 0; i < bStarts.Length; i++)
            {
                definition.Spawns.Add(new MagicSpawn { Strip = b, StartFrame = bStarts[i], ScreenPosition = new Vector2Int(bCellX[i], 0) });
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

            MagicEffectDefinition definition = new MagicEffectDefinition { MagicId = 102 }.UseFireLook();
            for (int i = 0; i < startFrames.Length; i++)
            {
                definition.Spawns.Add(new MagicSpawn { Strip = blue, StartFrame = startFrames[i], ScreenPosition = bluePositions[i] });
                definition.Spawns.Add(new MagicSpawn { Strip = red, StartFrame = startFrames[i], ScreenPosition = redPositions[i] });
            }
            return definition;
        }

        /// <summary>The blue-violet the lightning magics flash the screen edge and their targets with.</summary>
        private static readonly Color LightningFlash = new Color(0.5f, 0.3f, 1f, 0.85f);
        private static readonly Color LightningHit = new Color(0.5f, 0.3f, 1f);

        /// <summary>
        /// 落雷术 (Tools/FDMagic/FSmagic/25-落雷术): ten big lightning strikes of two kinds, like
        /// 电击术's, 17 frames in all. Each is a flicker at the top of the screen (frames 0-1),
        /// the zig-zag bolt striking down on frame 2 -- the hit -- and bursting on the ground
        /// (3-5). Ten hit frames.
        ///
        /// Not quite the .txt: its A and B per-frame offset lists are the wrong way round (its
        /// "A" list is the 闪电B folder's frames) and a few pixels out, and three of its four
        /// A strike positions are 5px right of where the GIF draws them. The strips were rebuilt
        /// from offsets measured off the GIF (see build_magic_strip.py) and the positions below
        /// are the GIF's -- verified frame by frame. Kind A here is the 闪电A folder, which
        /// plays at the .txt's B start frames. Blue-violet flashes, as 电击术.
        /// </summary>
        private static MagicEffectDefinition ThunderfallMagic()
        {
            Color boltGlow = new Color(0.5f, 0.55f, 1f);

            MagicStrip a = new MagicStrip
            {
                TexturePath = "Magics/106/LightningA",
                CellWidth = 82,
                CellHeight = 149,
                FrameCount = 6,
                HitFrame = 2,
                PaintedLeft = new int[] { 12, 4, 1, 7, 0, 41 },
                PaintedWidth = new int[] { 5, 15, 81, 74, 76, 13 },
                PaintedTop = new int[] { 0, 0, 0, 30, 47, 79 },
                PaintedHeight = new int[] { 16, 39, 149, 115, 87, 52 },
                Heat = new float[] { 0.2f, 0.35f, 1f, 0.7f, 0.45f, 0.2f },
                GlowSize = 0.6f,
                GlowColor = boltGlow,
                EmberColor = new Color(0.8f, 0.85f, 1f),
                EmberHotColor = Color.white,
                EmberMidColor = new Color(0.55f, 0.6f, 1f),
                EmberCoolColor = new Color(0.35f, 0.2f, 0.85f),
            };

            MagicStrip b = new MagicStrip
            {
                TexturePath = "Magics/106/LightningB",
                CellWidth = 82,
                CellHeight = 149,
                FrameCount = 6,
                HitFrame = 2,
                PaintedLeft = new int[] { 64, 62, 1, 0, 5, 23 },
                PaintedWidth = new int[] { 5, 15, 81, 74, 76, 16 },
                PaintedTop = new int[] { 0, 0, 0, 25, 39, 55 },
                PaintedHeight = new int[] { 16, 39, 149, 120, 95, 65 },
                Heat = a.Heat,
                GlowSize = a.GlowSize,
                GlowColor = boltGlow,
                EmberColor = a.EmberColor,
                EmberHotColor = a.EmberHotColor,
                EmberMidColor = a.EmberMidColor,
                EmberCoolColor = a.EmberCoolColor,
            };

            MagicEffectDefinition definition = new MagicEffectDefinition
            {
                MagicId = 106,
                ScreenFlashColor = LightningFlash,
                HitColor = LightningHit,
            }.UseLightningLook();

            int[] aStarts = { 0, 3, 5, 6, 8, 9 };
            int[] aCellX = { 30, 70, 40, 30, 60, 100 };
            for (int i = 0; i < aStarts.Length; i++)
            {
                definition.Spawns.Add(new MagicSpawn { Strip = a, StartFrame = aStarts[i], ScreenPosition = new Vector2Int(aCellX[i], 0) });
            }

            int[] bStarts = { 1, 7, 10, 11 };
            int[] bCellX = { 50, 80, 90, 30 };
            for (int i = 0; i < bStarts.Length; i++)
            {
                definition.Spawns.Add(new MagicSpawn { Strip = b, StartFrame = bStarts[i], ScreenPosition = new Vector2Int(bCellX[i], 0) });
            }
            return definition;
        }

        /// <summary>
        /// 轰雷术 (Tools/FDMagic/FSmagic/33-轰雷术), 24 frames:
        ///  - frames 1-6: five dark balls spread out in a ring round the caster;
        ///  - frames 7-17: a lightning bolt a frame, fired from each ball in turn at the target,
        ///    the ball staying behind; each bolt's thunder bomb bursts on the target a frame
        ///    later. Only the first five bombs hit (frames 8-12): five hits;
        ///  - frames 18-23: the ring gathers back in and is gone.
        /// Balls not yet fired from sit still at their places. As in the GIF, the bolts and bombs
        /// still playing when the ring gathers in are cut short there.
        ///
        /// The ring frames are the one ball image drawn five times (build_magic_strip.py); its
        /// spread is the .txt's prepare/end region positions, its shape measured off the GIF.
        /// Not quite the .txt: its fourth bomb position {-60,16} is {-60,-16} in the GIF.
        /// Blue-violet flashes, as 电击术.
        /// </summary>
        private static MagicEffectDefinition ThunderstormMagic()
        {
            Color boltGlow = new Color(0.5f, 0.55f, 1f);

            MagicStrip ring = new MagicStrip
            {
                TexturePath = "Magics/107/BallRing",
                CellWidth = 99,
                CellHeight = 115,
                FrameCount = 6,
                PaintedTop = new int[] { 0, 0, 0, 0, 0, 0 },
                PaintedHeight = new int[] { 115, 115, 115, 115, 115, 115 },
                Heat = new float[] { 0.15f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f },
                GlowSize = 0.4f,
                GlowColor = new Color(0.3f, 0.35f, 0.9f),
                CarryBeamsUp = false,
            };

            MagicStrip ball = new MagicStrip
            {
                TexturePath = "Magics/107/Ball",
                CellWidth = 33,
                CellHeight = 33,
                FrameCount = 1,
                PaintedTop = new int[] { 0 },
                PaintedHeight = new int[] { 33 },
                Heat = new float[] { 0.1f },
                GlowColor = ring.GlowColor,
                CarryBeamsUp = false,
            };

            MagicStrip bolt = new MagicStrip
            {
                TexturePath = "Magics/107/Lightning",
                CellWidth = 185,
                CellHeight = 95,
                FrameCount = 5,
                PaintedLeft = new int[] { 0, 87, 102, 141, 141 },
                PaintedWidth = new int[] { 185, 95, 76, 33, 33 },
                PaintedTop = new int[] { 0, 39, 41, 45, 45 },
                PaintedHeight = new int[] { 95, 50, 43, 33, 33 },
                Heat = new float[] { 1f, 0.6f, 0.4f, 0.15f, 0.15f },
                GlowSize = 0.35f,
                GlowColor = boltGlow,
                CarryBeamsUp = false,
                EmberColor = new Color(0.8f, 0.85f, 1f),
                EmberHotColor = Color.white,
                EmberMidColor = new Color(0.55f, 0.6f, 1f),
                EmberCoolColor = new Color(0.35f, 0.2f, 0.85f),
            };

            MagicStrip bomb = new MagicStrip
            {
                TexturePath = "Magics/107/ThunderBomb",
                CellWidth = 143,
                CellHeight = 111,
                FrameCount = 5,
                HitFrame = 0,
                PaintedLeft = new int[] { 33, 17, 12, 6, 0 },
                PaintedWidth = new int[] { 85, 113, 122, 131, 143 },
                PaintedTop = new int[] { 26, 12, 8, 4, 0 },
                PaintedHeight = new int[] { 63, 90, 98, 102, 111 },
                Heat = new float[] { 1f, 0.7f, 0.45f, 0.25f, 0.1f },
                GlowSize = 0.3f,
                GlowColor = boltGlow,
                CarryBeamsUp = false,
                EmberColor = new Color(0.8f, 0.85f, 1f),
                EmberHotColor = Color.white,
                EmberMidColor = new Color(0.55f, 0.6f, 1f),
                EmberCoolColor = new Color(0.35f, 0.2f, 0.85f),
            };

            MagicEffectDefinition definition = new MagicEffectDefinition
            {
                MagicId = 107,
                ScreenFlashColor = LightningFlash,
                HitColor = LightningHit,
            }.UseLightningLook();

            // Everything still playing is cut short on the frame the ring gathers in.
            const int gatherFrame = 18;

            definition.Spawns.Add(new MagicSpawn { Strip = ring, StartFrame = 1, ScreenPosition = new Vector2Int(151, 44) });

            // The five bolts' places; each bolt's ball is at its place + (141, 45).
            Vector2Int[] boltPositions =
            {
                new Vector2Int(76, 40), new Vector2Int(51, 81), new Vector2Int(51, -1), new Vector2Int(10, 14), new Vector2Int(10, 52),
            };
            for (int place = 1; place < boltPositions.Length; place++)
            {
                definition.Spawns.Add(new MagicSpawn
                {
                    Strip = ball,
                    StartFrame = 7,
                    ScreenPosition = boltPositions[place] + new Vector2Int(141, 45),
                    FrameSequence = new int[place],
                });
            }

            for (int i = 0; i < 11; i++)
            {
                int start = 7 + i;
                definition.Spawns.Add(new MagicSpawn
                {
                    Strip = bolt,
                    StartFrame = start,
                    ScreenPosition = boltPositions[i % 5],
                    FrameSequence = Run(Mathf.Min(bolt.FrameCount, gatherFrame - start)),
                });
            }

            Vector2Int[] bombPositions =
            {
                new Vector2Int(6, 10), new Vector2Int(-19, 51), new Vector2Int(-19, -31), new Vector2Int(-60, -16), new Vector2Int(-60, 22),
            };
            for (int i = 0; i < 10; i++)
            {
                int start = 8 + i;
                definition.Spawns.Add(new MagicSpawn
                {
                    Strip = bomb,
                    StartFrame = start,
                    ScreenPosition = bombPositions[i % 5],
                    FrameSequence = Run(Mathf.Min(bomb.FrameCount, gatherFrame - start)),
                    Hits = i < 5,
                });
            }

            definition.Spawns.Add(new MagicSpawn { Strip = ring, StartFrame = gatherFrame, ScreenPosition = new Vector2Int(151, 44), FrameSequence = new int[] { 5, 4, 3, 2, 1, 0 } });
            return definition;
        }

        /// <summary>
        /// 神雷术 (Tools/FDMagic/FSmagic/38-神雷术): eight bolts from the sky, one every second
        /// frame, 19 frames in all. Each is a flicker at the top of the screen (frame 0), a
        /// huge white bolt on frame 1 -- the hit -- and thinner bolts dying away (2-4). Eight
        /// hits.
        ///
        /// Not quite the .txt (its header counts 6 bolts, its lists 8, the GIF 8): its offsets
        /// for frames 1-4 are 3-7px left of where the GIF draws them, so the strip was packed
        /// with offsets measured off the GIF, verified frame by frame. Blue-violet flashes, as
        /// the other lightning magics.
        /// </summary>
        private static MagicEffectDefinition HolyThunderMagic()
        {
            MagicStrip bolt = new MagicStrip
            {
                TexturePath = "Magics/108/HolyBolt",
                CellWidth = 62,
                CellHeight = 148,
                FrameCount = 5,
                HitFrame = 1,
                PaintedLeft = new int[] { 21, 3, 18, 20, 32 },
                PaintedWidth = new int[] { 17, 59, 37, 32, 7 },
                PaintedTop = new int[] { 0, 0, 0, 0, 0 },
                PaintedHeight = new int[] { 37, 148, 143, 144, 144 },
                Heat = new float[] { 0.3f, 1f, 0.6f, 0.45f, 0.25f },
                GlowSize = 0.8f,
                GlowColor = new Color(0.5f, 0.55f, 1f),
                EmberColor = new Color(0.8f, 0.85f, 1f),
                EmberHotColor = Color.white,
                EmberMidColor = new Color(0.55f, 0.6f, 1f),
                EmberCoolColor = new Color(0.35f, 0.2f, 0.85f),
            };

            MagicEffectDefinition definition = new MagicEffectDefinition
            {
                MagicId = 108,
                ScreenFlashColor = LightningFlash,
                HitColor = LightningHit,
            }.UseLightningLook();

            int[] positions = { 27, -13, 67, 127, 37, 77, 107, 57 };
            for (int i = 0; i < positions.Length; i++)
            {
                definition.Spawns.Add(new MagicSpawn { Strip = bolt, StartFrame = 2 * i, ScreenPosition = new Vector2Int(positions[i], 0) });
            }
            return definition;
        }

        /// <summary>
        /// 圣光弹 (Tools/FDMagic/FSmagic/29-圣光弹): sixteen beams of holy light, one every second
        /// frame, shot at the target in turn from the lower right, upper right, bottom and
        /// middle right -- a tip, then the whole beam -- each leaving a light orb on the target
        /// that bursts on its second frame, the hit, and fades (6 frames). 38 frames, 16 hits.
        ///
        /// The beams come in from beyond the original screen's right and bottom edges, so they
        /// were packed fading out towards those. Two positions measured off the GIF rather than
        /// the .txt (see build_magic_strip.py), and its orb position {,-34,4} read as {-34,4}.
        /// Pale holy-blue edge flash and hit flash, one colour family.
        /// </summary>
        private static MagicEffectDefinition HolyLightMagic()
        {
            Color lightGlow = new Color(0.85f, 0.9f, 1f);

            MagicStrip upper = Beam("Magics/109/BeamUpper", 251, 60, new[] { 156, 0 }, new[] { 95, 251 }, new[] { 0, 0 }, new[] { 33, 60 }, lightGlow);
            MagicStrip middle = Beam("Magics/109/BeamMiddle", 253, 70, new[] { 200, 0 }, new[] { 53, 253 }, new[] { 15, 0 }, new[] { 41, 70 }, lightGlow);
            MagicStrip lower = Beam("Magics/109/BeamLower", 250, 128, new[] { 128, 0 }, new[] { 122, 250 }, new[] { 86, 0 }, new[] { 42, 128 }, lightGlow);
            MagicStrip bottom = Beam("Magics/109/BeamBottom", 116, 132, new[] { 22, 0 }, new[] { 58, 116 }, new[] { 95, 0 }, new[] { 37, 132 }, lightGlow);

            MagicStrip orb = new MagicStrip
            {
                TexturePath = "Magics/109/LightOrb",
                CellWidth = 145,
                CellHeight = 112,
                FrameCount = 6,
                HitFrame = 1,
                PaintedLeft = new int[] { 60, 35, 19, 14, 8, 2 },
                PaintedWidth = new int[] { 41, 85, 113, 122, 131, 143 },
                PaintedTop = new int[] { 39, 27, 13, 9, 5, 1 },
                PaintedHeight = new int[] { 41, 63, 90, 98, 102, 111 },
                Heat = new float[] { 0.7f, 1f, 0.7f, 0.45f, 0.25f, 0.1f },
                GlowSize = 0.3f,
                GlowColor = lightGlow,
                CarryBeamsUp = false,
                EmberColor = new Color(0.9f, 0.95f, 1f),
                EmberHotColor = Color.white,
                EmberMidColor = new Color(0.7f, 0.8f, 1f),
                EmberCoolColor = new Color(0.4f, 0.55f, 0.95f),
            };

            MagicEffectDefinition definition = new MagicEffectDefinition
            {
                MagicId = 109,
                ScreenFlashColor = new Color(0.7f, 0.85f, 1f, 0.9f),
                HitColor = new Color(0.45f, 0.65f, 1f),
            };

            // Beams first, so the orbs burst over them.
            foreach (int start in new[] { 2, 8, 18, 24 })
            {
                definition.Spawns.Add(new MagicSpawn { Strip = upper, StartFrame = start, ScreenPosition = new Vector2Int(69, 0) });
            }
            foreach (int start in new[] { 6, 14, 22, 30 })
            {
                definition.Spawns.Add(new MagicSpawn { Strip = middle, StartFrame = start, ScreenPosition = new Vector2Int(67, 73) });
            }
            foreach (int start in new[] { 0, 10, 16, 26 })
            {
                definition.Spawns.Add(new MagicSpawn { Strip = lower, StartFrame = start, ScreenPosition = new Vector2Int(70, 72) });
            }
            foreach (int start in new[] { 4, 12, 20, 28 })
            {
                definition.Spawns.Add(new MagicSpawn { Strip = bottom, StartFrame = start, ScreenPosition = new Vector2Int(39, 68) });
            }

            Vector2Int[] orbPositions =
            {
                new Vector2Int(-2, 21), new Vector2Int(-1, -10), new Vector2Int(-34, 4), new Vector2Int(-2, 54),
                new Vector2Int(-1, -10), new Vector2Int(-2, 21), new Vector2Int(-34, 4), new Vector2Int(-2, 54),
                new Vector2Int(-2, 21), new Vector2Int(-1, -10), new Vector2Int(-34, 4), new Vector2Int(-2, 54),
                new Vector2Int(-1, -10), new Vector2Int(-2, 21), new Vector2Int(-34, 4), new Vector2Int(-2, 54),
            };
            for (int i = 0; i < orbPositions.Length; i++)
            {
                definition.Spawns.Add(new MagicSpawn { Strip = orb, StartFrame = 2 + 2 * i, ScreenPosition = orbPositions[i] });
            }
            return definition;
        }

        /// <summary>
        /// One of 圣光弹's beams: a tip frame, then the whole beam, packed at their positions in
        /// the beam's region. No hit, and never carried upwards -- they fade at the screen edge.
        /// </summary>
        private static MagicStrip Beam(string texturePath, int width, int height, int[] left, int[] paintedWidth,
            int[] top, int[] paintedHeight, Color glow)
        {
            return new MagicStrip
            {
                TexturePath = texturePath,
                CellWidth = width,
                CellHeight = height,
                FrameCount = 2,
                PaintedLeft = left,
                PaintedWidth = paintedWidth,
                PaintedTop = top,
                PaintedHeight = paintedHeight,
                Heat = new float[] { 0.5f, 1f },
                GlowSize = 0.3f,
                GlowColor = glow,
                CarryBeamsUp = false,
            };
        }

        /// <summary>Frames 0..count-1.</summary>
        private static int[] Run(int count)
        {
            int[] frames = new int[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = i;
            }
            return frames;
        }
    }
}
