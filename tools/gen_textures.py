"""Generates all original textures for HUMMINGBIRD X (no third-party images).
Run: python3 tools/gen_textures.py   -> Assets/HummingbirdX/Resources/Textures
All tiling textures are made periodic with FFT noise so they tile seamlessly."""
import math, os, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "HummingbirdX", "Resources", "Textures")
os.makedirs(OUT, exist_ok=True)


def fnoise(n, beta=2.0, seed=0, sx=1.0, sy=1.0, m=None):
    """Periodic 1/f^beta noise, normalised to 0..1. sx/sy stretch frequencies."""
    m = m or n
    rng = np.random.default_rng(seed)
    ky = np.fft.fftfreq(n)[:, None] * sy
    kx = np.fft.fftfreq(m)[None, :] * sx
    k = np.sqrt(kx * kx + ky * ky)
    k[0, 0] = 1.0
    amp = 1.0 / k ** (beta / 2.0)
    amp[0, 0] = 0
    ph = rng.uniform(0, 2 * np.pi, (n, m))
    f = np.real(np.fft.ifft2(amp * np.exp(1j * ph)))
    f -= f.min(); f /= f.max()
    return f


def normal_from_height(h, strength=4.0):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    nz = np.ones_like(h)
    l = np.sqrt(dx * dx + dy * dy + nz * nz)
    n = np.stack([-dx / l, dy / l, nz / l], -1)
    return ((n * 0.5 + 0.5) * 255).clip(0, 255).astype(np.uint8)


def lerp(a, b, t):
    return a + (b - a) * t


def colorize(t, stops):
    """t: HxW 0..1, stops: list of (pos, (r,g,b))."""
    t = np.clip(t, 0, 1)
    out = np.zeros(t.shape + (3,))
    for i in range(len(stops) - 1):
        p0, c0 = stops[i]; p1, c1 = stops[i + 1]
        mask = (t >= p0) & (t <= p1)
        u = ((t - p0) / max(p1 - p0, 1e-6))[mask][:, None]
        out[mask] = np.array(c0) * (1 - u) + np.array(c1) * u
    return out


