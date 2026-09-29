3D Death Explosion - 8 Frames (00 to 07)
=======================================

Played by MapObjects/CreatureIcon/CreatureDying.cs (via ExplosionPlayer.cs) when a
creature dies on the map.

Source: original game sprite OriginData/Other/040-04.bmp .. 040-11.bmp
    040-04 -> explosion_00   white-hot fireball
    040-05 -> explosion_01   fireball bursting open
    040-06 -> explosion_02   ring of fire with spikes
    040-07 -> explosion_03
    040-08 -> explosion_04   red embers
    040-09 .. 040-11 -> explosion_05 .. 07   last dying sparks

Timing (CreatureDying): 0.15 s, 0.15 s, 0.12 s, 0.1 s, then 0.08 s each -- the fireball and its
bursting open are held, the tail is short. Frame 00 is the closed fireball enlarged to
~80% of the widest frame; frame 01's hollow middle is filled with that fireball except a
small opening (death_explosion_to_obj.py FRAME_SCALE / OPEN_DEPTH), so it blows open
from the centre.

Generator: Tools/Vox_Generator/death_explosion_to_obj.py  (re-run to regenerate)

Geometry
--------
The sprite is a top-down view, so each frame is the explosion's footprint on the
ground plane. Height is inferred by Poisson inflation (laplace(h) = -2 inside the lit
pixels, half = sqrt(h) - 0.5): the fireball becomes a dome/ball, the ring a crown of
flames, thin wisps stay thin. Flames reach 1.4x above the mid layer and 0.8x below
(UP_SCALE / DOWN_SCALE); late frames lift a few voxels like rising embers (LIFT).
Seen from straight above, every frame is exactly the original sprite.

1 OBJ unit = 1 original pixel, all frames share the origin (centre of a 72 x 72
canvas). CreatureDying scales by 2 / 64 (64 sprite px per 2-unit map tile), so the
widest frame spans about 1.13 tiles -- under 130% of a tile's area (user 2026-09-28).

Colours
-------
One material per exact sprite colour, named m_RRGGBB. CreatureDying reads each
imported material's colour and draws it with the self-lit Custom/DeathExplosion
shader (Resources/Materials/DeathExplosionShader.shader), plus an orange point
light that fades with the frames.
