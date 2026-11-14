"""
Low-poly Gothic-style stone/rock set (untextured), built procedurally in Blender.

Run headless:
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" -b --factory-startup -P Tools/Blender/build_stones.py

Output (Assets/_Project/Art/Stones/):
  Stone_XX_Name.fbx   - one file per stone, Unity-ready (1 unit = 1 m, pivot at base centre, transforms applied)
  Stones.blend        - all stones laid out in a row for preview / editing

Flat-shaded faceted look, cube-projected UVs (so any tiling texture works), no material.
Sizes are realistic-ish metres; scale freely in Unity.
"""
import bpy, bmesh, os, math, random
from mathutils import Vector

PROJECT = r"C:\Users\jurij\Desktop\dev\anthem"
OUT = os.path.join(PROJECT, "Assets", "_Project", "Art", "Stones")
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)


def hull(points):
    bm = bmesh.new()
    for p in points:
        bm.verts.new(p)
    bmesh.ops.convex_hull(bm, input=bm.verts[:])
    # drop loose verts left inside the hull
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    return bm


def rand_pts(rng, n, size, round_=0.0):
    """Random points on a squashed ellipsoid -> angular chunk."""
    sx, sy, sz = size
    pts = []
    for _ in range(n):
        v = Vector((rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1)))
        v.normalize()
        r = 1.0 - round_ * rng.random() * 0.3
        pts.append(Vector((v.x * sx * r, v.y * sy * r, v.z * sz * r)))
    return pts


def merge(bms):
    out = bmesh.new()
    me = bpy.data.meshes.new("tmp")
    for b in bms:
        b.to_mesh(me)
        out.from_mesh(me)
        b.free()
    bpy.data.meshes.remove(me)
    return out


def chunk(seed, size, n=14):
    rng = random.Random(seed)
    return hull(rand_pts(rng, int(n * 2.2), size, 0.5))


def boulder(seed, size, subdiv=2, noise=0.22):
    """Rounded rock: noisy icosphere."""
    rng = random.Random(seed)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=1.0)
    for v in bm.verts:
        n = 1.0 + rng.uniform(-noise, noise)
        v.co = Vector((v.co.x * size[0] * n, v.co.y * size[1] * n, v.co.z * size[2] * n))
    return bm


def slab(seed, size, jitter=0.12):
    """Flat-ish angular slab (box with jittered corners, 2x2 top subdiv)."""
    rng = random.Random(seed)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=2, use_grid_fill=True)
    for v in bm.verts:
        v.co.x *= size[0]; v.co.y *= size[1]; v.co.z *= size[2]
        v.co += Vector((rng.uniform(-1, 1) * size[0] * jitter,
                        rng.uniform(-1, 1) * size[1] * jitter,
                        rng.uniform(-1, 1) * size[2] * jitter * 0.6))
    return bm


def menhir(seed, h, w, taper=0.55, sides=5):
    """Tall standing stone: tapered irregular prism, tilted top."""
    rng = random.Random(seed)
    bm = bmesh.new()
    rings = []
    for lvl, (z, s) in enumerate([(0, 1.0), (h * 0.5, 0.88), (h, taper)]):
        ring = []
        for i in range(sides):
            a = 2 * math.pi * i / sides + rng.uniform(-0.25, 0.25)
            r = w * s * rng.uniform(0.8, 1.15)
            ring.append(bm.verts.new((math.cos(a) * r + rng.uniform(-.05, .05) * w * lvl,
                                      math.sin(a) * r * 0.6, z + rng.uniform(-.04, .04) * h * (lvl > 0))))
        rings.append(ring)
    for r0, r1 in zip(rings, rings[1:]):
        for i in range(sides):
            j = (i + 1) % sides
            bm.faces.new((r0[i], r0[j], r1[j], r1[i]))
    bm.faces.new(rings[0][::-1])
    bm.faces.new(rings[-1])
    return bm


def stack(seed, n, base, shrink):
    """Strata-like pile: slightly offset stacked chunks."""
    rng = random.Random(seed)
    parts, z = [], 0.0
    for i in range(n):
        s = [base[0] * shrink ** i, base[1] * shrink ** i, base[2]]
        b = hull(rand_pts(rng, 22, s, 0.5))
        for v in b.verts:
            v.co.z = max(v.co.z, -s[2] * 0.7)  # flatten bottoms
        zmin = min(v.co.z for v in b.verts)
        for v in b.verts:
            v.co += Vector((rng.uniform(-.1, .1) * base[0], rng.uniform(-.1, .1) * base[1], z - zmin))
        z += (max(v.co.z for v in b.verts) - min(v.co.z for v in b.verts)) * 0.8 + 0.0
        parts.append(b)
        z = max(v.co.z for v in b.verts) * 0.92
    return merge(parts)