def save(arr, name):
    if arr.dtype != np.uint8:
        arr = (np.clip(arr, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(arr).save(os.path.join(OUT, name), optimize=True)
    print("wrote", name, arr.shape)


def ss(img, n):
    return img.resize((n, n) if isinstance(n, int) else n, Image.LANCZOS)


# ---------------------------------------------------------------- bark
def bark():
    n = 1024
    fib = fnoise(n, 2.2, 1, sx=0.35, sy=6.0)      # long vertical fibres
    big = fnoise(n, 2.8, 2)
    crack = np.abs(fnoise(n, 2.0, 3, sx=0.5, sy=3.0) - 0.5) * 2
    crack = 1 - np.clip(crack * 6, 0, 1)            # thin dark lines
    h = fib * 0.6 + big * 0.4 - crack * 0.35
    h = (h - h.min()) / (h.max() - h.min())
    col = colorize(h, [(0, (0.10, 0.08, 0.06)), (0.45, (0.30, 0.25, 0.19)),
                       (0.75, (0.45, 0.40, 0.32)), (1, (0.58, 0.55, 0.47))])
    lichen = fnoise(n, 2.4, 4)
    lm = np.clip((lichen - 0.68) * 6, 0, 1)[..., None] * (h[..., None] > 0.4)
    col = lerp(col, np.array([0.55, 0.60, 0.50]), lm * 0.6)
    save(col, "bark.png")
    save(normal_from_height(h, 6.0), "bark_n.png")


def moss():
    n = 512
    h = fnoise(n, 1.6, 10) * 0.5 + fnoise(n, 2.6, 11) * 0.5
    col = colorize(h, [(0, (0.06, 0.12, 0.03)), (0.5, (0.18, 0.32, 0.07)),
                       (0.8, (0.32, 0.46, 0.12)), (1, (0.50, 0.60, 0.22))])
    save(col, "moss.png")
    save(normal_from_height(h, 5.0), "moss_n.png")


# ---------------------------------------------------------------- ground (leaf litter)
def ground():
    n, S = 1024, 2
    N = n * S
    rng = random.Random(5)
    soil = fnoise(N, 2.4, 20)
    base = colorize(soil, [(0, (0.07, 0.05, 0.03)), (0.6, (0.16, 0.11, 0.07)), (1, (0.24, 0.18, 0.11))])
    img = Image.fromarray((base * 255).astype(np.uint8)).convert("RGB")
    hmap = Image.fromarray((soil * 60).astype(np.uint8))
    d = ImageDraw.Draw(img); dh = ImageDraw.Draw(hmap)
    palette = [(98, 62, 30), (130, 88, 42), (150, 110, 55), (80, 50, 28), (110, 96, 40), (70, 90, 35), (160, 120, 70)]
    for i in range(2600):
        cx, cy = rng.uniform(0, N), rng.uniform(0, N)
        L = rng.uniform(18, 70) * S; W = L * rng.uniform(0.25, 0.5)
        a = rng.uniform(0, math.pi)
        c = palette[rng.randrange(len(palette))]
        shade = rng.uniform(0.7, 1.15)
        c = tuple(int(min(255, v * shade)) for v in c)
        hv = int(70 + i / 2600 * 160)
        pts = []
        for k in range(16):
            t = k / 15.0
            x = (t - 0.5) * L
            y = math.sin(t * math.pi) * W * 0.5
            pts.append((x, y))
        for k in range(15, -1, -1):
            t = k / 15.0
            pts.append(((t - 0.5) * L, -math.sin(t * math.pi) * W * 0.5))
        for ox in (-N, 0, N):
            for oy in (-N, 0, N):
                P = [(cx + ox + x * math.cos(a) - y * math.sin(a), cy + oy + x * math.sin(a) + y * math.cos(a)) for x, y in pts]
                if max(p[0] for p in P) < 0 or min(p[0] for p in P) > N or max(p[1] for p in P) < 0 or min(p[1] for p in P) > N:
                    continue
                d.polygon(P, fill=c)
                dh.polygon(P, fill=hv)
                x0, y0 = cx + ox - math.cos(a) * L / 2, cy + oy - math.sin(a) * L / 2
                x1, y1 = cx + ox + math.cos(a) * L / 2, cy + oy + math.sin(a) * L / 2
                d.line([(x0, y0), (x1, y1)], fill=tuple(int(v * 0.75) for v in c), width=max(1, S))
    img = img.filter(ImageFilter.GaussianBlur(0.6 * S)).resize((n, n), Image.LANCZOS)
    h = np.asarray(hmap.filter(ImageFilter.GaussianBlur(1.5 * S)).resize((n, n), Image.LANCZOS)).astype(float) / 255
    col = np.asarray(img).astype(float) / 255
    ao = 0.65 + 0.35 * np.clip(h * 1.6, 0, 1)
    save(col * ao[..., None], "ground.png")
    save(normal_from_height(h, 5.0), "ground_n.png")


def rock():
    n = 1024
    h = fnoise(n, 2.6, 30) * 0.7 + fnoise(n, 1.8, 31) * 0.3
    cr = 1 - np.clip(np.abs(fnoise(n, 2.2, 32) - 0.5) * 18, 0, 1)
    h = h - cr * 0.2
    h = (h - h.min()) / (h.max() - h.min())
    col = colorize(h, [(0, (0.06, 0.07, 0.07)), (0.5, (0.24, 0.25, 0.23)), (1, (0.46, 0.46, 0.42))])
    m = np.clip((fnoise(n, 2.2, 33) - 0.55) * 4, 0, 1)[..., None]
    col = lerp(col, np.array([0.16, 0.26, 0.08]), m * 0.7)
    save(col, "rock.png")
    save(normal_from_height(h, 7.0), "rock_n.png")


# ---------------------------------------------------------------- leaves (2x2 atlas with alpha)
def leaf_cell(kind, size, seed):
    S = 2
    W = size * S
    rng = random.Random(seed)
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    hm = Image.new("L", (W, W), 0)
    d = ImageDraw.Draw(img); dh = ImageDraw.Draw(hm)
    cx = W / 2
    top, bot = W * 0.04, W * 0.97
    L = bot - top

    def width_at(t):
        if kind == "banana":
            return W * 0.21 * (math.sin(min(t, 0.98) * math.pi) ** 0.55)
        if kind == "heart":
            return W * 0.42 * (math.sin(t * math.pi) ** 0.8) * (1.0 - 0.55 * t) + (W * 0.12 if t > 0.82 else 0) * (t - 0.82) / 0.18
        if kind == "monstera":
            return W * 0.44 * (math.sin(t * math.pi) ** 0.6) * (1 - 0.2 * t)
        return W * 0.12 * math.sin(t * math.pi)

    pts_l, pts_r = [], []
    for k in range(80):
        t = k / 79.0
        y = top + t * L
        w = width_at(t)
        pts_l.append((cx - w, y)); pts_r.append((cx + w, y))
    poly = pts_l + pts_r[::-1]
    base_g = np.array([0.10, 0.30, 0.06]) * rng.uniform(0.85, 1.15)
    d.polygon(poly, fill=(255, 255, 255, 255))
    dh.polygon(poly, fill=120)
    alpha = np.asarray(img)[..., 3].copy()

    # colour: darker at margins, lighter along midrib, subtle mottling
    yy, xx = np.mgrid[0:W, 0:W]
    dist = np.abs(xx - cx) / (W * 0.45)
    mott = np.asarray(Image.fromarray((fnoise(W, 2.0, seed) * 255).astype(np.uint8))).astype(float) / 255
    col = np.zeros((W, W, 3))
    for i in range(3):
        col[..., i] = base_g[i] * (1.15 - 0.45 * dist) * (0.85 + 0.3 * mott) * (0.9 + 0.2 * (yy / W))
    img2 = Image.fromarray((np.clip(col, 0, 1) * 255).astype(np.uint8)).convert("RGBA")
    d2 = ImageDraw.Draw(img2)
    vein = (150, 175, 90, 255)
    # midrib
    d2.line([(cx, top), (cx, bot)], fill=vein, width=int(W * 0.012))
    dh.line([(cx, top), (cx, bot)], fill=200, width=int(W * 0.016))
    nveins = {"banana": 46, "heart": 9, "monstera": 10, "palm": 14}[kind]
    for v in range(nveins):
        t = (v + 0.5) / nveins
        y0 = top + t * L
        w = width_at(min(t + 0.06, 1))
        for s in (-1, 1):
            if kind == "banana":
                pts = [(cx, y0), (cx + s * w * 0.6, y0 + L * 0.03), (cx + s * w, y0 + L * 0.05)]
            else:
                pts = [(cx, y0), (cx + s * w * 0.5, y0 + L * 0.05), (cx + s * w * 0.95, y0 + L * 0.13)]
            d2.line(pts, fill=(120, 150, 70, 255), width=max(1, int(W * (0.003 if kind == "banana" else 0.006))))
            dh.line(pts, fill=170, width=max(1, int(W * 0.006)))
    a = Image.fromarray(alpha)
    if kind == "monstera":
        da = ImageDraw.Draw(a)
        for v in range(1, 8):
            t = v / 8.5
            y0 = top + t * L
            w = width_at(t)
            for s in (-1, 1):
                # slits from margin
                da.polygon([(cx + s * w * 1.05, y0 + L * 0.02), (cx + s * w * 0.35, y0 + L * 0.055),
                            (cx + s * w * 1.05, y0 + L * 0.075)], fill=0)
                # holes
                hx = cx + s * w * 0.28; hy = y0 + L * 0.02
                da.ellipse([hx - W * 0.018, hy - W * 0.03, hx + W * 0.018, hy + W * 0.03], fill=0)
    if kind == "banana":
        da = ImageDraw.Draw(a)
        for v in range(5):  # wind tears
            t = rng.uniform(0.15, 0.85); y0 = top + t * L; w = width_at(t); s = rng.choice((-1, 1))
            da.polygon([(cx + s * w * 1.05, y0), (cx + s * w * 0.15, y0 + L * 0.012), (cx + s * w * 1.05, y0 + L * 0.02)], fill=0)
    # brown tip/edge damage
    edge = np.asarray(a.filter(ImageFilter.MinFilter(9))).astype(float) / 255
    c = np.asarray(img2).astype(float)
    edgeband = (np.asarray(a).astype(float) / 255) - edge
    brown = np.array([110, 85, 40])
    c[..., :3] = c[..., :3] * (1 - edgeband[..., None] * 0.6) + brown * edgeband[..., None] * 0.6
    c[..., 3] = np.asarray(a)
    out = Image.fromarray(c.clip(0, 255).astype(np.uint8), "RGBA").resize((size, size), Image.LANCZOS)
    hmo = hm.filter(ImageFilter.GaussianBlur(2)).resize((size, size), Image.LANCZOS)
    return out, hmo


def palm_cell(size, seed):
    S = 2; W = size * S
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    hm = Image.new("L", (W, W), 0); dh = ImageDraw.Draw(hm)
    cx = W / 2
    d.line([(cx, W * 0.02), (cx, W * 0.98)], fill=(90, 110, 50, 255), width=int(W * 0.012))
    dh.line([(cx, W * 0.02), (cx, W * 0.98)], fill=220, width=int(W * 0.012))
    for k in range(26):
        t = k / 25.0
        y = W * (0.05 + t * 0.9)
        ln = W * 0.45 * math.sin(0.15 + t * 2.9) * (1 - 0.3 * t)
        for s in (-1, 1):
            tip = (cx + s * ln, y + W * 0.10)
            mid = (cx + s * ln * 0.5, y + W * 0.025)
            wdt = W * 0.012
            P = [(cx, y - wdt), (mid[0], mid[1] - wdt * 1.5), tip, (mid[0], mid[1] + wdt * 1.5), (cx, y + wdt)]
            g = (int(30 + 20 * t), int(80 + 30 * t), int(25 + 10 * t), 255)
            d.polygon(P, fill=g); dh.polygon(P, fill=140)
            d.line([(cx, y), mid, tip], fill=(110, 140, 70, 255), width=max(1, int(W * 0.003)))
    return img.resize((size, size), Image.LANCZOS), hm.filter(ImageFilter.GaussianBlur(2)).resize((size, size), Image.LANCZOS)


def leaves():
    size = 512
    atlas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0)); hat = Image.new("L", (1024, 1024), 0)
    cells = [leaf_cell("banana", size, 41), leaf_cell("heart", size, 42), leaf_cell("monstera", size, 43), palm_cell(size, 44)]
    for i, (c, h) in enumerate(cells):
        x, y = (i % 2) * size, (i // 2) * size
        atlas.paste(c, (x, y)); hat.paste(h, (x, y))
    # bleed colour into transparent area to avoid dark fringes in mips
    a = np.asarray(atlas).astype(float)
    rgb = Image.fromarray(a[..., :3].astype(np.uint8)).filter(ImageFilter.MaxFilter(5))
    bleed = np.asarray(rgb).astype(float)
    m = (a[..., 3:] > 8)
    a[..., :3] = np.where(m, a[..., :3], bleed)
    save(a.astype(np.uint8), "leaf_atlas.png")
    save(normal_from_height(np.asarray(hat).astype(float) / 255, 3.0), "leaf_atlas_n.png")


def fern():
    W, H = 512 * 2, 1024 * 2
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    cx = W / 2
    d.line([(cx, H * 0.02), (cx, H * 0.99)], fill=(60, 80, 30, 255), width=8)
    for k in range(34):
        t = k / 33.0
        y = H * (0.03 + t * 0.95)
        ln = W * 0.46 * math.sin(0.1 + t * 2.7) * (1 - 0.15 * t)
        for s in (-1, 1):
            # pinna: tapered serrated blade
            pts_a, pts_b = [], []
            for j in range(14):
                u = j / 13.0
                px = cx + s * ln * u; py = y + ln * 0.18 * u
                r = (1 - u) ** 0.8 * W * 0.030 * (1.0 + 0.25 * (j % 2))
                pts_a.append((px, py - r)); pts_b.append((px, py + r * 0.8))
            g = (int(40 + 20 * t), int(105 - 15 * t), 30, 255)
            d.polygon(pts_a + pts_b[::-1], fill=g)
            d.line([(cx, y), (cx + s * ln, y + ln * 0.18)], fill=(80, 110, 45, 255), width=3)
    img = img.resize((512, 1024), Image.LANCZOS)
    save(np.asarray(img), "fern.png")


# ---------------------------------------------------------------- flower petal (tintable, white)
def petal():
    W = 1024
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    cx = W / 2
    pts = []
    for k in range(100):
        t = k / 99.0
        w = W * 0.36 * (math.sin(t * math.pi) ** 0.7) * (0.6 + 0.4 * t)
        pts.append((cx - w, W * 0.97 - t * W * 0.94))
    pts += [(2 * cx - x, y) for x, y in pts[::-1]]
    d.polygon(pts, fill=(255, 255, 255, 255))
    a = np.asarray(img).astype(float)
    yy, xx = np.mgrid[0:W, 0:W]
    t = 1 - yy / W
    veins = 0.5 + 0.5 * np.cos((xx - cx) / (W * 0.012) * (0.5 + t))
    shade = (0.55 + 0.45 * t) * (0.93 + 0.07 * veins)
    a[..., 0] = 255 * shade; a[..., 1] = 255 * shade; a[..., 2] = 255 * shade
    out = Image.fromarray(a.clip(0, 255).astype(np.uint8), "RGBA").resize((512, 512), Image.LANCZOS)
    save(np.asarray(out), "petal.png")


# ---------------------------------------------------------------- feathers (tiling)
def feathers():
    n = 512
    h = np.zeros((n, n))
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    rows, cols = 12, 10
    for r in range(rows):
        for c in range(cols):
            cy = (r + 0.5) * n / rows
            cx = (c + 0.5 + 0.5 * (r % 2)) * n / cols
            dx = (xx - cx + n / 2) % n - n / 2
            dy = (yy - cy + n / 2) % n - n / 2
            rad = n / cols * 0.75
            d = np.sqrt(dx * dx + (dy * 1.25) ** 2) / rad
            bump = np.clip(1 - d, 0, 1) * (0.6 + 0.4 * np.clip(-dy / rad + 0.5, 0, 1))
            barb = 0.85 + 0.15 * np.cos(np.arctan2(dy, dx) * 28)
            h = np.maximum(h, bump * barb)
    h = (h - h.min()) / (h.max() - h.min())
    save(np.stack([h] * 3, -1), "feather_mask.png")
    save(normal_from_height(h, 3.0), "feather_n.png")


def hb_wing():
    W, H = 1024, 512
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    # wing root at left-middle; primaries fan to the right
    for k in range(10):
        t = k / 9.0
        ang = math.radians(-12 + t * 28)
        ln = W * (0.92 - t * 0.38)
        x1, y1 = 20 + ln * math.cos(ang), H * 0.45 + ln * math.sin(ang)
        wdt = H * 0.07
        P = [(20, H * 0.45 - wdt * 0.4), (x1 - 40, y1 - wdt), (x1, y1), (x1 - 40, y1 + wdt * 0.6), (20, H * 0.45 + wdt * 0.4)]
        g = int(70 - 20 * t)
        d.polygon(P, fill=(g, g + 6, g + 4, 150))
        d.line([(20, H * 0.45), (x1, y1)], fill=(40, 40, 40, 200), width=3)
    d.polygon([(10, H * 0.30), (W * 0.35, H * 0.38), (W * 0.32, H * 0.62), (10, H * 0.62)], fill=(40, 70, 45, 235))
    img = img.filter(ImageFilter.GaussianBlur(0.8))
    save(np.asarray(img), "hb_wing.png")


def smooth(P, steps=10):
    """closed Catmull-Rom spline through points"""
    out = []
    n = len(P)
    for i in range(n):
        p0, p1, p2, p3 = P[i - 1], P[i], P[(i + 1) % n], P[(i + 2) % n]
        for k in range(steps):
            t = k / steps; t2 = t * t; t3 = t2 * t
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2
                                    + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3) for j in range(2)))
    return out


