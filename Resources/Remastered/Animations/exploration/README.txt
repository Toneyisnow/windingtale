3D Explosion Animation - 8 Frames (00 to 07)
============================================

Source: original/blowup/044-04.bmp .. 044-11.bmp (2D BMP sprites)
Frame mapping:
    044-04.bmp -> explosion_00
    044-05.bmp -> explosion_01
    044-06.bmp -> explosion_02
    044-07.bmp -> explosion_03
    044-08.bmp -> explosion_04
    044-09.bmp -> explosion_05
    044-10.bmp -> explosion_06
    044-11.bmp -> explosion_07

Files
-----
explosion_00.obj + explosion_00.mtl
explosion_01.obj + explosion_01.mtl
explosion_02.obj + explosion_02.mtl
explosion_03.obj + explosion_03.mtl
explosion_04.obj + explosion_04.mtl
explosion_05.obj + explosion_05.mtl
explosion_06.obj + explosion_06.mtl
explosion_07.obj + explosion_07.mtl
ExplosionPlayer.cs    - Unity component that plays the frames as a mesh-swap animation

Bounding box
------------
All 8 frames share an IDENTICAL bounding box of 36 x 36 x 36 units, centred at the
origin (-18..+18 on each axis). Each .obj contains 8 anchor corner vertices that
guarantee Unity computes the same mesh.bounds for every frame, so the model will
NOT shift in position when swapping frames during playback.

Voxel size: 1 Unity unit per voxel (36 voxels per side).
If your game uses metres and you want a 1.5 m explosion, set the GameObject scale
to (1/24, 1/24, 1/24) or whatever your engine prefers.

Geometry
--------
Each frame is a chunky voxel mesh built from the 2D source frame using a
"two-projection intersection" technique:

  - The 2D image is treated as both the front view and the side view of the
    explosion.
  - A voxel (x, y, z) is filled only when BOTH (x, y) and (z, y) are lit in
    the 2D image, intersected with a spherical envelope.
  - This produces a 3D shape that resembles the original 2D sprite from any
    horizontal viewing angle (front, side, back), with rays/fragments
    extending in 3D space.

Internal faces are culled, so each .obj contains only the visible exterior
shell of the voxel set.

Colors
------
Six-stop fire palette baked as Lambert-style materials in the .mtl file:

  m_white    255,245,200   (hottest core)
  m_yellow   255,215, 70
  m_orange   250,150, 35
  m_red      215, 70, 20
  m_darkred  140, 30, 10
  m_ember     60, 15,  5   (coolest fading embers)

Each color is a separate material so you can assign custom emissive shaders
in Unity if you want the explosion to glow with HDR bloom.

Unity import
------------
1. Drag the eight .obj files (with their .mtl companions) into your Project
   window. Unity will import each as a Model asset with an embedded Mesh and
   six SubMeshes (one per palette color).

2. Recommended import settings (Inspector for each .obj):
     - Scale Factor: 1 (or whatever fits your game's scale)
     - Mesh Compression: Off  (the voxel geometry is already small)
     - Read/Write Enabled: Off
     - Normals: Calculate, Smoothing Angle 1 (so cube faces stay flat)
     - Tangents: None
     - Materials > Use External Materials (Legacy) -- so Unity creates real
       Material assets you can tweak with bloom / emission

3. Create a GameObject, add MeshFilter + MeshRenderer, attach ExplosionPlayer.cs,
   drag the 8 meshes (in order 00 -> 07) into the `frames` array. Press Play.

Notes for VFX use
-----------------
- Add a Light (point light, orange) parented to the explosion GameObject and
  fade its intensity in sync with the frame index for an extra punch.
- Add a particle system on top for smoke / sparks; the voxel mesh is the core
  flash, particles add the haze.
- If you want the explosion to spin while playing, give the GameObject an
  AnimatedRotation script or rotate via ExplosionPlayer.
