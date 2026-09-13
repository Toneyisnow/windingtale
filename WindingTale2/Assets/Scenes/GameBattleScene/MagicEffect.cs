using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WindingTale.Scenes.GameBattleScene
{
    /// <summary>
    /// Replays one original FlameDragon magic (a MagicEffectDefinition) on a battle target,
    /// and dresses it up for the 3D scene. Two phases on one timeline:
    ///
    ///  1. the red ring round the screen edge flashes (MagicEffectDefinition.ScreenFlash);
    ///  2. the magic itself -- the original pixel frames, drawn as sprites in the target's
    ///     own sprite space so the .txt screen coordinates land exactly where they did on
    ///     the 320 x 200 screen, with an additive glow behind every flame, one point light
    ///     following (and coloured by) the hottest flames, and embers thrown off each flame
    ///     as it erupts. Beams the original screen cut off are carried on upwards. For a
    ///     friend on the right the whole layout, frames included, is mirrored.
    ///
    /// What a hit does to the target (its red flash, the HP bar) is left to onHit.
    ///
    /// Time only moves through Advance(), which Update feeds, so a capture tool can scrub
    /// the effect frame by frame too.
    /// </summary>
    public class MagicEffect : MonoBehaviour
    {
        /// <summary>Width of the original battle screen the .txt coordinates are in.</summary>
        private const int ScreenWidth = 320;

        /// <summary>
        /// Where the feet of the creature on each side stand, in original screen pixels,
        /// measured over every Resources/Fights sheet's first frame. The 1280x800 sheets
        /// are the original screen exactly; the older 1280x840 ones sit higher. The .txt
        /// coordinates are all given against the enemy's ground line, EnemyGround.
        /// </summary>
        private const int EnemyGround = 150;
        private const int EnemyGroundTallFrame = 131;
        private const int FriendGround = 192;
        private const int FriendGroundTallFrame = 186;

        /// <summary>How long the light and embers linger after the last frame.</summary>
        private const float TailSeconds = 0.6f;

        private const int EruptionEmbers = 16;
        private const int BurningEmbers = 5;
        private const int BurningEmberFrames = 3;

        private const float LightIntensity = 3f;
        private const float LightRange = 70f;

        /// <summary>How far a beam frame is carried on above its cell: a whole original screen.</summary>
        private const int BeamExtensionPixels = 200;

        /// <summary>What every flame of one strip shares.</summary>
        private class StripAssets
        {
            public Sprite[] Frames;

            /// <summary>Per frame, the cell's top row for beam frames, else null.</summary>
            public Sprite[] BeamRows;

            public ParticleSystem Embers;
        }

        private class Flame
        {
            public MagicSpawn Spawn;
            public StripAssets Assets;
            public SpriteRenderer Core;
            public SpriteRenderer Beam;
            public SpriteRenderer Glow;

            /// <summary>Top-left corner of the cell, in the effect's local space.</summary>
            public Vector3 CellTopLeft;

            /// <summary>Drawn mirrored, for a friend on the right.</summary>
            public bool Mirrored;
        }

        private MagicEffectDefinition definition;
        private Action<int> onHit;
        private Action onComplete;

        private GameObject screenFlashCanvas;
        private RawImage screenFlash;

        private readonly List<Flame> flames = new List<Flame>();
        private Light flameLight;

        private float unitsPerPixel;
        private float towardCamera;

        private readonly List<UnityEngine.Object> createdAssets = new List<UnityEngine.Object>();

        /// <summary>Frames of phase 1 this run plays: all of ScreenFlash, or none.</summary>
        private int flashFrames = 0;

        private float time = 0;
        private int lastFrame = -1;
        private int hitsLanded = 0;
        private bool completed = false;
        private bool lightPlaced = false;

        /// <summary>
        /// Starts a magic on a target body.
        /// </summary>
        /// <param name="targetBody">The target's FightBody sprite object.</param>
        /// <param name="restLocalPosition">
        /// The body's resting local position, so the magic lands where the body stands even
        /// if something is moving it when the spell goes off.
        /// </param>
        /// <param name="targetIsFriend">A friend stands on the right, so the layout is mirrored.</param>
        /// <param name="withScreenFlash">
        /// Whether to open with phase 1. An area magic flashes once, before its first target only.
        /// </param>
        /// <param name="onHit">Called at every hit frame with the cumulative damage percent.</param>
        /// <param name="onComplete">Called once the screen flash and the last original frame have played.</param>
        public static MagicEffect Play(MagicEffectDefinition definition, GameObject targetBody, Vector3 restLocalPosition,
            bool targetIsFriend, bool withScreenFlash, Action<int> onHit, Action onComplete)
        {
            GameObject root = new GameObject("MagicEffect_" + definition.MagicId);

            // Siblings with the body, in its sprite space: parenting also moves the effect
            // into the additively loaded battle scene, so it unloads with it.
            root.transform.SetParent(targetBody.transform.parent, false);
            root.transform.localPosition = restLocalPosition;
            root.transform.localRotation = targetBody.transform.localRotation;
            root.transform.localScale = targetBody.transform.localScale;

            MagicEffect effect = root.AddComponent<MagicEffect>();
            effect.flashFrames = withScreenFlash ? definition.ScreenFlash.Length : 0;
            effect.Build(definition, targetBody.GetComponent<SpriteRenderer>(), FindSceneCamera(targetBody.scene),
                targetIsFriend, onHit, onComplete);
            effect.Advance(0);
            return effect;
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (screenFlashCanvas != null)
            {
                Destroy(screenFlashCanvas);
            }

            foreach (UnityEngine.Object asset in createdAssets)
            {
                Destroy(asset);
            }
        }

        /// <summary>
        /// The camera that films the battle. Not Camera.main: the battle scene is loaded on
        /// top of the field scene, whose camera is tagged MainCamera too and stays enabled.
        /// </summary>
        public static Camera FindSceneCamera(Scene scene)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (Camera camera in rootObject.GetComponentsInChildren<Camera>())
                {
                    if (camera.isActiveAndEnabled)
                    {
                        return camera;
                    }
                }
            }
            return null;
        }

        private void Build(MagicEffectDefinition definition, SpriteRenderer target, Camera camera, bool targetIsFriend,
            Action<int> onHit, Action onComplete)
        {
            this.definition = definition;
            this.onHit = onHit;
            this.onComplete = onComplete;

            // The fight sprites are the original frame scaled up (1280 texels for 320
            // pixels), so one original pixel is this many local units.
            Sprite frameSprite = target.sprite;
            float texelsPerPixel = frameSprite.rect.width / ScreenWidth;
            unitsPerPixel = texelsPerPixel / frameSprite.pixelsPerUnit;
            int frameHeight = Mathf.RoundToInt(frameSprite.rect.height / texelsPerPixel);
            Vector3 frameTopLeft = new Vector3(frameSprite.bounds.min.x, frameSprite.bounds.max.y, 0);

            int ground = targetIsFriend
                ? (frameHeight > 200 ? FriendGroundTallFrame : FriendGround)
                : (frameHeight > 200 ? EnemyGroundTallFrame : EnemyGround);

            // Draw in front of the body: find which local z faces the camera.
            towardCamera = -0.05f;
            if (camera != null)
            {
                Vector3 toCamera = transform.InverseTransformDirection(camera.transform.position - transform.position);
                towardCamera = Mathf.Sign(toCamera.z) * 0.05f;
            }

            Texture2D glowTexture = CreateGlowTexture();
            Material additive = new Material(Shader.Find("Custom/MagicAdditive"));
            additive.mainTexture = glowTexture;
            Sprite glowSprite = Sprite.Create(glowTexture, new Rect(0, 0, glowTexture.width, glowTexture.height),
                new Vector2(0.5f, 0.5f), glowTexture.width, 0, SpriteMeshType.FullRect);
            createdAssets.Add(glowTexture);
            createdAssets.Add(additive);
            createdAssets.Add(glowSprite);

            Dictionary<MagicStrip, StripAssets> stripAssets = new Dictionary<MagicStrip, StripAssets>();
            int sortingOrder = target.sortingOrder;

            // Glows first, then each spawn's frames in the definition's order (炎龙术's fire
            // over its head), then all the embers.
            int emberSortingOrder = sortingOrder + 2 + definition.Spawns.Count;

            for (int spawnIndex = 0; spawnIndex < definition.Spawns.Count; spawnIndex++)
            {
                MagicSpawn spawn = definition.Spawns[spawnIndex];
                MagicStrip strip = spawn.Strip;
                StripAssets assets;
                if (!stripAssets.TryGetValue(strip, out assets))
                {
                    assets = CreateStripAssets(strip, additive, emberSortingOrder);
                    stripAssets[strip] = assets;
                }

                int x = targetIsFriend ? ScreenWidth - spawn.ScreenPosition.x - strip.CellWidth : spawn.ScreenPosition.x;
                int y = spawn.ScreenPosition.y + ground - EnemyGround;

                Flame flame = new Flame
                {
                    Spawn = spawn,
                    Assets = assets,
                    CellTopLeft = frameTopLeft + new Vector3(x * unitsPerPixel, -y * unitsPerPixel, 0),
                    Mirrored = targetIsFriend,
                };

                // The frames hang from their top-left corner; mirrored, from the top-right one.
                Vector3 framePosition = flame.CellTopLeft + new Vector3(flame.Mirrored ? strip.CellWidth * unitsPerPixel : 0, 0, towardCamera * 2);

                GameObject glowObject = new GameObject("Glow");
                glowObject.transform.SetParent(transform, false);
                flame.Glow = glowObject.AddComponent<SpriteRenderer>();
                flame.Glow.sprite = glowSprite;
                flame.Glow.sharedMaterial = additive;
                flame.Glow.sortingOrder = sortingOrder + 1;

                GameObject coreObject = new GameObject("Flame");
                coreObject.transform.SetParent(transform, false);
                coreObject.transform.localPosition = framePosition;
                flame.Core = coreObject.AddComponent<SpriteRenderer>();
                flame.Core.sortingOrder = sortingOrder + 2 + spawnIndex;
                flame.Core.flipX = flame.Mirrored;

                // Sits on the cell's top edge and stretches that one row up.
                GameObject beamObject = new GameObject("Beam");
                beamObject.transform.SetParent(transform, false);
                beamObject.transform.localPosition = framePosition;
                beamObject.transform.localScale = new Vector3(1, BeamExtensionPixels, 1);
                flame.Beam = beamObject.AddComponent<SpriteRenderer>();
                flame.Beam.sortingOrder = sortingOrder + 2 + spawnIndex;
                flame.Beam.flipX = flame.Mirrored;

                flames.Add(flame);
            }

            flameLight = CreateLight();
            CreateScreenFlash(camera);
        }

        /// <summary>
        /// Moves the effect on by dt seconds: lands the hits and eruptions of every frame
        /// passed, then shows the current one.
        /// </summary>
        public void Advance(float dt)
        {
            time += dt;
            int frame = Mathf.FloorToInt(time / definition.FrameDuration);

            // Phase 1 is the screen flash; the magic's own frame 0 comes after it.
            int totalFrames = flashFrames + definition.TotalFrames;

            for (int passed = lastFrame + 1; passed <= Mathf.Min(frame, totalFrames - 1); passed++)
            {
                bool hit = false;
                foreach (Flame flame in flames)
                {
                    hit |= LandFrame(flame, flame.Spawn.StripFrameAt(passed - flashFrames - flame.Spawn.StartFrame));
                }

                // Flames hitting on the same frame are one hit.
                if (hit)
                {
                    hitsLanded++;
                    onHit?.Invoke(hitsLanded * 100 / Mathf.Max(1, definition.HitCount));
                }
            }
            lastFrame = Mathf.Max(lastFrame, frame);

            if (screenFlash != null)
            {
                screenFlash.color = new Color(1, 1, 1, frame < flashFrames ? definition.ScreenFlash[frame] : 0);
            }

            float totalHeat = 0;
            Vector3 heatCentre = Vector3.zero;
            Color heatColour = Color.clear;
            foreach (Flame flame in flames)
            {
                int stripFrame = flame.Spawn.StripFrameAt(frame - flashFrames - flame.Spawn.StartFrame);
                float heat = ShowFrame(flame, stripFrame);
                if (heat > 0)
                {
                    totalHeat += heat;
                    heatCentre += heat * GlowCentre(flame, stripFrame);
                    heatColour += heat * flame.Spawn.Strip.GlowColor;
                }
            }

            UpdateLight(totalHeat, heatCentre, heatColour, frame, totalFrames);

            if (!completed && frame >= totalFrames)
            {
                completed = true;
                onComplete?.Invoke();
            }

            if (time >= totalFrames * definition.FrameDuration + TailSeconds && Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>Throws off the frame's embers; true when it is the flame's hit frame and the flame hits.</summary>
        private bool LandFrame(Flame flame, int stripFrame)
        {
            MagicStrip strip = flame.Spawn.Strip;
            if (strip.HitFrame < 0 || stripFrame < 0)
            {
                return false;
            }

            if (stripFrame == strip.HitFrame)
            {
                EmitEmbers(flame, stripFrame, EruptionEmbers, 1f);
                return flame.Spawn.Hits;
            }

            if (stripFrame > strip.HitFrame && stripFrame <= strip.HitFrame + BurningEmberFrames)
            {
                EmitEmbers(flame, stripFrame, BurningEmbers, 0.6f);
            }
            return false;
        }

        /// <summary>Shows one flame's frame (-1: none) and returns its heat, 0 when it is not burning.</summary>
        private float ShowFrame(Flame flame, int stripFrame)
        {
            MagicStrip strip = flame.Spawn.Strip;
            bool active = stripFrame >= 0 && stripFrame < strip.FrameCount;
            flame.Core.enabled = active;
            flame.Glow.enabled = active;
            flame.Beam.enabled = active && flame.Assets.BeamRows[stripFrame] != null;
            if (!active)
            {
                return 0;
            }

            flame.Core.sprite = flame.Assets.Frames[stripFrame];
            flame.Beam.sprite = flame.Assets.BeamRows[stripFrame];

            float heat = strip.Heat[stripFrame];
            float widthPixels = strip.CellWidth * (2.2f + 1.3f * heat) * strip.GlowSize;
            float heightPixels = (strip.PaintedHeight[stripFrame] * 1.2f + strip.CellWidth * 1.5f) * strip.GlowSize;
            flame.Glow.transform.localPosition = GlowCentre(flame, stripFrame) + new Vector3(0, 0, towardCamera);
            flame.Glow.transform.localScale = new Vector3(widthPixels * unitsPerPixel, heightPixels * unitsPerPixel, 1);

            Color glowColor = strip.GlowColor;
            glowColor.a = 0.55f * heat;
            flame.Glow.color = glowColor;
            return heat;
        }

        private Vector3 GlowCentre(Flame flame, int stripFrame)
        {
            MagicStrip strip = flame.Spawn.Strip;
            float centreY = strip.PaintedTop[stripFrame] + strip.PaintedHeight[stripFrame] * 0.5f;
            return flame.CellTopLeft + new Vector3(CellX(flame, strip.PaintedCentreX(stripFrame)) * unitsPerPixel, -centreY * unitsPerPixel, 0);
        }

        /// <summary>A pixel column of the strip's frames, placed in the (maybe mirrored) cell.</summary>
        private static float CellX(Flame flame, float x)
        {
            return flame.Mirrored ? flame.Spawn.Strip.CellWidth - x : x;
        }

        private void UpdateLight(float totalHeat, Vector3 heatCentre, Color heatColour, int frame, int totalFrames)
        {
            float tail = Mathf.Clamp01((time - totalFrames * definition.FrameDuration) / TailSeconds);
            if (totalHeat > 0)
            {
                // The light takes the colour of what is burning, weighted by heat.
                Color colour = heatColour / totalHeat;
                colour.a = 1;
                flameLight.color = colour;

                Vector3 centre = heatCentre / totalHeat;
                centre.z = towardCamera * 60;
                // Ease towards the new centre so the light sways between columns rather than jumping.
                flameLight.transform.localPosition = Vector3.Lerp(flameLight.transform.localPosition, centre, lightPlaced ? 0.35f : 1);
                lightPlaced = true;
            }

            float flicker = 0.8f + 0.4f * Mathf.PerlinNoise(time * 14f, definition.MagicId);
            float strength = Mathf.Min(1f, totalHeat / 2f);
            if (frame >= totalFrames)
            {
                strength = Mathf.Max(strength, 0.3f * (1 - tail));
            }
            flameLight.intensity = LightIntensity * strength * flicker;
        }

        private void EmitEmbers(Flame flame, int stripFrame, int count, float speedScale)
        {
            MagicStrip strip = flame.Spawn.Strip;
            float top = strip.PaintedTop[stripFrame];
            float bottom = top + strip.PaintedHeight[stripFrame];
            float left = strip.PaintedLeft != null ? strip.PaintedLeft[stripFrame] : 0;
            float width = strip.PaintedLeft != null ? strip.PaintedWidth[stripFrame] : strip.CellWidth;

            // Wide fire throws off more (up to three times a column's worth).
            count *= Mathf.Clamp(Mathf.RoundToInt(width / 44f), 1, 3);

            // Local simulation space with the effect's own scale, so everything here is in
            // original pixels times unitsPerPixel.
            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                float x = CellX(flame, left + UnityEngine.Random.Range(0.15f, 0.85f) * width);
                float y = Mathf.Lerp(bottom, top, Mathf.Pow(UnityEngine.Random.value, 1.5f) * 0.7f);
                emit.position = flame.CellTopLeft + new Vector3(x * unitsPerPixel, -y * unitsPerPixel, towardCamera * 3);
                emit.velocity = new Vector3(
                    UnityEngine.Random.Range(-12f, 12f),
                    UnityEngine.Random.Range(35f, 90f) * speedScale,
                    0) * unitsPerPixel;
                emit.startSize = UnityEngine.Random.Range(1.5f, 3.5f) * unitsPerPixel;
                emit.startLifetime = UnityEngine.Random.Range(0.45f, 0.9f);
                emit.startColor = strip.EmberColor;
                flame.Assets.Embers.Emit(emit, 1);
            }
        }

        private StripAssets CreateStripAssets(MagicStrip strip, Material additive, int emberSortingOrder)
        {
            Texture2D texture = Resources.Load<Texture2D>(strip.TexturePath);
            // Pixel art: never let the importer's defaults smear it.
            texture.filterMode = FilterMode.Point;
            float pixelsPerUnit = 1f / unitsPerPixel;

            StripAssets assets = new StripAssets
            {
                Frames = new Sprite[strip.FrameCount],
                BeamRows = new Sprite[strip.FrameCount],
                Embers = CreateEmbers(strip, additive, emberSortingOrder),
            };

            // Cells run in rows as wide as 2048 texels hold (build_magic_strip.py).
            int columns = Mathf.Max(1, 2048 / (strip.CellWidth + 2));
            for (int i = 0; i < strip.FrameCount; i++)
            {
                float cellX = 1 + (i % columns) * (strip.CellWidth + 2);
                float cellTop = texture.height - 1 - (i / columns) * (strip.CellHeight + 2);
                Rect cell = new Rect(cellX, cellTop - strip.CellHeight, strip.CellWidth, strip.CellHeight);
                assets.Frames[i] = Sprite.Create(texture, cell, new Vector2(0, 1), pixelsPerUnit, 0, SpriteMeshType.FullRect);
                createdAssets.Add(assets.Frames[i]);

                if (strip.IsBeam(i))
                {
                    Rect topRow = new Rect(cellX, cellTop - 1, strip.CellWidth, 1);
                    assets.BeamRows[i] = Sprite.Create(texture, topRow, Vector2.zero, pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    createdAssets.Add(assets.BeamRows[i]);
                }
            }
            return assets;
        }

        private ParticleSystem CreateEmbers(MagicStrip strip, Material material, int sortingOrder)
        {
            GameObject emberObject = new GameObject("Embers");
            emberObject.transform.SetParent(transform, false);
            ParticleSystem particles = emberObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 500;
            main.gravityModifier = -0.02f;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;

            ParticleSystem.ColorOverLifetimeModule colour = particles.colorOverLifetime;
            colour.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(strip.EmberHotColor, 0f), new GradientColorKey(strip.EmberMidColor, 0.35f), new GradientColorKey(strip.EmberCoolColor, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;

            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.25f));

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 20f * unitsPerPixel;
            noise.frequency = 1.5f;
            noise.scrollSpeed = 1f;

            ParticleSystemRenderer particleRenderer = emberObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingOrder = sortingOrder;

            particles.Play();
            return particles;
        }

        /// <summary>
        /// A red ring round the screen edge, fading towards the middle, stretched over the
        /// whole screen at any aspect ratio. Its alpha is set frame by frame from
        /// MagicEffectDefinition.ScreenFlash.
        ///
        /// In play it is an overlay canvas, drawn over everything whichever of the field and
        /// battle cameras renders last. Outside play (MagicEffectCapture renders one camera
        /// by hand, and overlays are not part of a camera's image) it sits just in front of
        /// the battle camera instead.
        /// </summary>
        private void CreateScreenFlash(Camera camera)
        {
            if (flashFrames == 0 || (!Application.isPlaying && camera == null))
            {
                return;
            }

            screenFlashCanvas = new GameObject("MagicScreenFlash");
            SceneManager.MoveGameObjectToScene(screenFlashCanvas, gameObject.scene);

            Canvas canvas = screenFlashCanvas.AddComponent<Canvas>();
            if (Application.isPlaying)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.05f;
            }
            canvas.sortingOrder = 30000;

            GameObject imageObject = new GameObject("EdgeRing");
            imageObject.transform.SetParent(screenFlashCanvas.transform, false);
            screenFlash = imageObject.AddComponent<RawImage>();
            screenFlash.raycastTarget = false;
            RectTransform rect = screenFlash.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Texture2D ring = CreateEdgeRingTexture(definition.ScreenFlashColor);
            createdAssets.Add(ring);
            screenFlash.texture = ring;
            screenFlash.color = new Color(1, 1, 1, 0);
        }

        private Light CreateLight()
        {
            GameObject lightObject = new GameObject("FlameLight");
            lightObject.transform.SetParent(transform, false);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = definition.Spawns[0].Strip.GlowColor;
            light.range = LightRange;
            light.intensity = 0;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            return light;
        }

        /// <summary>
        /// The colour at the screen edge fading to clear inside, stretched over the view.
        /// Each axis fades over its outer part and the two combine as a screen blend, so the
        /// corners are the deepest red.
        /// </summary>
        private static Texture2D CreateEdgeRingTexture(Color colour)
        {
            const int size = 128;
            const float innerX = 0.55f;
            const float innerY = 0.45f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float ex = Mathf.Abs((x + 0.5f) / size * 2 - 1);
                    float ey = Mathf.Abs((y + 0.5f) / size * 2 - 1);
                    float fx = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(innerX, 1, ex));
                    float fy = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(innerY, 1, ey));
                    Color pixel = colour;
                    pixel.a = colour.a * (1 - (1 - fx) * (1 - fy));
                    pixels[y * size + x] = pixel;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>A soft white disc, brightest in the middle: the glow and ember shape.</summary>
        private static Texture2D CreateGlowTexture()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2 - 1;
                    float dy = (y + 0.5f) / size * 2 - 1;
                    float falloff = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * falloff * falloff));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
