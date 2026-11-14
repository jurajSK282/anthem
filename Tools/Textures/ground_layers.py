# Generates the four seamless 512 px Emberwood ground layers (soil, litter, drift, path).
# Usage: python Tools/Textures/ground_layers.py <leaf-sprite-folder>
# Soil is periodic noise (no photo, no mirroring); leaves are real sprites tinted to an earth palette.
import os, sys
import numpy as np
from PIL import Image

S = 512
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Art", "Emberwood", "MJ")
SRC = sys.argv[1]
rng = np.random.default_rng(7)

UMBER = (0.30, 0.21, 0.15)
SIENNA = (0.62, 0.30, 0.13)
OCHRE = (0.80, 0.62, 0.27)
PALE = (0.66, 0.56, 0.38)
RUST = (0.50, 0.20, 0.10)


def noise(cells):
    """Seamless smooth noise 0..1: random grid, wrapped, bicubic upscaled."""
    g = rng.random((cells, cells)).astype(np.float32)
    g = np.tile(g, (3, 3))
    im = Image.fromarray(g).resize((S * 3, S * 3), Image.BICUBIC)
    return np.clip(np.asarray(im)[S:2 * S, S:2 * S], 0, 1)


def fbm(cells, octaves):
    n, amp, tot = np.zeros((S, S), np.float32), 1.0, 0.0
    for _ in range(octaves):
        n += noise(cells) * amp
        tot += amp; amp *= 0.55; cells *= 2
    return n / tot


def earth(dark, light, grey=0.0):
    """Fine-grained dirt: low contrast so the tile does not read as a pattern."""
    n = fbm(20, 5)
    n = np.clip((n - 0.5) * 1.7 + 0.5, 0, 1)
    hue = fbm(5, 3)[..., None]
    img = np.asarray(dark)[None, None] * (1 - n[..., None]) + np.asarray(light)[None, None] * n[..., None]
    img = img * (0.96 + 0.08 * hue) + grey * (hue - 0.5) * np.array([0.02, 0.05, 0.06])
    img += (rng.random((S, S, 1)) - 0.5) * 0.05      # grit
    return np.clip(img, 0, 1).astype(np.float32)


def specks(img, count, col, rmin, rmax):
    """Pebbles / bark crumbs: small soft ellipses, wrapped."""
    yy, xx = np.mgrid[0:S, 0:S]
    for _ in range(count):
        cx, cy, r = rng.random() * S, rng.random() * S, rng.uniform(rmin, rmax)
        a, sq = rng.random() * np.pi, rng.uniform(0.5, 1.0)
        dx, dy = (xx - cx + S / 2) % S - S / 2, (yy - cy + S / 2) % S - S / 2
        u, v = dx * np.cos(a) + dy * np.sin(a), (-dx * np.sin(a) + dy * np.cos(a)) / sq
        m = np.clip(1.4 - np.sqrt(u * u + v * v) / r, 0, 1)[..., None]
        shade = 0.75 + 0.5 * np.clip(-(u + v) / (2 * r), -0.5, 0.5)[..., None]   # lit from one side
        c = np.asarray(col) * rng.uniform(0.7, 1.15)
        img[:] = img * (1 - m) + c * shade * m
    return img


def load(names, px=96):
    out = []
    for f in names:
        im = Image.open(os.path.join(SRC, f)).convert("RGBA")
        im = im.crop(im.getbbox())
        im.thumbnail((px, px), Image.LANCZOS)
        out.append(im)
    return out


