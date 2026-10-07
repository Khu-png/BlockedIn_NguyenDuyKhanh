"""Draw a top-down preview of every level (data/levels/builtin + local) and add a mechanics column to the index.

Board: cols x rows, cell (col, row) with row 0 drawn at the top. Shapes are coloured with the middle of their
ColorRamp and carry the colour's symbol; 'blocker' uses the Obstacle palette entry. Overlays: arrows = movement
restricted to one axis, frosted cells + number = iceCount, coloured stripes = rope locks (lockColor), key badge =
keyColor, dark slab = obstacle/pinned. The right column lists the orders (target shapes to assemble).
usage: python scripts/render_levels.py
"""
import os, csv, json, glob
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IMG = os.path.join(ROOT, "images", "game")
OUT = os.path.join(ROOT, "images", "levels")
LV = os.path.join(ROOT, "data", "levels")
CELL = 44
COLORS = ["green", "yellow", "red", "blue", "forestGreen", "purple", "orange", "turquoise", "pink", "black", "white"]


def font(sz, bold=False):
    try:
        return ImageFont.truetype("arialbd.ttf" if bold else "arial.ttf", sz)
    except Exception:
        return ImageFont.load_default()


F, FB, FS = font(14), font(18, True), font(11)


def ramp_colors(name):
    a = np.asarray(Image.open(os.path.join(IMG, "blocks", f"ColorRamp_{name}.png")).convert("RGB"))[4]
    pick = lambda t: tuple(int(x) for x in a[int(t * (len(a) - 1))])
    return pick(0.55), pick(0.12), pick(0.9)


PAL = {}
for c in COLORS + ["blocker"]:
    key = "Obstacle" if c == "blocker" else c[0].upper() + c[1:]
    fill, edge, light = ramp_colors(key)
    sym = Image.open(os.path.join(IMG, "blocks", f"Symbol_{key}.png")).convert("RGBA")
    PAL[c] = dict(fill=fill, edge=edge, light=light, sym=sym)
ARROW = {"horizontal": Image.open(os.path.join(IMG, "blocks", "Movement_Double_Horizontal.png")).convert("RGBA"),
         "vertical": Image.open(os.path.join(IMG, "blocks", "Movement_Double_Vertical.png")).convert("RGBA")}
TILE_A, TILE_B, WALL, BG = (13, 30, 76), (12, 33, 90), (120, 132, 160), (9, 18, 44)


