using System.Collections;
using UnityEngine;
using WindingTale.Core.Definitions;

namespace WindingTale.MapObjects.CreatureIcon
{
    /// <summary>
    /// The footstep sound of a creature walking on the map: loops the clip that fits how the
    /// creature moves -- wings for a flyer, hooves for a knight, feet for everyone else --
    /// for as long as the walk lasts, then fades it out briefly so the loop is never cut off
    /// mid-step. Lives on its own child object, which removes itself once the fade is done.
    /// </summary>
    public class CreatureWalkSound : MonoBehaviour
    {
        private const string NormalClip = "Audios/Effects/sfx_move_normal";
        private const string KnightClip = "Audios/Effects/sfx_move_knight";
        private const string FlyClip = "Audios/Effects/sfx_move_fly";

        private const float Volume = 0.8f;
        private const float FadeOutDuration = 0.15f;

        private AudioSource audioSource;

        /// <summary>
        /// Starts the walking sound for <paramref name="definition"/> under
        /// <paramref name="creatureIcon"/>; returns null when the clip cannot be loaded.
        /// </summary>
        public static CreatureWalkSound Play(Transform creatureIcon, CreatureDefinition definition)
        {
            AudioClip clip = Resources.Load<AudioClip>(ClipNameFor(definition));
            if (clip == null)
            {
                Debug.LogWarning("Cannot load walk sound: " + ClipNameFor(definition));
                return null;
            }

            GameObject holder = new GameObject("WalkSound");
            holder.transform.SetParent(creatureIcon, false);

            CreatureWalkSound sound = holder.AddComponent<CreatureWalkSound>();
            sound.audioSource = holder.AddComponent<AudioSource>();
            sound.audioSource.clip = clip;
            sound.audioSource.loop = true;
            sound.audioSource.playOnAwake = false;
            sound.audioSource.spatialBlend = 0f;
            sound.audioSource.volume = Volume;
            sound.audioSource.Play();

            return sound;
        }

        /// <summary>A flyer that is also a knight (the pegasus knight) takes to the air, so flying is checked first.</summary>
        private static string ClipNameFor(CreatureDefinition definition)
        {
            if (definition.CanFly())
            {
                return FlyClip;
            }

            if (definition.IsKnight())
            {
                return KnightClip;
            }

            return NormalClip;
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
