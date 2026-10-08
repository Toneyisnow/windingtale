using System.Collections;
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
    /// </summary>
    public class CreatureWalkSound : MonoBehaviour
    {
        private const float Volume = 0.8f;
        private const float FadeOutDuration = 0.15f;

        private AudioSource audioSource;

        private WalkSurface surface;

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
            sound.audioSource.Play();

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

            audioSource.clip = clip;
            audioSource.Play();
        }

        /// <summary>Fades the loop out and removes the sound object.</summary>
        public void Stop()
        {
            // The whole map is being torn down (scene change): nothing left to fade on.
            if (!gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            StartCoroutine(FadeOutAndDestroy());
        }

        private IEnumerator FadeOutAndDestroy()
        {
            float from = audioSource.volume;
            float elapsed = 0f;
            while (elapsed < FadeOutDuration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(from, 0f, elapsed / FadeOutDuration);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
