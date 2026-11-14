"""
Low-poly Gothic-2-style wooden watchtower, built procedurally in Blender.

Run headless:
  blender.exe -b --factory-startup -P Tools/Blender/build_watchtower.py

Output (Assets/Models/Watchtower/):
  Watchtower.fbx        - 4 mesh objects, Unity-ready (Y-up, 1 unit = 1 m, transforms applied)
  Watchtower_Atlas.png  - 256x256 shared texture atlas
  Watchtower.blend      - editable source

Objects: Watchtower_Beams, Watchtower_Platform, Watchtower_Roof, Watchtower_Ladder
All share one material that samples the one 256px atlas (4 quadrants of 128px).
Pivot: world origin at ground level, centred under the tower.
"""

import bpy
import bmesh
import os
import random
from mathutils import Vector

# ---------------------------------------------------------------- paths
PROJECT = r"C:\Users\jurij\Desktop\dev\anthem"
OUT_DIR = os.path.join(PROJECT, "Assets", "Models", "Watchtower")
ATLAS_PATH = os.path.join(OUT_DIR, "Watchtower_Atlas.png")
FBX_PATH = os.path.join(OUT_DIR, "Watchtower.fbx")
BLEND_PATH = os.path.join(OUT_DIR, "Watchtower.blend")
os.makedirs(OUT_DIR, exist_ok=True)

# Fixed seed: irregularity is random-looking but reproducible.
rng = random.Random(1408)


def jit(a):
    return rng.uniform(-a, a)


# ---------------------------------------------------------------- atlas
ATLAS = 256
# Regions in UV space (u0, v0, u1, v1). Quadrants of the 256px atlas.
REGION = {
    "timber":    (0.0, 0.0, 0.5, 0.5),   # bottom-left  : dark weathered posts/girts/braces
    "plank":     (0.5, 0.0, 1.0, 0.5),   # bottom-right : light deck planks, rails
    "shingle":   (0.0, 0.5, 0.5, 1.0),   # top-left     : roof shingles
    "weathered": (0.5, 0.5, 1.0, 1.0),   # top-right    : ladder (grey, sun-bleached)
}


def hash01(x, y, s=0):
    h = (x * 374761393 + y * 668265263 + s * 2147483647) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((h ^ (h >> 16)) & 0xFFFF) / 65535.0


