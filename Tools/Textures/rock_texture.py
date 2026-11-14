# Generates T_MJ_RockLight.png: a seamless 256 px rock surface with no distinct repeating features.
# Usage: python Tools/Textures/rock_texture.py
import os
import numpy as np
from PIL import Image

S = 256
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Art", "Emberwood", "MJ", "T_MJ_RockLight.png")
rng = np.random.default_rng(11)


def noise(cells):
    g = np.tile(rng.random((cells, cells)).astype(np.float32), (3, 3))
    im = Image.fromarray(g).resize((S * 3, S * 3), Image.BICUBIC)
    return np.clip(np.asarray(im)[S:2 * S, S:2 * S], 0, 1)


def fbm(cells, octaves, ridged=False):
    n, amp, tot = np.zeros((S, S), np.float32), 1.0, 0.0
    for _ in range(octaves):
        o = noise(cells)
        n += (1 - np.abs(o * 2 - 1) if ridged else o) * amp
        tot += amp; amp *= 0.55; cells *= 2
    return n / tot


base = fbm(7, 5)                                   # mottled stone
cracks = np.clip((fbm(8, 4, True) - 0.74) * 9, 0, 1)   # thin dark veins where ridged noise peaks
lichen = np.clip((fbm(4, 3) - 0.58) * 5, 0, 1)[..., None]
v = np.clip(0.50 + (base - 0.5) * 1.7 - cracks * 0.35, 0, 1)[..., None]
img = np.array([0.56, 0.51, 0.45]) * (0.45 + 0.75 * v)
img = img * (1 - lichen * 0.35) + np.array([0.55, 0.50, 0.33]) * lichen * 0.35   # faint ochre lichen stains
img += (rng.random((S, S, 1)) - 0.5) * 0.06
Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(OUT)
print("rock mean", (img.mean((0, 1)) * 255).round())
