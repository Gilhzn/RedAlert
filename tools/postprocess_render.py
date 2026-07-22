#!/usr/bin/env python3
# Post-process the Blender render: additive bloom on bright/glowing pixels,
# warm-highlight / cool-shadow dusk grade, saturation lift, and a soft vignette.
import sys, numpy as np
from PIL import Image, ImageFilter

src = sys.argv[1] if len(sys.argv) > 1 else "map_render.png"
dst = sys.argv[2] if len(sys.argv) > 2 else src.replace(".png", "_final.png")

img = Image.open(src).convert("RGB")
W, H = img.size
a = np.asarray(img).astype(np.float32) / 255.0

# ---- 1. bloom: isolate bright pixels, blur at several radii, add back --------
lum = a @ np.array([0.2126, 0.7152, 0.0722], np.float32)
thresh = 0.80
mask = np.clip((lum - thresh) / (1 - thresh), 0, 1)[..., None]
bright = a * mask
bright_img = Image.fromarray((np.clip(bright, 0, 1) * 255).astype(np.uint8))
bloom = np.zeros_like(a)
for radius, weight in [(4, 0.55), (10, 0.45), (22, 0.35), (44, 0.28)]:
    b = np.asarray(bright_img.filter(ImageFilter.GaussianBlur(radius))).astype(np.float32) / 255.0
    bloom += b * weight
a = 1.0 - (1.0 - a) * (1.0 - np.clip(bloom * 0.68, 0, 1))   # screen blend

# ---- 2. dusk grade: warm highlights, cool shadows (teal-orange) --------------
l = (a @ np.array([0.2126, 0.7152, 0.0722], np.float32))[..., None]
warm = np.array([1.09, 1.02, 0.90], np.float32)     # multiply highlights
cool = np.array([0.96, 1.00, 1.08], np.float32)     # tint shadows
grade = cool * (1 - l) + warm * l
a = np.clip(a * grade, 0, 1)

# ---- 3. saturation + gentle contrast S-curve --------------------------------
gray = (a @ np.array([0.2126, 0.7152, 0.0722], np.float32))[..., None]
a = np.clip(gray + (a - gray) * 1.16, 0, 1)          # +16% saturation
a = np.clip((a - 0.5) * 1.07 + 0.5, 0, 1)            # subtle contrast

# ---- 4. vignette -------------------------------------------------------------
yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
cx, cy = W / 2, H / 2
r = np.sqrt(((xx - cx) / (W * 0.62)) ** 2 + ((yy - cy) / (H * 0.62)) ** 2)
vig = np.clip(1.0 - 0.32 * np.clip(r - 0.55, 0, 1) ** 2, 0, 1)[..., None]
a = a * vig

out = Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8))
out.save(dst)
print("post ->", dst, out.size)
