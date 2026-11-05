"""
Builds the red "off" art for the battlefield settings menu's on/off items (220 音效,
221 音乐), the same way the Action menu's disabled (_3) art differs from its normal
(_1) art: the blue face (palette idx 171) turns red (219) and the dark navy rim (242)
turns dark red (223); every other voxel stays.

    Menu_NNN_1 -> Menu_NNN_3   (off, static)
    Menu_NNN_2 -> Menu_NNN_4   (off, second frame of the selected blink)

Writes the VOX to Resources/Remastered/Menu/vox, the OBJ to Resources/Remastered/Menu/obj
and to WindingTale2/Assets/Resources/Menus. The menu OBJs are palette-UV meshes (one
texcoord per face, u = (idx - 0.5) / 256 against material-fd2-palette), so the OBJ is
recoloured by remapping its texcoords; the geometry is the source frame's. A new
Unity .meta is cloned from the source's (same import settings, fresh GUID) when the
target has none.

    python -I Tools/Vox_Generator/menu_off_variants.py
"""
import os
import re
import struct
import sys
import uuid

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
VOX_DIR = os.path.join(ROOT, 'Resources', 'Remastered', 'Menu', 'vox')
OBJ_DIR = os.path.join(ROOT, 'Resources', 'Remastered', 'Menu', 'obj')
UNITY_DIR = os.path.join(ROOT, 'WindingTale2', 'Assets', 'Resources', 'Menus')

ITEMS = [220, 221]
FRAMES = {1: 3, 2: 4}
REMAP = {171: 219, 242: 223}


def recolour_vox(data):
    """Rewrites every XYZI colour byte through REMAP; the rest of the file is kept as is."""
    out = bytearray(data)

    def walk(pos, end):
        while pos < end:
            cid = bytes(out[pos:pos + 4])
            n, m = struct.unpack('<ii', out[pos + 4:pos + 12])
            if cid == b'XYZI':
                count = struct.unpack('<i', out[pos + 12:pos + 16])[0]
                for i in range(count):
                    at = pos + 16 + i * 4 + 3
                    out[at] = REMAP.get(out[at], out[at])
            pos += 12 + n
            if m:
                walk(pos, pos + m)
                pos += m

    assert out[:4] == b'VOX '
    walk(8, len(out))
    return bytes(out)


def recolour_obj(text):
    def fix(match):
        u = float(match.group(1))
        idx = int(round(u * 256 + 0.5))
        if idx in REMAP:
            return 'vt %s %s' % (repr((REMAP[idx] - 0.5) / 256), match.group(2))
        return match.group(0)

    return re.sub(r'^vt (\S+) (\S+)', fix, text, flags=re.M)


def clone_meta(src_meta, dst_meta):
    if os.path.exists(dst_meta) or not os.path.exists(src_meta):
        return
    text = open(src_meta, encoding='utf-8').read()
    text = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + uuid.uuid4().hex, text, count=1, flags=re.M)
    open(dst_meta, 'w', encoding='utf-8', newline='').write(text)


def main():
    for item in ITEMS:
        for src_frame, dst_frame in FRAMES.items():
            src = 'Menu_%d_%d' % (item, src_frame)
            dst = 'Menu_%d_%d' % (item, dst_frame)

            vox = open(os.path.join(VOX_DIR, src + '.vox'), 'rb').read()
            open(os.path.join(VOX_DIR, dst + '.vox'), 'wb').write(recolour_vox(vox))

            obj = open(os.path.join(UNITY_DIR, src + '.obj'), encoding='utf-8', newline='').read()
            obj = recolour_obj(obj)
            for folder in (OBJ_DIR, UNITY_DIR):
                open(os.path.join(folder, dst + '.obj'), 'w', encoding='utf-8', newline='').write(obj)

            clone_meta(os.path.join(UNITY_DIR, src + '.obj.meta'), os.path.join(UNITY_DIR, dst + '.obj.meta'))
            print(src, '->', dst)


if __name__ == '__main__':
    sys.exit(main())
