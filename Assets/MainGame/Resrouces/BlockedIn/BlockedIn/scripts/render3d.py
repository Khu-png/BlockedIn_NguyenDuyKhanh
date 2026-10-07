"""Tiny numpy software rasterizer for mesh preview thumbnails (orthographic, z-buffer, textured, 2x supersampled).

Items are in Unity space (left-handed, y up). Textured items are drawn unlit like the game's URP/Unlit-style shaders,
untextured ones get a little Lambert shading so their shape stays readable.
"""
import numpy as np
from PIL import Image

LIGHT = np.array([0.35, 0.85, -0.4]) / np.linalg.norm([0.35, 0.85, -0.4])


def basis(elev, azim):
    el, az = np.radians(elev), np.radians(azim)
    d = np.array([np.sin(az) * np.cos(el), np.sin(el), -np.cos(az) * np.cos(el)])  # target -> camera
    r = np.cross(d, [0.0, 1.0, 0.0]); r /= np.linalg.norm(r)
    u = np.cross(r, d)
    return r, u, d


def tex_array(img):
    return None if img is None else np.asarray(img.convert("RGBA"), dtype=np.float32) / 255.0


def render(items, size=256, elev=55, azim=-30, bg=(14, 28, 66), ss=2, pad=0.08, shade_textured=0.0):
    r, u, d = basis(elev, azim)
    W = size * ss
    P_all = np.concatenate([it["V"] for it in items])
    sx, sy = P_all @ r, P_all @ u
    cx, cy = (sx.min() + sx.max()) / 2, (sy.min() + sy.max()) / 2
    span = max(sx.max() - sx.min(), sy.max() - sy.min(), 1e-6)
    scale = W * (1 - 2 * pad) / span
    img = np.zeros((W, W, 3), np.float32); img[:] = np.array(bg, np.float32) / 255
    zbuf = np.full((W, W), -np.inf, np.float32)
    for it in sorted(items, key=lambda it: bool(it.get("blend"))):
        V = it["V"]
        X = (V @ r - cx) * scale + W / 2
        Y = W / 2 - (V @ u - cy) * scale
        Z = V @ d
        UV, tex = it.get("UV"), it.get("tex")
        col = np.array(it.get("color", (1, 1, 1, 1)), np.float32)
        th, tw = (tex.shape[0], tex.shape[1]) if tex is not None else (0, 0)
        for a, b, c in it["tris"]:
            x0, x1, x2, y0, y1, y2 = X[a], X[b], X[c], Y[a], Y[b], Y[c]
            area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0)
            if abs(area) < 1e-9:
                continue
            xa, xb = max(int(np.floor(min(x0, x1, x2))), 0), min(int(np.ceil(max(x0, x1, x2))), W - 1)
            ya, yb = max(int(np.floor(min(y0, y1, y2))), 0), min(int(np.ceil(max(y0, y1, y2))), W - 1)
            if xa > xb or ya > yb:
                continue
            gx, gy = np.meshgrid(np.arange(xa, xb + 1) + 0.5, np.arange(ya, yb + 1) + 0.5)
            w0 = ((x1 - gx) * (y2 - gy) - (x2 - gx) * (y1 - gy)) / area
            w1 = ((x2 - gx) * (y0 - gy) - (x0 - gx) * (y2 - gy)) / area
            w2 = 1 - w0 - w1
            inside = (w0 >= -1e-6) & (w1 >= -1e-6) & (w2 >= -1e-6)
            if not inside.any():
                continue
            z = w0 * Z[a] + w1 * Z[b] + w2 * Z[c]
            zb = zbuf[ya:yb + 1, xa:xb + 1]
            m = inside & (z > zb + 1e-5)
            if not m.any():
                continue
            if tex is not None and UV is not None:
                uu = (w0 * UV[a, 0] + w1 * UV[b, 0] + w2 * UV[c, 0])[m] % 1.0
                vv = (w0 * UV[a, 1] + w1 * UV[b, 1] + w2 * UV[c, 1])[m] % 1.0
                px = tex[np.clip(((1 - vv) * th).astype(int), 0, th - 1), np.clip((uu * tw).astype(int), 0, tw - 1)] * col
                shade = shade_textured
            else:
                px = np.broadcast_to(col, (int(m.sum()), 4)).copy()
                shade = 1.0
            if shade:
                n = np.cross(V[b] - V[a], V[c] - V[a]); n /= max(np.linalg.norm(n), 1e-12)
                lam = 0.5 + 0.5 * abs(float(n @ LIGHT))
                px[:, :3] *= (1 - shade) + shade * lam
            region = img[ya:yb + 1, xa:xb + 1]
            if it.get("blend"):
                al = px[:, 3:4]
                region[m] = region[m] * (1 - al) + px[:, :3] * al
            else:
                region[m] = px[:, :3]
                zb[m] = z[m]
    out = Image.fromarray(np.clip(img * 255, 0, 255).astype(np.uint8))
    return out.resize((size, size), Image.LANCZOS) if ss > 1 else out