def paint(kind, lx, ly):
    """Return RGB for a pixel at local coords (0..127) inside a region."""
    if kind == "timber":
        base = (0.24, 0.15, 0.09)
        shade = 0.82 + 0.25 * hash01(lx, ly // 4)          # vertical grain streaks
        if ly % 32 < 2:                                     # board seam
            shade *= 0.45
    elif kind == "plank":
        base = (0.56, 0.41, 0.24)
        shade = 0.86 + 0.22 * hash01(lx // 16, ly // 6)   # plank tone variation
        if lx % 16 < 1:                                     # gap between boards
            shade *= 0.4
    elif kind == "shingle":
        base = (0.36, 0.22, 0.12)
        row = ly // 10
        off = 8 if row % 2 else 0
        col = (lx + off) // 16
        shade = 0.78 + 0.32 * hash01(col, row, 3)
        if ly % 10 < 1:                                     # shingle lower edge
            shade *= 0.5
    else:  # weathered
        base = (0.44, 0.42, 0.38)
        shade = 0.84 + 0.28 * hash01(lx, ly // 3, 7)
        if lx % 32 < 1:
            shade *= 0.6
    return tuple(min(1.0, c * shade) for c in base)


def build_atlas():
    pixels = [0.0] * (ATLAS * ATLAS * 4)
    half = ATLAS // 2
    for py in range(ATLAS):
        for px in range(ATLAS):
            kind = ("timber" if py < half else "shingle") if px < half else \
                   ("plank" if py < half else "weathered")
            r, g, b = paint(kind, px % half, py % half)
            i = (py * ATLAS + px) * 4
            pixels[i:i + 4] = [r, g, b, 1.0]
    img = bpy.data.images.new("Watchtower_Atlas", ATLAS, ATLAS, alpha=False)
    img.pixels.foreach_set(pixels)
    img.filepath_raw = ATLAS_PATH
    img.file_format = 'PNG'
    img.save()
    return img


# ---------------------------------------------------------------- material
def build_material(img):
    mat = bpy.data.materials.new("Watchtower_Atlas_MAT")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = 'Closest'          # crisp texels, pixel-art look
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.95
    return mat


# ---------------------------------------------------------------- geometry
QUAD_UV = [(0, 0), (1, 0), (1, 1), (0, 1)]
TRI_UV = [(0, 0), (1, 0), (0.5, 1)]


class Builder:
    def __init__(self):
        self.bm = bmesh.new()
        self.uvl = self.bm.loops.layers.uv.new("UVMap")

    def face(self, verts, region):
        f = self.bm.faces.new(verts)
        u0, v0, u1, v1 = REGION[region]
        pts = QUAD_UV if len(verts) == 4 else TRI_UV
        for loop, (x, y) in zip(f.loops, pts):
            loop[self.uvl].uv = (u0 + x * (u1 - u0), v0 + y * (v1 - v0))
        return f

    def beam(self, p0, p1, w, h, region):
        """Square-ish prism between two points. w = width, h = height (perp. to axis)."""
        p0, p1 = Vector(p0), Vector(p1)
        d = (p1 - p0).normalized()
        ref = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
        s = d.cross(ref).normalized() * (w / 2)
        t = s.cross(d).normalized() * (h / 2)
        V = {}
        for end, base in ((0, p0), (1, p1)):
            for sx in (-1, 1):
                for sy in (-1, 1):
                    V[(end, sx, sy)] = self.bm.verts.new(base + s * sx + t * sy)
        faces = [
            [(0, 1, -1), (0, 1, 1), (1, 1, 1), (1, 1, -1)],
            [(0, -1, -1), (1, -1, -1), (1, -1, 1), (0, -1, 1)],
            [(0, -1, 1), (1, -1, 1), (1, 1, 1), (0, 1, 1)],
            [(0, -1, -1), (0, 1, -1), (1, 1, -1), (1, -1, -1)],
            [(0, -1, -1), (0, -1, 1), (0, 1, 1), (0, 1, -1)],
            [(1, -1, -1), (1, 1, -1), (1, 1, 1), (1, -1, 1)],
        ]
        for f in faces:
            self.face([V[k] for k in f], region)

    def hip_roof(self, eave, ridge, region):
        """eave: 4 points (-x-y, +x-y, +x+y, -x+y); ridge: 2 points (-x end, +x end)."""
        E0, E1, E2, E3 = [Vector(p) for p in eave]
        R0, R1 = [Vector(p) for p in ridge]
        vE = [self.bm.verts.new(p) for p in (E0, E1, E2, E3)]
        vR = [self.bm.verts.new(p) for p in (R0, R1)]
        self.face([vE[0], vE[1], vR[1], vR[0]], region)   # front slope
        self.face([vE[2], vE[3], vR[0], vR[1]], region)   # back slope
        self.face([vE[1], vE[2], vR[1]], region)          # east hip
        self.face([vE[3], vE[0], vR[0]], region)          # west hip
        self.face([vE[0], vE[3], vE[2], vE[1]], region)   # underside (closed mesh)

    def finish(self, name, mat):
        bm = self.bm
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        for f in bm.faces:
            f.smooth = False                                 # flat shading
        mesh = bpy.data.meshes.new(name)
        bm.to_mesh(mesh)
        bm.free()
        mesh.materials.append(mat)
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.collection.objects.link(obj)
        return obj


# ---------------------------------------------------------------- design
TOWER_HALF_X = 1.7     # base footprint of the timber frame (irregular on purpose)
TOWER_HALF_Y = 1.5
DECK_HALF_X = 2.2
DECK_HALF_Y = 2.0
DECK_Z = 6.2           # deck underside
DECK_TOP = 6.4         # deck surface
RAIL_Z = 7.15          # top of platform rail
EAVE_Z = 8.6
RIDGE_Z = 10.3
CORNERS = [(-1, -1), (1, -1), (1, 1), (-1, 1)]


def build_beams(mat, img):
    b = Builder()
    reg = "timber"
    # Corner posts: slightly different heights and leans
    posts = {}
    for sx, sy in CORNERS:
        bx, by = sx * TOWER_HALF_X, sy * TOWER_HALF_Y
        top = Vector((bx - 0.06 * sx + jit(0.1), by - 0.05 * sy + jit(0.08), EAVE_Z - 0.08 + jit(0.08)))
        bottom = Vector((bx + jit(0.05), by + jit(0.05), -0.15))
        posts[(sx, sy)] = (bottom, top)
        b.beam(bottom, top, 0.26 + jit(0.03), 0.26 + jit(0.03), reg)

    # Girts at three levels around all four sides
    for z in (0.5, 3.5, 6.0):
        zz = z + jit(0.1)
        b.beam((-TOWER_HALF_X, -TOWER_HALF_Y, zz), (TOWER_HALF_X, -TOWER_HALF_Y, zz + jit(0.05)), 0.2, 0.2, reg)
        b.beam((-TOWER_HALF_X, TOWER_HALF_Y, zz), (TOWER_HALF_X, TOWER_HALF_Y, zz + jit(0.05)), 0.2, 0.2, reg)
        b.beam((-TOWER_HALF_X, -TOWER_HALF_Y, zz), (-TOWER_HALF_X, TOWER_HALF_Y, zz + jit(0.05)), 0.2, 0.2, reg)
        b.beam((TOWER_HALF_X, -TOWER_HALF_Y, zz), (TOWER_HALF_X, TOWER_HALF_Y, zz + jit(0.05)), 0.2, 0.2, reg)

    # Diagonal bracing, one per side, alternating direction
    b.beam((-TOWER_HALF_X, -TOWER_HALF_Y, 0.5), (TOWER_HALF_X, -TOWER_HALF_Y, 3.5), 0.16, 0.16, reg)
    b.beam((TOWER_HALF_X, TOWER_HALF_Y, 0.5), (-TOWER_HALF_X, TOWER_HALF_Y, 3.5), 0.16, 0.16, reg)
    b.beam((TOWER_HALF_X, -TOWER_HALF_Y, 0.5), (TOWER_HALF_X, TOWER_HALF_Y, 3.5), 0.16, 0.16, reg)
    b.beam((-TOWER_HALF_X, TOWER_HALF_Y, 0.5), (-TOWER_HALF_X, -TOWER_HALF_Y, 3.5), 0.16, 0.16, reg)

    # Corbel braces holding the overhanging deck (outward in X and in Y)
    for sx, sy in CORNERS:
        px, py = sx * TOWER_HALF_X, sy * TOWER_HALF_Y
        start = (px, py, 4.7 + jit(0.15))
        b.beam(start, (sx * (DECK_HALF_X + jit(0.05)), py, DECK_Z), 0.16, 0.16, reg)
        b.beam(start, (px, sy * (DECK_HALF_Y + jit(0.05)), DECK_Z), 0.16, 0.16, reg)

    return b.finish("Watchtower_Beams", mat)


def build_platform(mat, img):
    b = Builder()
    reg = "plank"
    # Deck planks running along X, irregular ends
    count = 9
    width = 0.41
    pitch = 0.44
    y0 = -DECK_HALF_Y + 0.205
    for i in range(count):
        y = y0 + i * pitch
        x0 = -DECK_HALF_X + jit(0.06)
        x1 = DECK_HALF_X + jit(0.06)
        z = (DECK_Z + DECK_TOP) / 2
        b.beam((x0, y, z), (x1, y + jit(0.02), z), width, DECK_TOP - DECK_Z, reg)

    # Rail corner posts
    rail_pts = {}
    for sx, sy in CORNERS:
        px = sx * (DECK_HALF_X - 0.05) + jit(0.04)
        py = sy * (DECK_HALF_Y - 0.05) + jit(0.04)
        rail_pts[(sx, sy)] = (px, py)
        b.beam((px, py, DECK_TOP), (px, py, RAIL_Z + jit(0.05)), 0.14, 0.14, reg)

    def rail(a, c):
        b.beam((a[0], a[1], RAIL_Z), (c[0], c[1], RAIL_Z + jit(0.03)), 0.12, 0.12, reg)

    # Top rails: full on X sides and back (+Y); split on front (-Y) to leave the ladder opening
    rail(rail_pts[(-1, 1)], rail_pts[(1, 1)])
    rail(rail_pts[(-1, -1)], rail_pts[(-1, 1)])
    rail(rail_pts[(1, -1)], rail_pts[(1, 1)])
    rail((rail_pts[(-1, -1)][0], rail_pts[(-1, -1)][1]), (-0.6, rail_pts[(-1, -1)][1]))
    rail((0.6, rail_pts[(1, -1)][1]), (rail_pts[(1, -1)][0], rail_pts[(1, -1)][1]))

    return b.finish("Watchtower_Platform", mat)


def build_roof(mat, img):
    b = Builder()
    reg = "shingle"
    ex, ey = DECK_HALF_X + 0.35, DECK_HALF_Y + 0.3   # eave overhang beyond the deck
    eave = [
        (-ex + jit(0.12), -ey + jit(0.12), EAVE_Z + jit(0.1)),
        (ex + jit(0.12), -ey + jit(0.12), EAVE_Z + jit(0.1)),
        (ex + jit(0.12), ey + jit(0.12), EAVE_Z + jit(0.1)),
        (-ex + jit(0.12), ey + jit(0.12), EAVE_Z + jit(0.1)),
    ]
    ridge = [(-0.8 + jit(0.1), 0.15 + jit(0.05), RIDGE_Z + jit(0.2)),
             (0.9 + jit(0.1), -0.1 + jit(0.05), RIDGE_Z - 0.3 + jit(0.15))]
    b.hip_roof(eave, ridge, reg)
    return b.finish("Watchtower_Roof", mat)


def build_ladder(mat, img):
    b = Builder()
    reg = "weathered"
    foot_y, foot_z = -4.6, 0.0
    top_y, top_z = -2.25, 7.3
    rail_x = 0.4
    rails_l = []
    for sgn in (-1, 1):
        foot = Vector((sgn * rail_x + jit(0.03), foot_y + jit(0.1), foot_z))
        top = Vector((sgn * rail_x + jit(0.03), top_y + jit(0.03), top_z))
        b.beam(foot, top, 0.1, 0.08, reg)
        rails_l.append((foot, top))

    # Rungs: evenly spaced along the rails
    (fl, tl), (fr, tr) = rails_l
    length = (tl - fl).length
    n = max(2, int(length / 0.36))
    for i in range(1, n):
        t = i / n + jit(0.01)
        left = fl.lerp(tl, t)
        right = fr.lerp(tr, t)
        b.beam(left, right, 0.07, 0.07, reg)

    return b.finish("Watchtower_Ladder", mat)


# ---------------------------------------------------------------- export
def export(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        object_types={'MESH'},
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=False,
        axis_forward='-Z',
        axis_up='Y',
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        bake_anim=False,
        path_mode='COPY',
        embed_textures=False,
    )


def report(objs):
    for o in objs:
        me = o.data
        tris = sum(len(p.vertices) - 2 for p in me.polygons)
        dims = o.dimensions
        print(f"[watchtower] {o.name}: {len(me.polygons)} faces, {tris} tris, "
              f"dims {dims.x:.2f} x {dims.y:.2f} x {dims.z:.2f} m, "
              f"smooth={any(p.use_smooth for p in me.polygons)}")


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    img = build_atlas()
    mat = build_material(img)
    objs = [
        build_beams(mat, img),
        build_platform(mat, img),
        build_roof(mat, img),
        build_ladder(mat, img),
    ]
    report(objs)
    export(objs)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print(f"[watchtower] FBX:   {FBX_PATH}")
    print(f"[watchtower] Atlas: {ATLAS_PATH}")
    print(f"[watchtower] Blend: {BLEND_PATH}")


main()