def cluster(seed, count, spread, size):
    rng = random.Random(seed)
    parts = []
    for i in range(count):
        s = rng.uniform(0.35, 1.0)
        b = hull(rand_pts(rng, 18, [size[0] * s, size[1] * s, size[2] * s], 0.3))
        zmin = min(v.co.z for v in b.verts)
        off = Vector((rng.uniform(-1, 1) * spread, rng.uniform(-1, 1) * spread, -zmin - size[2] * s * 0.25))
        for v in b.verts:
            v.co += off
        parts.append(b)
    return merge(parts)


def finish(bm, name, bury=0.12, flat_bottom=True):
    # pivot: base centre; flatten underside so it sits on terrain, slightly buried
    zs = [v.co.z for v in bm.verts]
    zmin, zmax = min(zs), max(zs)
    h = zmax - zmin
    cut = zmin + h * 0.18
    if flat_bottom:
        for v in bm.verts:
            if v.co.z < cut:
                v.co.z = cut
    zs = [v.co.z for v in bm.verts]
    cx = sum(v.co.x for v in bm.verts) / len(bm.verts)
    cy = sum(v.co.y for v in bm.verts) / len(bm.verts)
    zmin = min(zs)
    for v in bm.verts:
        v.co += Vector((-cx, -cy, -zmin - h * bury))
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=0.0005)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = False
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    # cube-projected UVs, ~1 UV tile per metre
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.cube_project(cube_size=1.0, correct_aspect=True, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    return ob


# (name, builder) — sizes are half-extents in metres
SPECS = [
    ("Pebble",        lambda: chunk(1, (0.18, 0.14, 0.09), 9)),
    ("SmallRock_A",   lambda: chunk(2, (0.35, 0.28, 0.22), 11)),
    ("SmallRock_B",   lambda: boulder(3, (0.40, 0.32, 0.26), 2, 0.3)),
    ("MediumRock_A",  lambda: chunk(4, (0.70, 0.55, 0.45), 14)),
    ("MediumRock_B",  lambda: boulder(5, (0.75, 0.60, 0.50), 2, 0.3)),
    ("MediumRock_C",  lambda: chunk(6, (0.80, 0.50, 0.55), 16)),
    ("Boulder_A",     lambda: boulder(7, (1.3, 1.1, 0.95), 3, 0.28)),
    ("Boulder_B",     lambda: chunk(8, (1.4, 1.0, 1.1), 18)),
    ("Boulder_Large", lambda: boulder(9, (2.2, 1.8, 1.6), 3, 0.25)),
    ("Slab_Flat",     lambda: slab(10, (1.0, 0.8, 0.14), 0.12)),
    ("Slab_Thick",    lambda: slab(11, (0.9, 0.6, 0.3), 0.14)),
    ("Slab_Leaning",  lambda: chunk(12, (0.25, 1.0, 0.9), 12)),
    ("Menhir_A",      lambda: menhir(13, 3.0, 0.55)),
    ("Menhir_B",      lambda: menhir(14, 2.0, 0.45, 0.7, 6)),
    ("Strata_Stack",  lambda: stack(15, 4, (0.9, 0.8, 0.28), 0.82)),
    ("Cluster_Small", lambda: cluster(16, 5, 0.7, (0.35, 0.3, 0.25))),
    ("Cluster_Large", lambda: cluster(17, 6, 1.5, (0.9, 0.75, 0.6))),
    ("Rubble_Chunk",  lambda: chunk(18, (0.55, 0.4, 0.3), 9)),
]

objs = []
for i, (name, fn) in enumerate(SPECS, 1):
    objs.append(finish(fn(), f"Stone_{i:02d}_{name}", flat_bottom=not name.startswith(("Menhir", "Pebble"))))

# export each as FBX (Unity: Y-up, -Z forward, metres)
tris = {}
for ob in objs:
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    tris[ob.name] = len(ob.data.polygons)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(OUT, ob.name + ".fbx"), use_selection=True,
        apply_scale_options='FBX_SCALE_ALL', apply_unit_scale=True,
        bake_space_transform=True, axis_forward='-Z', axis_up='Y',
        mesh_smooth_type='FACE', add_leaf_bones=False, object_types={'MESH'})

# preview layout in a row, then save .blend
x = 0.0
for ob in objs:
    w = max(v.co.x for v in ob.data.vertices) - min(v.co.x for v in ob.data.vertices)
    ob.location.x = x + w / 2
    x += w + 0.8
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Stones.blend"))
for n, t in tris.items():
    print(f"{n}: {t} tris")
