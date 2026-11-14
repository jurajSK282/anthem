# Generates lake / flower textures: T_MJ_WaterN (ripple normal map), T_MJ_LeafFloat (floating leaf patch),
# T_MJ_Flower (glowing flower card). Usage: python Tools/Textures/lake_textures.py <leaf-sprite-folder>
import os, sys
import numpy as np
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Art", "Emberwood", "MJ")
SRC = sys.argv[1]
rng = np.random.default_rng(21)


def noise(cells, s):
    g = np.tile(rng.random((cells, cells)).astype(np.float32), (3, 3))
    return np.asarray(Image.fromarray(g).resize((s * 3, s * 3), Image.BICUBIC))[s:2 * s, s:2 * s]


def save(name, arr, mode):
    Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8), mode).save(os.path.join(OUT, name + ".png"))


# --- ripple normal map (seamless) ---
S = 128
h = noise(6, S) + 0.6 * noise(12, S) + 0.35 * noise(24, S)
dx, dy = np.roll(h, -1, 1) - np.roll(h, 1, 1), np.roll(h, -1, 0) - np.roll(h, 1, 0)
n = np.dstack([-dx * 6, -dy * 6, np.ones_like(h)])
n /= np.linalg.norm(n, axis=2, keepdims=True)
save("T_MJ_WaterN", n * 0.5 + 0.5, "RGB")

# --- floating leaf patch: bright leaves inside a ragged round mask, transparent elsewhere ---
S = 256
leaves = []
for f in ("foliage_10.png", "foliage_18.png", "foliage_21.png", "foliage_25.png", "foliage_68.png"):
    im = Image.open(os.path.join(SRC, f)).convert("RGBA")
    im = im.crop(im.getbbox()); im.thumbnail((64, 64), Image.LANCZOS); leaves.append(im)
cols = [(1.0, 0.55, 0.15), (0.95, 0.70, 0.25), (0.85, 0.35, 0.10), (1.0, 0.62, 0.20)]
canvas = Image.new("RGBA", (S, S), (230, 130, 40, 0))
edge = noise(5, S)
for _ in range(900):
    x, y = rng.integers(10, S - 10, 2)
    if np.hypot(x - S / 2, y - S / 2) > S * (0.22 + 0.26 * edge[y, x]): continue
    leaf = leaves[rng.integers(len(leaves))]
    s = int(rng.uniform(14, 24))
    l = leaf.resize((s, max(4, int(s * leaf.height / leaf.width))), Image.LANCZOS)
    a = np.asarray(l).astype(np.float32) / 255
    lum = a[..., :3].mean(-1, keepdims=True); lum = lum / max(lum[a[..., 3:] > 0.5].mean(), 1e-3)
    rgb = np.clip(np.asarray(cols[rng.integers(len(cols))]) * rng.uniform(0.8, 1.1) * (0.5 + 0.5 * lum), 0, 1)
    l = Image.fromarray((np.dstack([rgb, a[..., 3:]]) * 255).astype(np.uint8), "RGBA").rotate(rng.uniform(0, 360), Image.BICUBIC, expand=True)
    canvas.alpha_composite(l, (int(x - l.width / 2), int(y - l.height / 2)))
canvas.save(os.path.join(OUT, "T_MJ_LeafFloat.png"))

# --- glowing flower card: thin dark stems, spiky star-shaped orange heads ---
K = 4; S = 64 * K
im = Image.new("RGBA", (S, S), (60, 45, 22, 0)); d = ImageDraw.Draw(im)
for i in range(6):
    x0 = S * (0.2 + 0.12 * i) + rng.uniform(-8, 8); top = S * rng.uniform(0.12, 0.55); x1 = x0 + rng.uniform(-30, 30)
    pts = [(x0 + (x1 - x0) * t * t, S - (S - top) * t) for t in np.linspace(0, 1, 8)]
    d.line(pts, fill=(70, 52, 26, 255), width=5)
    mx, my = pts[3]
    d.polygon([(mx, my), (mx + rng.choice([-1, 1]) * 26, my - 14), (mx + 4, my - 10)], fill=(84, 62, 28, 255))
    # head: one flat irregular star, no inner disc
    r, pts_n = rng.uniform(17, 25), int(rng.integers(8, 12))
    a0, star = rng.uniform(0, 1), []
    for k in range(pts_n * 2):
        rr = r * (rng.uniform(0.75, 1.35) if k % 2 == 0 else rng.uniform(0.32, 0.45))
        ang = a0 + k * np.pi / pts_n
        star.append((x1 + np.cos(ang) * rr, top + np.sin(ang) * rr))
    d.polygon(star, fill=(255, int(rng.uniform(135, 175)), 45, 255))
im.resize((64, 64), Image.LANCZOS).save(os.path.join(OUT, "T_MJ_Flower.png"))
print("ok")
