"""Builds sparse, gap-filled leaf cards (alpha cutout) from real leaf scans with alpha.
Usage: python Tools/Textures/make_leafcards.py <leaf_scan_dir> <out_dir>
Writes T_MJ_LeafCardA.png (bright autumn) and T_MJ_LeafCardB.png (dark undergrowth), 256x256 RGBA.
Leaf sources: foliage_*.png single leaves with alpha. Recoloured via luminance -> autumn palettes."""
import glob, math, os, random, sys
from PIL import Image, ImageDraw, ImageOps

Image.MAX_IMAGE_PIXELS = None
S = 2          # supersample
SIZE = 256
W = SIZE * S


def load_leaves(src):
    out = []
    for f in sorted(glob.glob(os.path.join(src, "foliage_*.png"))):
        im = Image.open(f).convert("RGBA")
        if min(im.size) < 400 or im.width / im.height > 3 or im.height / im.width > 3:
            continue          # skip tall twig sprigs
        if os.path.basename(f) == "foliage_68.png":
            continue          # sheet of 12 petals, not a single leaf
        im.thumbnail((160, 160), Image.LANCZOS)
        im = im.crop(im.getchannel("A").getbbox())
        out.append(im)
    return out


def recolor(leaf, dark, light):
    gray = ImageOps.autocontrast(leaf.convert("L"), cutoff=2)
    col = ImageOps.colorize(gray, black=dark, white=light).convert("RGBA")
    col.putalpha(leaf.getchannel("A"))
    return col


def twigs(d, rng, col):
    nodes = []
    for _ in range(rng.randint(3, 5)):
        x, y = W / 2 + rng.uniform(-40, 40), W * 0.98
        ang = math.radians(rng.uniform(-48, 48))
        length = rng.uniform(0.55, 0.9) * W
        steps = 14
        px, py = x, y
        for s in range(steps):
            ang += math.radians(rng.uniform(-9, 9))
            nx, ny = px + math.sin(ang) * length / steps, py - math.cos(ang) * length / steps
            d.line([px, py, nx, ny], fill=col, width=max(2, int(7 - s * 0.3)))
            nodes.append((nx, ny, ang))
            if s > 2 and s % 3 == 0:
                sa = ang + math.radians(rng.choice([-1, 1]) * rng.uniform(30, 60))
                bx, by = nx, ny
                for t in range(rng.randint(2, 4)):
                    bx2, by2 = bx + math.sin(sa) * 26, by - math.cos(sa) * 26
                    d.line([bx, by, bx2, by2], fill=col, width=3)
                    nodes.append((bx2, by2, sa))
                    bx, by = bx2, by2
            px, py = nx, ny
    return nodes


def make(leaves, palettes, twig_col, seed, target_cover, leaf_len):
    rng = random.Random(seed)
    canvas = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(canvas)
    nodes = twigs(d, rng, twig_col)
    rng.shuffle(nodes)
    placed = 0
    for (nx, ny, ang) in nodes:
        if placed > 70:
            break
        src = rng.choice(leaves)
        dark, light = rng.choice(palettes)
        leaf = recolor(src, dark, light)
        sc = rng.uniform(*leaf_len) * S / max(leaf.size)
        leaf = leaf.resize((max(4, int(leaf.width * sc)), max(4, int(leaf.height * sc))), Image.LANCZOS)
        leaf = leaf.rotate(math.degrees(ang) + rng.uniform(-110, 110), expand=True, resample=Image.BICUBIC)
        canvas.alpha_composite(leaf, (int(nx - leaf.width / 2), int(ny - leaf.height / 2)))
        placed += 1
        a = canvas.getchannel("A").resize((64, 64))
        if sum(1 for p in a.getdata() if p > 128) / 4096 > target_cover:
            break
    # darker toward the bottom of the card (the ground side), lighter at the tips
    px = canvas.load()
    for y in range(W):
        k = 0.50 + 0.50 * (1 - y / W) ** 0.8
        for x in range(W):
            r, g, b, a = px[x, y]
            if a:
                px[x, y] = (int(r * k), int(g * k), int(b * k), a)
    out = canvas.resize((SIZE, SIZE), Image.BOX)
    # hard alpha, bleed a mean colour into empty pixels so mips/clip don't get dark fringes
    p = out.load()
    cols = [p[x, y][:3] for y in range(SIZE) for x in range(SIZE) if p[x, y][3] > 200]
    mean = tuple(sum(c[i] for c in cols) // max(1, len(cols)) for i in range(3))
    for y in range(SIZE):
        for x in range(SIZE):
            r, g, b, a = p[x, y]
            p[x, y] = (r, g, b, 255) if a > 110 else (*mean, 0)
    return out


A = [((60, 20, 8), (205, 105, 30)), ((70, 28, 10), (225, 150, 45)), ((55, 12, 8), (180, 55, 22)),
     ((80, 45, 14), (215, 175, 70)), ((60, 18, 10), (160, 60, 25))]
B = [((30, 14, 8), (120, 58, 24)), ((40, 10, 8), (110, 34, 18)), ((28, 20, 12), (100, 74, 34)),
     ((36, 16, 10), (140, 70, 28))]

if __name__ == "__main__":
    src, dst = sys.argv[1], sys.argv[2]
    leaves = load_leaves(src)
    print(len(leaves), "leaf sources")
    make(leaves, A, (34, 22, 14, 255), 7, 0.30, (28, 48)).save(os.path.join(dst, "T_MJ_LeafCardA.png"))
    make(leaves, B, (22, 14, 10, 255), 13, 0.28, (26, 46)).save(os.path.join(dst, "T_MJ_LeafCardB.png"))