def stamp(img, leaves, count, palette, size=(20, 34)):
    """Paste tinted leaves with wrap-around. palette: list of (colour, weight)."""
    cols, w = [p[0] for p in palette], np.array([p[1] for p in palette], float)
    canvas = Image.fromarray((img * 255).astype(np.uint8)).convert("RGBA")
    for _ in range(count):
        leaf = leaves[rng.integers(len(leaves))]
        s = int(rng.uniform(*size))
        l = leaf.resize((s, max(4, int(s * leaf.height / leaf.width))), Image.LANCZOS)
        a = np.asarray(l).astype(np.float32) / 255
        lum = a[..., :3].mean(-1, keepdims=True)
        lum = lum / max(lum[a[..., 3:] > 0.5].mean(), 1e-3)          # keep veins, drop source colour
        c = np.asarray(cols[rng.choice(len(cols), p=w / w.sum())]) * rng.uniform(0.85, 1.12)
        rgb = np.clip(c * (0.45 + 0.55 * lum), 0, 1)
        l = Image.fromarray((np.dstack([rgb, a[..., 3:]]) * 255).astype(np.uint8), "RGBA")
        l = l.rotate(rng.uniform(0, 360), Image.BICUBIC, expand=True)
        x, y = int(rng.integers(S)), int(rng.integers(S))
        for ox in (-S, 0):
            for oy in (-S, 0):
                canvas.alpha_composite(l, (x + ox, y + oy)) if 0 <= x + ox and 0 <= y + oy else \
                    canvas.paste(l, (x + ox, y + oy), l)
    return np.asarray(canvas.convert("RGB")).astype(np.float32) / 255


def save(name, img):
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(os.path.join(OUT, name + ".png"))
    print(name, (img.mean((0, 1)) * 255).round())


leaves = load(["foliage_10.png", "foliage_18.png", "foliage_21.png", "foliage_25.png", "foliage_52.png", "foliage_68.png"])
twigs = load(["yshaped-brown-twig-with-small-thorns-detailed-texture-free-png.webp"], 128)
moss = load(["pngtree-vibrant-green-moss-clump-isolated-on-black-background-png-image_18906130.webp",
             "pngtree-realistic-green-forest-moss-ground-cover-png-image_19652702.webp"], 128)
BROWN = (0.47, 0.31, 0.19)
WOOD, OLIVE = [((0.50, 0.38, 0.27), 1)], [((0.40, 0.38, 0.19), 1)]
old = [(BROWN, 3), (RUST, 1)]          # faded bottom layer: never darker than the soil highlights

soil = specks(earth((0.20, 0.14, 0.10), (0.42, 0.31, 0.22)), 90, (0.50, 0.42, 0.33), 1.5, 4)
soil = stamp(soil, moss, 2, OLIVE, (40, 60))
soil = stamp(soil, twigs, 7, WOOD, (50, 90))
soil = stamp(soil, leaves, 16, old)
save("T_L_Soil", soil)

litter = earth((0.26, 0.18, 0.12), (0.48, 0.34, 0.22))
litter = stamp(litter, moss, 3, OLIVE, (50, 80))
litter = stamp(litter, leaves, 300, old)
litter = stamp(litter, twigs, 8, WOOD, (50, 90))
litter = stamp(litter, leaves, 240, [(SIENNA, 4), (RUST, 2), (OCHRE, 1), (PALE, 1)])
save("T_L_Litter", litter)

drift = earth((0.30, 0.20, 0.12), (0.50, 0.36, 0.20))
drift = stamp(drift, leaves, 450, [(SIENNA, 2), (BROWN, 1)])
drift = stamp(drift, leaves, 520, [(OCHRE, 5), (PALE, 3), (SIENNA, 2)])
save("T_L_Drift", drift)

path = specks(earth((0.30, 0.25, 0.20), (0.52, 0.45, 0.36), grey=1.0), 160, (0.62, 0.57, 0.50), 1.5, 5)
path = stamp(path, twigs, 4, WOOD, (40, 70))
path = stamp(path, leaves, 24, [(SIENNA, 1), (PALE, 1), (BROWN, 2)])
save("T_L_Path", path)

sheet = np.concatenate([np.tile(t, (2, 2, 1))[:768, :768] for t in (soil, litter, drift, path)], 1)
if len(sys.argv) > 2:
    Image.fromarray((sheet * 255).astype(np.uint8)).resize((1536, 384)).save(sys.argv[2])
