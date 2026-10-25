using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WindingTale.Core.Definitions;
using WindingTale.UI.Audio;

namespace WindingTale.MapObjects.CreatureIcon
{
    /// <summary>
    /// The footstep sound of a creature walking on the map: loops the clip for what it walks
    /// on (SoundEffectTable.Walks -- wings for a flyer, else normal / snow / stone / marsh
    /// by the tile) for as long as the walk lasts, switching as the ground changes, then
    /// fades it out briefly so the loop is never cut off mid-step. Lives on its own child
    /// object, which removes itself once the fade is done.
    ///
    /// The same clip is heard once however many walk at the same time (a party marching
    /// in): of the walkers on one clip only the first plays it, the rest stay silent, and
    /// when the one playing stops, the next still walking on that clip takes the loop over.
    /// </summary>
    public class CreatureWalkSound : MonoBehaviour
    {
        private const float Volume = 0.8f;
        private const float FadeOutDuration = 0.15f;

        /// <summary>Every walk sound still walking (not fading out), oldest first.</summary>
        private static readonly List<CreatureWalkSound> walking = new List<CreatureWalkSound>();

        private AudioSource audioSource;

        private WalkSurface surface;

        private bool stopping = false;

        /// <summary>
        /// Starts the walking sound for <paramref name="surface"/> under
        /// <paramref name="creatureIcon"/>; returns null when there is no clip for it.
        /// </summary>
        public static CreatureWalkSound Play(Transform creatureIcon, WalkSurface surface)
        {
            AudioClip clip = SoundEffects.GetWalkClip(surface);
            if (clip == null)
            {
                return null;
            }

            GameObject holder = new GameObject("WalkSound");
            holder.transform.SetParent(creatureIcon, false);

            CreatureWalkSound sound = holder.AddComponent<CreatureWalkSound>();
            sound.surface = surface;
            sound.audioSource = holder.AddComponent<AudioSource>();
            sound.audioSource.clip = clip;
            sound.audioSource.loop = true;
            sound.audioSource.playOnAwake = false;
            sound.audioSource.spatialBlend = 0f;
            sound.audioSource.volume = Volume;

            walking.Add(sound);
            Arrange(clip);

            return sound;
        }

        /// <summary>The walker has stepped onto different ground: switch to its loop.</summary>
        public void SetSurface(WalkSurface newSurface)
        {
            if (newSurface == surface)
            {
                return;
            }
            surface = newSurface;

            AudioClip clip = SoundEffects.GetWalkClip(newSurface);
            if (clip == null || clip == audioSource.clip)
            {
                return;
            }

            AudioClip previous = audioSource.clip;
            audioSource.Stop();
            audioSource.clip = clip;

            // Leave the old clip's loop to whoever else walks on it, and join the new one's.
            Arrange(previous);
            Arrange(clip);
        }

        /// <summary>Fades the loop out and removes the sound object.</summary>
        public void Stop()
        {
            if (stopping)
            {
                return;
            }
            stopping = true;
            walking.Remove(this);

            // The whole map is being torn down (scene change): nothing left to fade on.
            if (!gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            // Someone else still walks on this clip: hand the loop straight over to them
            // rather than fade under it, so it is never heard twice.
            AudioClip clip = audioSource.clip;
            if (audioSource.isPlaying && walking.Exists(s => s.audioSource.clip == clip))
            {
                audioSource.Stop();
                Arrange(clip);
                Destroy(gameObject);
                return;
            }

            StartCoroutine(FadeOutAndDestroy());
        }

        void OnDestroy()
        {
            if (walking.Remove(this) && audioSource != null)
            {
                Arrange(audioSource.clip);
            }
        }

        /// <summary>
        /// Of the walkers on <paramref name="clip"/>, the one already playing it carries on
        /// (else the oldest starts it); every other one is silent.
        /// </summary>
        private static void Arrange(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            walking.RemoveAll(s => s == null || s.audioSource == null);

            CreatureWalkSound player = walking.Find(s => s.audioSource.clip == clip && s.audioSource.isPlaying)
                ?? walking.Find(s => s.audioSource.clip == clip);
            foreach (CreatureWalkSound sound in walking)
            {
                if (sound.audioSource.clip != clip)
                {
                    continue;
                }

                if (sound == player)
                {
                    if (!sound.audioSource.isPlaying)
                    {
                        sound.audioSource.volume = Volume;
                        sound.audioSource.Play();
                    }
                }
                else if (sound.audioSource.isPlaying)
                {
                    sound.audioSource.Stop();
                }
            }
        }

        private IEnumerator FadeOutAndDestroy()
        {
            float from = audioSource.volume;
            float elapsed = 0f;
            while (elapsed < FadeOutDuration)
            {
                // A new walker on the same clip has started meanwhile: stop at once, so the
                // clip is never heard twice.
                AudioClip clip = audioSource.clip;
                if (walking.Exists(s => s.audioSource.clip == clip && s.audioSource.isPlaying))
                {
                    break;
                }

                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(from, 0f, elapsed / FadeOutDuration);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
