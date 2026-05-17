// ExplosionPlayer.cs
// Plays a per-frame mesh swap animation of the 3D blowup explosion.
//
// Setup:
//   1. Import all explosion_00 .. explosion_07 .obj + .mtl files into Unity (drag into Project window).
//      Unity imports them as Models. Their bounding boxes are identical (36 x 36 x 36 centred at origin),
//      so the pivot does NOT shift between frames.
//   2. Create an empty GameObject in the scene. Add a MeshFilter and MeshRenderer component.
//   3. Add this script to the GameObject.
//   4. Drag the 8 imported meshes (in order 04 -> 11) into the `frames` array in the Inspector.
//      You can find each mesh inside its imported Model asset (expand the model to see the Mesh child).
//   5. Optional: assign your own materials to the MeshRenderer.materials slot, or rely on the imported
//      ones generated from the .mtl files.
//   6. Set framesPerSecond to taste (10-15 fps usually feels right for a quick explosion).
//   7. Press Play. The script cycles through the meshes once and disables the renderer at the end
//      (or destroys the object if `destroyOnEnd` is true).

using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ExplosionPlayer : MonoBehaviour
{
    [Tooltip("Meshes for explosion frames in order (explosion_00 .. explosion_07).")]
    public Mesh[] frames;

    [Tooltip("Playback speed in frames per second.")]
    public float framesPerSecond = 12f;

    [Tooltip("Restart from frame 0 every time the object is enabled.")]
    public bool playOnEnable = true;

    [Tooltip("If true, destroy the GameObject when the animation finishes.")]
    public bool destroyOnEnd = true;

    [Tooltip("If true, the renderer is left enabled after the last frame instead of being hidden.")]
    public bool keepLastFrameVisible = false;

    private MeshFilter   _filter;
    private MeshRenderer _renderer;
    private float        _time;
    private bool         _playing;

    private void Awake()
    {
        _filter   = GetComponent<MeshFilter>();
        _renderer = GetComponent<MeshRenderer>();
    }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    public void Play()
    {
        _time    = 0f;
        _playing = true;
        if (frames != null && frames.Length > 0)
        {
            _filter.sharedMesh = frames[0];
            _renderer.enabled  = true;
        }
    }

    private void Update()
    {
        if (!_playing || frames == null || frames.Length == 0) return;

        _time += Time.deltaTime;
        int idx = Mathf.FloorToInt(_time * framesPerSecond);

        if (idx >= frames.Length)
        {
            _playing = false;
            if (!keepLastFrameVisible) _renderer.enabled = false;
            if (destroyOnEnd) Destroy(gameObject);
            return;
        }

        if (_filter.sharedMesh != frames[idx])
            _filter.sharedMesh = frames[idx];
    }
}
