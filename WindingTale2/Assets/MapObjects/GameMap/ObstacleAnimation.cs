using System.Collections.Generic;
using UnityEngine;

namespace WindingTale.MapObjects.GameMap
{
    /// <summary>
    /// Plays an obstacle's animation frames -- the flicker of chapter 10's fire
    /// pillars. Each frame is a separate model: the obstacle's own model is frame 1
    /// and the models Resources/Obstacles/{DefinitionKey}_f2, _f3, ... are the
    /// frames after it. ObstaclesLayer instantiates every frame under the one
    /// obstacle root, all with the same transform, and hands their renderers here;
    /// this component then shows exactly one frame at a time, switching at
    /// FramesPerSecond.
    ///
    /// The frames are toggled through Renderer.enabled rather than by activating
    /// and deactivating GameObjects, so the hidden frames still count for
    /// everything that walks the hierarchy for renderers -- the fade
    /// (MapObjectFade), the clip shader, the bounds the footprint is read from.
    /// </summary>
    public class ObstacleAnimation : MonoBehaviour
    {
        /// <summary>
        /// The rate every animated obstacle plays at, in frames per second. One
        /// global constant: the frames are authored as a flicker, not as timed
        /// motion, so no model needs its own rate.
        /// </summary>
        public const float FramesPerSecond = 4f;

        private readonly List<Renderer[]> frames = new List<Renderer[]>();

        // Seconds added to the clock before it is divided into frames, so that
        // neighbouring obstacles do not all flip in unison. Set by ObstaclesLayer.
        private float phase = 0f;

        private int shown = -1;

        public int FrameCount
        {
            get { return frames.Count; }
        }

        /// <summary>
        /// Registers the renderers under one frame's root as the next frame. Call
        /// for frame 1 before the later frames are parented under it, or frame 1
        /// would collect theirs as well.
        /// </summary>
        public void AddFrame(GameObject frameRoot)
        {
            frames.Add(frameRoot.GetComponentsInChildren<Renderer>(true));
        }

        /// <summary>
        /// Offsets this obstacle's clock so a row of the same model does not blink
        /// together. Any value works; ObstaclesLayer derives it from the obstacle id.
        /// </summary>
        public void SetPhase(float seconds)
        {
            phase = seconds;
        }

        /// <summary>
        /// Enables the renderers of one frame and disables every other frame's.
        /// </summary>
        public void Show(int frame)
        {
            if (frames.Count == 0 || frame == shown)
            {
                return;
            }

            for (int i = 0; i < frames.Count; i++)
            {
                bool visible = i == frame;
                foreach (Renderer renderer in frames[i])
                {
                    if (renderer != null)
                    {
                        renderer.enabled = visible;
                    }
                }
            }

            shown = frame;
        }

        void Update()
        {
            if (frames.Count < 2)
            {
                return;
            }

            int frame = Mathf.FloorToInt((Time.time + phase) * FramesPerSecond) % frames.Count;
            Show(frame);
        }
    }
}
