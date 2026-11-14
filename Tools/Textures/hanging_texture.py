# Generates T_MJ_Hanging.png (128 x 256, RGBA): left half = hanging vine keyed from the user's image,
# right half = procedural moss strands. Both hang from the top edge.
# Usage: python Tools/Textures/hanging_texture.py <vine image>
import os, sys
import numpy as np
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Art", "Emberwood", "MJ", "T_MJ_Hanging.png")
rng = np.random.default_rng(31)
W, H = 64, 256
TINT = np.array([0.46, 0.38, 0.22])          # dry olive-brown: the scene has no green

# --- vine: the source is a JPG with a baked light checkerboard, so key on saturation and darkness ---
src = np.asarray(Image.open(sys.argv[1]).convert("RGB")).astype(np.float32) / 255
sat, lum = src.max(-1) - src.min(-1), src.mean(-1)
mask = np.clip(np.maximum((sat - 0.10) / 0.10, (0.55 - lum) / 0.15), 0, 1)
ys, xs = np.where(mask > 0.5)
box = (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)
rgb = np.clip(TINT * (0.35 + 1.5 * lum[..., None]), 0, 1)
vine = Image.fromarray((np.dstack([rgb, mask]) * 255).astype(np.uint8), "RGBA").crop(box)
w = min(W, max(8, int(vine.width * H / vine.height)))
vine = vine.resize((w, H), Image.LANCZOS)

# --- moss: ragged strands of different lengths, drawn at 4x and downsampled ---
K = 4
moss = Image.new("RGBA", (W * K, H * K), (0, 0, 0, 0))
d = ImageDraw.Draw(moss)
for _ in range(16):
    x, length = rng.uniform(8, W - 8) * K, rng.uniform(0.25, 1.0) * H * K
    col = tuple(int(c * 255 * rng.uniform(0.6, 1.1)) for c in TINT) + (255,)
    pts, y, phase = [], 0.0, rng.uniform(0, 6)
    while y < length:
        pts.append((x + np.sin(y * 0.012 + phase) * 5 * K * (y / length), y))
        y += 6 * K
    for i in range(len(pts) - 1):
        d.line([pts[i], pts[i + 1]], fill=col, width=max(2, int((3.2 - 2.2 * i / len(pts)) * K)))
        if rng.random() < 0.45:                      # side tufts
            px, py = pts[i]
            d.line([(px, py), (px + rng.uniform(-5, 5) * K, py + rng.uniform(3, 9) * K)], fill=col, width=K)
d.rectangle([0, 0, W * K, 5 * K], fill=tuple(int(c * 255 * 0.8) for c in TINT) + (255,))   # clump at the branch
moss = moss.resize((W, H), Image.LANCZOS)

atlas = Image.new("RGBA", (W * 2, H), tuple(int(c * 255) for c in TINT) + (0,))
atlas.alpha_composite(vine, ((W - w) // 2, 0))
atlas.alpha_composite(moss, (W, 0))
a = np.asarray(atlas).copy()
a[a[..., 3] < 100, :3] = (TINT * 255).astype(np.uint8)      # colour bleed so mips do not darken the edges
Image.fromarray(a, "RGBA").save(OUT)
print("ok", atlas.size)