def paste_fit(img, im, box):
    x0, y0, x1, y1 = box
    im = im.copy()
    im.thumbnail((x1 - x0, y1 - y0))
    img.paste(im, (x0 + (x1 - x0 - im.width) // 2, y0 + (y1 - y0 - im.height) // 2), im)


def mechanics(lay):
    m = set()
    for s in lay["shapes"]:
        if s.get("color") == "blocker": m.add("blocker")
        if s.get("movement"): m.add("arrow")
        if s.get("iceCount"): m.add("ice")
        if s.get("lockColor"): m.add("rope")
        if s.get("keyColor"): m.add("key")
        if s.get("obstacle") or s.get("pinned"): m.add("stationary")
    if lay["walls"]: m.add("walls")
    if lay.get("timerDisabled"): m.add("no_timer")
    return sorted(m)


def draw_level(lay, title):
    cols, rows = lay["cols"], lay["rows"]
    bw, bh = cols * CELL, rows * CELL
    orders = lay.get("orders", [])
    oc = 14
    ow = max([max((max(c[0] for c in p.get("cells", [[0, 0]])) + p["dc"] + 1) for p in o["parts"]) * oc for o in orders] + [80]) + 20
    W = 20 + bw + 20 + ow + 10
    H = max(50 + bh + 20, 50 + sum((max((max(c[1] for c in p.get("cells", [[0, 0]])) + p["dr"] + 1) for p in o["parts"]) * oc + 22) for o in orders) + 10)
    img = Image.new("RGB", (W, H), BG)
    dr = ImageDraw.Draw(img, "RGBA")
    dr.text((20, 12), title, fill=(255, 255, 255), font=FB)
    ox, oy = 20, 50
    for c in range(cols):
        for r in range(rows):
            dr.rectangle([ox + c * CELL, oy + r * CELL, ox + (c + 1) * CELL - 1, oy + (r + 1) * CELL - 1],
                         fill=TILE_A if (c + r) % 2 else TILE_B)
    for c, r in lay["walls"]:
        dr.rounded_rectangle([ox + c * CELL + 1, oy + r * CELL + 1, ox + (c + 1) * CELL - 2, oy + (r + 1) * CELL - 2], 6, fill=WALL)
    for s in lay["shapes"]:
        p = PAL.get(s["color"], PAL["blocker"])
        cells = {(s["col"] + dc, s["row"] + drr) for dc, drr in s["cells"]}
        stationary = s.get("obstacle") or s.get("pinned")
        fill = (70, 78, 96) if stationary else p["fill"]
        for (c, r) in cells:
            x0, y0 = ox + c * CELL, oy + r * CELL
            # extend into neighbours of the same shape so multi-cell shapes read as one piece
            l = 0 if (c - 1, r) in cells else 4
            rr = 0 if (c + 1, r) in cells else 4
            t = 0 if (c, r - 1) in cells else 4
            b = 0 if (c, r + 1) in cells else 4
            dr.rectangle([x0 + l, y0 + t, x0 + CELL - 1 - rr, y0 + CELL - 1 - b], fill=fill)
            if l: dr.line([x0 + l, y0 + t, x0 + l, y0 + CELL - 1 - b], fill=p["edge"], width=2)
            if rr: dr.line([x0 + CELL - 1 - rr, y0 + t, x0 + CELL - 1 - rr, y0 + CELL - 1 - b], fill=p["edge"], width=2)
            if t: dr.line([x0 + l, y0 + t, x0 + CELL - 1 - rr, y0 + t], fill=p["edge"], width=2)
            if b: dr.line([x0 + l, y0 + CELL - 1 - b, x0 + CELL - 1 - rr, y0 + CELL - 1 - b], fill=p["edge"], width=2)
            if s.get("iceCount"):
                dr.rectangle([x0 + l, y0 + t, x0 + CELL - 1 - rr, y0 + CELL - 1 - b], fill=(210, 240, 255, 120))
        # anchor cell for overlays: the first listed cell
        ac, ar = s["col"] + s["cells"][0][0], s["row"] + s["cells"][0][1]
        ax, ay = ox + ac * CELL, oy + ar * CELL
        paste_fit(img, p["sym"], (ax + 10, ay + 10, ax + CELL - 10, ay + CELL - 10))
        if s.get("movement") in ARROW:
            paste_fit(img, ARROW[s["movement"]], (ax + CELL - 20, ay + 2, ax + CELL - 2, ay + 20))
        if s.get("iceCount"):
            dr.text((ax + 3, ay + 1), f"{s['iceCount']}", fill=(0, 60, 120), font=FB)
        for i, lc in enumerate(s.get("lockColor") or []):
            lp = PAL.get(lc, PAL["blocker"])
            yy = ay + CELL - 9 - i * 6
            dr.line([ax + 3, yy, ax + CELL - 4, yy], fill=lp["fill"], width=4)
            dr.line([ax + 3, yy, ax + CELL - 4, yy], fill=lp["edge"], width=1)
        for i, kc in enumerate(s.get("keyColor") or []):
            kp = PAL.get(kc, PAL["blocker"])
            cx, cy = ax + 9 + i * 12, ay + CELL - 10
            dr.ellipse([cx - 6, cy - 6, cx + 6, cy + 6], fill=kp["fill"], outline=(255, 255, 255), width=2)
            dr.text((cx - 3, cy - 7), "K", fill=(255, 255, 255), font=FS)
        if stationary:
            dr.text((ax + 3, ay + 1), "pin", fill=(230, 230, 240), font=FS)
    # orders
    x = ox + bw + 20
    y = oy
    dr.text((x, 30), f"orders ({len(orders)})", fill=(200, 210, 235), font=F)
    for o in orders:
        hmax = 0
        for prt in o["parts"]:
            pp = PAL.get(prt.get("color"), PAL["blocker"])
            for dc, drr in prt.get("cells", [[0, 0]]):
                cx, cy = x + (prt["dc"] + dc) * oc, y + (prt["dr"] + drr) * oc
                dr.rectangle([cx, cy, cx + oc - 2, cy + oc - 2], fill=pp["fill"], outline=pp["edge"])
                hmax = max(hmax, (prt["dr"] + drr + 1) * oc)
        y += hmax + 22
    return img


def main():
    os.makedirs(os.path.join(OUT, "builtin"), exist_ok=True)
    os.makedirs(os.path.join(OUT, "local"), exist_ok=True)
    idx_path = os.path.join(LV, "levels_index.csv")
    rows = list(csv.DictReader(open(idx_path, encoding="utf-8")))
    thumbs = {"builtin": [], "local": []}
    for r in rows:
        rec = json.load(open(os.path.join(ROOT, "data", r["file"]), encoding="utf-8"))
        lay = rec["data"]["layout_text_json"] if "data" in rec else rec
        mech = mechanics(lay)
        r["mechanics"] = " ".join(mech)
        name = os.path.splitext(os.path.basename(r["file"]))[0]
        title = (f"Level {int(r['number'])}" if r["kind"] == "builtin" else name) + \
                f"  {lay['cols']}x{lay['rows']}  {lay['time']}s" + (f"  [{', '.join(mech)}]" if mech else "")
        im = draw_level(lay, title)
        im.save(os.path.join(OUT, r["kind"], name + ".png"), optimize=True)
        thumbs[r["kind"]].append((name, im))
    with open(idx_path, "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)
    # contact sheets: 25 per sheet
    for kind, items in thumbs.items():
        for k in range(0, len(items), 25):
            chunk = items[k:k + 25]
            cw, ch, cols = 300, 300, 5
            sheet = Image.new("RGB", (cols * cw, ((len(chunk) + cols - 1) // cols) * ch), BG)
            for i, (n, im) in enumerate(chunk):
                t = im.copy(); t.thumbnail((cw - 8, ch - 8))
                sheet.paste(t, ((i % cols) * cw + 4, (i // cols) * ch + 4))
            sheet.save(os.path.join(OUT, f"contact_sheet_{kind}_{k + 1:03d}-{k + len(chunk):03d}.png"), optimize=True)
    print({k: len(v) for k, v in thumbs.items()})


if __name__ == "__main__":
    main()