# ---------------------------------------------------------------- butterflies (2x2 atlas, right wing pair; mirrored in mesh)
def butterfly():
    S = 512
    atlas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))

    def wing_shape(d, ox, oy, fill):
        fw = [(ox + 10, oy + S * 0.48), (ox + S * 0.25, oy + S * 0.10), (ox + S * 0.75, oy + S * 0.04),
              (ox + S * 0.95, oy + S * 0.12), (ox + S * 0.80, oy + S * 0.40), (ox + 14, oy + S * 0.52)]
        hw = [(ox + 10, oy + S * 0.52), (ox + S * 0.62, oy + S * 0.50), (ox + S * 0.78, oy + S * 0.72),
              (ox + S * 0.60, oy + S * 0.94), (ox + S * 0.25, oy + S * 0.90), (ox + 10, oy + S * 0.60)]
        fw, hw = smooth(fw), smooth(hw)
        d.polygon(fw, fill=fill); d.polygon(hw, fill=fill)
        return fw, hw

    specs = [
        ("morpho", (20, 90, 230, 255)), ("heliconius", (190, 30, 20, 255)),
        ("glasswing", (180, 210, 220, 60)), ("owl", (120, 85, 50, 255)),
    ]
    for i, (name, base) in enumerate(specs):
        ox, oy = (i % 2) * S, (i // 2) * S
        cell = Image.new("RGBA", (S * 2, S * 2), (0, 0, 0, 0))
        d = ImageDraw.Draw(cell)
        fw, hw = wing_shape(d, 0, 0, (0, 0, 0, 0))
        fw2 = [(x * 2, y * 2) for x, y in fw]; hw2 = [(x * 2, y * 2) for x, y in hw]
        border = (15, 12, 10, 255)
        d.polygon(fw2, fill=border); d.polygon(hw2, fill=border)
        cen = lambda P: (sum(p[0] for p in P) / len(P), sum(p[1] for p in P) / len(P))
        shrink = lambda P, f: [(cen(P)[0] + (x - cen(P)[0]) * f, cen(P)[1] + (y - cen(P)[1]) * f) for x, y in P]
        if name == "morpho":
            d.polygon(shrink(fw2, 0.82), fill=base); d.polygon(shrink(hw2, 0.80), fill=(30, 110, 240, 255))
            for P in (fw2, hw2):
                c = cen(P)
                for k in range(5):
                    d.ellipse([c[0] + 150 + k * 18, c[1] - 120 + k * 50, c[0] + 166 + k * 18, c[1] - 104 + k * 50], fill=(240, 240, 255, 255))
        elif name == "heliconius":
            d.polygon(shrink(fw2, 0.75), fill=base)
            c = cen(fw2)
            d.ellipse([c[0] + 40, c[1] - 120, c[0] + 230, c[1] - 30], fill=(250, 210, 40, 255))
            d.polygon(shrink(hw2, 0.7), fill=(15, 12, 10, 255))
            for k in range(6):
                d.line([(cen(hw2)[0] - 200 + k * 50, cen(hw2)[1] - 30), (cen(hw2)[0] - 150 + k * 60, cen(hw2)[1] + 160)], fill=(200, 60, 20, 255), width=10)
        elif name == "glasswing":
            d.polygon(shrink(fw2, 0.86), fill=base); d.polygon(shrink(hw2, 0.86), fill=base)
            c = cen(fw2)
            d.ellipse([c[0] + 150, c[1] - 160, c[0] + 250, c[1] - 60], fill=(250, 250, 250, 220))
        else:
            d.polygon(shrink(fw2, 0.85), fill=(140, 110, 75, 255)); d.polygon(shrink(hw2, 0.82), fill=(100, 75, 50, 255))
            c = cen(hw2)
            for r, col in ((95, (230, 200, 90, 255)), (75, (20, 15, 10, 255)), (30, (240, 240, 230, 255))):
                d.ellipse([c[0] + 40 - r, c[1] - r, c[0] + 40 + r, c[1] + r], fill=col)
        # scale texture
        a = np.asarray(cell).astype(float)
        nz = fnoise(S * 2, 1.2, 70 + i)[..., None]
        a[..., :3] *= 0.85 + 0.3 * nz
        cell = Image.fromarray(a.clip(0, 255).astype(np.uint8), "RGBA").filter(ImageFilter.GaussianBlur(1.2))
        atlas.paste(cell.resize((S, S), Image.LANCZOS), (ox, oy))
    save(np.asarray(atlas), "butterfly_atlas.png")


def water():
    n = 512
    h = fnoise(n, 2.4, 80, sx=1.0, sy=1.6) * 0.6 + fnoise(n, 1.6, 81) * 0.4
    save(normal_from_height(h, 8.0), "water_n.png")


def noise_rgb():
    n = 512
    r = fnoise(n, 2.0, 90); g = fnoise(n, 2.8, 91); b = fnoise(n, 1.4, 92)
    save(np.stack([r, g, b], -1), "noise_mask.png")


def particles():
    n = 128
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    d = np.sqrt((xx - n / 2 + 0.5) ** 2 + (yy - n / 2 + 0.5) ** 2) / (n / 2)
    soft = np.clip(1 - d, 0, 1) ** 2.2
    rgba = np.stack([np.ones_like(d), np.ones_like(d), np.ones_like(d), soft], -1)
    save(rgba, "ui_softdot.png")
    streak = np.clip(1 - np.abs(xx - n / 2) / 3.0, 0, 1) * np.clip(np.sin(yy / n * math.pi), 0, 1)
    save(np.stack([np.ones_like(d)] * 3 + [streak * 0.8], -1), "ui_raindrop.png")


# ---------------------------------------------------------------- UI icons (white on transparent)
def icons():
    S = 4; N = 128 * S
    def new():
        im = Image.new("RGBA", (N, N), (255, 255, 255, 0)); return im, ImageDraw.Draw(im)
    def done(im, name, size=128):
        im.resize((size, size), Image.LANCZOS).save(os.path.join(OUT, name)); print("wrote", name)
    W = (255, 255, 255, 255); c = N / 2; lw = int(N * 0.055)

    im, d = new(); d.ellipse([lw, lw, N - lw, N - lw], outline=W, width=lw); done(im, "ui_ring.png", 256)
    im, d = new(); d.ellipse([0, 0, N - 1, N - 1], fill=W); done(im, "ui_circle.png", 256)
    # rounded rect for 9-slice (radius 32 of 128)
    im, d = new(); d.rounded_rectangle([0, 0, N - 1, N - 1], radius=int(N * 0.25), fill=W); done(im, "ui_round.png")
    # boost: chevrons
    im, d = new()
    for k in range(2):
        y0 = N * (0.30 + k * 0.22)
        d.line([(N * 0.25, y0 + N * 0.18), (c, y0), (N * 0.75, y0 + N * 0.18)], fill=W, width=lw, joint="curve")
    done(im, "ui_boost.png")
    # hover: bird-like wings / pause bars with dot
    im, d = new(); d.ellipse([c - N * 0.08, c - N * 0.08, c + N * 0.08, c + N * 0.08], fill=W)
    d.arc([N * 0.12, N * 0.12, N * 0.88, N * 0.88], 200, 340, fill=W, width=lw)
    d.arc([N * 0.12, N * 0.12, N * 0.88, N * 0.88], 20, 160, fill=W, width=lw); done(im, "ui_hover.png")
    # land: arrow down to line
    im, d = new(); d.line([(c, N * 0.15), (c, N * 0.62)], fill=W, width=lw)
    d.line([(N * 0.32, N * 0.45), (c, N * 0.65), (N * 0.68, N * 0.45)], fill=W, width=lw, joint="curve")
    d.line([(N * 0.2, N * 0.82), (N * 0.8, N * 0.82)], fill=W, width=lw); done(im, "ui_land.png")
    # up/down
    for name, flip in (("ui_up.png", False), ("ui_down.png", True)):
        im, d = new()
        P = [(N * 0.22, N * 0.62), (c, N * 0.34), (N * 0.78, N * 0.62)]
        if flip: P = [(x, N - y) for x, y in P]
        d.line(P, fill=W, width=int(lw * 1.2), joint="curve"); done(im, name)
    # camera switch
    im, d = new(); d.rounded_rectangle([N * 0.12, N * 0.3, N * 0.88, N * 0.78], radius=N * 0.08, outline=W, width=lw)
    d.ellipse([c - N * 0.14, N * 0.4, c + N * 0.14, N * 0.68], outline=W, width=lw)
    d.rectangle([N * 0.36, N * 0.2, N * 0.64, N * 0.3], fill=W); done(im, "ui_camera.png")
    # photo aperture
    im, d = new(); d.ellipse([N * 0.12, N * 0.12, N * 0.88, N * 0.88], outline=W, width=lw)
    for k in range(6):
        a = k * math.pi / 3
        d.line([(c + math.cos(a) * N * 0.36, c + math.sin(a) * N * 0.36), (c + math.cos(a + 1.2) * N * 0.12, c + math.sin(a + 1.2) * N * 0.12)], fill=W, width=int(lw * 0.7))
    done(im, "ui_photo.png")
    # menu
    im, d = new()
    for k in range(3):
        y = N * (0.3 + k * 0.2); d.line([(N * 0.2, y), (N * 0.8, y)], fill=W, width=lw)
    done(im, "ui_menu.png")
    # close
    im, d = new(); d.line([(N * 0.25, N * 0.25), (N * 0.75, N * 0.75)], fill=W, width=lw); d.line([(N * 0.75, N * 0.25), (N * 0.25, N * 0.75)], fill=W, width=lw); done(im, "ui_close.png")
    # map
    im, d = new(); d.line([(N * 0.15, N * 0.25), (N * 0.38, N * 0.15), (N * 0.62, N * 0.25), (N * 0.85, N * 0.15), (N * 0.85, N * 0.75), (N * 0.62, N * 0.85), (N * 0.38, N * 0.75), (N * 0.15, N * 0.85), (N * 0.15, N * 0.25)], fill=W, width=lw, joint="curve")
    d.line([(N * 0.38, N * 0.15), (N * 0.38, N * 0.75)], fill=W, width=int(lw * 0.6)); d.line([(N * 0.62, N * 0.25), (N * 0.62, N * 0.85)], fill=W, width=int(lw * 0.6)); done(im, "ui_map.png")
    # journal (book)
    im, d = new(); d.rounded_rectangle([N * 0.22, N * 0.12, N * 0.8, N * 0.88], radius=N * 0.05, outline=W, width=lw)
    d.line([(N * 0.34, N * 0.12), (N * 0.34, N * 0.88)], fill=W, width=lw); d.line([(N * 0.45, N * 0.35), (N * 0.68, N * 0.35)], fill=W, width=int(lw * 0.7)); done(im, "ui_journal.png")
    # gear
    im, d = new()
    for k in range(8):
        a = k * math.pi / 4
        d.line([(c + math.cos(a) * N * 0.22, c + math.sin(a) * N * 0.22), (c + math.cos(a) * N * 0.40, c + math.sin(a) * N * 0.40)], fill=W, width=int(lw * 1.6))
    d.ellipse([c - N * 0.27, c - N * 0.27, c + N * 0.27, c + N * 0.27], fill=W)
    d.ellipse([c - N * 0.11, c - N * 0.11, c + N * 0.11, c + N * 0.11], fill=(255, 255, 255, 0)); done(im, "ui_gear.png")
    # eye (hide UI)
    im, d = new(); d.chord([N * 0.1, N * 0.25, N * 0.9, N * 0.95], 200, 340, outline=W, width=lw)
    d.chord([N * 0.1, N * 0.05, N * 0.9, N * 0.75], 20, 160, outline=W, width=lw)
    d.ellipse([c - N * 0.1, c - N * 0.1, c + N * 0.1, c + N * 0.1], fill=W); done(im, "ui_eye.png")
    # flower/nectar drop
    im, d = new(); d.polygon([(c, N * 0.12), (N * 0.72, N * 0.55), (c, N * 0.88), (N * 0.28, N * 0.55)], fill=W)
    d.ellipse([N * 0.28, N * 0.45, N * 0.72, N * 0.88], fill=W); done(im, "ui_drop.png")
    # hummingbird logo silhouette (original drawing)
    im, d = new()
    P = lambda pts: [(x * N, y * N) for x, y in pts]
    body = smooth(P([(0.30, 0.60), (0.42, 0.50), (0.56, 0.44), (0.66, 0.38), (0.72, 0.36), (0.76, 0.40),
                     (0.74, 0.46), (0.64, 0.54), (0.52, 0.62), (0.40, 0.66)]), 12)
    d.polygon(body, fill=W)
    d.polygon(P([(0.75, 0.385), (0.98, 0.33), (0.755, 0.41)]), fill=W)                     # beak
    tail = smooth(P([(0.34, 0.60), (0.18, 0.74), (0.08, 0.86), (0.16, 0.84), (0.26, 0.76), (0.40, 0.65)]), 10)
    d.polygon(tail, fill=W)
    wing = smooth(P([(0.48, 0.50), (0.40, 0.34), (0.30, 0.18), (0.18, 0.06), (0.30, 0.10), (0.46, 0.22),
                     (0.58, 0.36), (0.58, 0.46)]), 12)
    d.polygon(wing, fill=W)
    wing2 = smooth(P([(0.52, 0.48), (0.56, 0.32), (0.62, 0.16), (0.70, 0.06), (0.70, 0.18), (0.64, 0.34), (0.60, 0.46)]), 12)
    d.polygon(wing2, fill=(255, 255, 255, 150))
    d.ellipse([0.695 * N, 0.385 * N, 0.715 * N, 0.405 * N], fill=(255, 255, 255, 0))
    done(im, "ui_logo.png", 256)


if __name__ == "__main__":
    bark(); moss(); ground(); rock(); leaves(); fern(); petal(); feathers(); hb_wing(); butterfly()
    water(); noise_rgb(); particles(); icons()
