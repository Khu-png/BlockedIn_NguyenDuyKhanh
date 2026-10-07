"""Extract images, 3D models, fonts and data from Blocked In (Tripledot, Unity 6000.3.11f1, IL2CPP).

Input : extracted/apk/assets/bin/Data (4 built-in scenes, sharedassets*, Resources-style GUID files)
        extracted/apk/assets/aa/Android (2 local Addressables bundles, which carry type trees)
Output: images/, models/, fonts/, data/ under D:\\ExtractApk\\BlockedIn
Sounds are done by D:\\ExtractApk\\_sound_tools\\extract_sounds.py BlockedIn (FSB5 -> Ogg Vorbis).
usage: python scripts/extract_assets.py
"""
import os, sys, io, csv, json, re, hashlib, shutil, collections, traceback
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from glb import GLB, to_gltf_space, to_gltf_trs, write_obj
from render3d import render, tex_array

ROOT = os.path.dirname(HERE)
DATA = os.path.join(ROOT, "extracted", "apk", "assets", "bin", "Data")
AA = os.path.join(ROOT, "extracted", "apk", "assets", "aa", "Android")
OUT_IMG, OUT_MOD, OUT_FONT, OUT_DATA = (os.path.join(ROOT, d) for d in ("images", "models", "fonts", "data"))

GAME_FILES = {"sharedassets0.assets", "sharedassets1.assets", "sharedassets2.assets", "sharedassets3.assets",
              "level0", "level1", "level2", "level3", "aa:app_assets_all", "aa:gameplay_assets_all"}
# debug tools bundled in the Lobby scene (IngameDebugConsole, UnityDebugSheet)
DEBUG_TEX = re.compile(r"^(tex_ui|tex_uds|sactx-|IconInfoHighRes$|Tools_Icon$|InputFieldBackground$)")
GAME_CATEGORIES = [
    ("branding", r"^(Blocked-In_Logo|Splash_|Logo_)"),
    ("fonts", r" Atlas$"),
    ("blocks", r"^(Block_|ColorRamp_|Symbol_|StationaryBlock_|Movement_|Shadow$|SnapLine$|Glow_Square$)"),
    ("ice", r"^(Ice|IceBlock|scratchesnormal$|Frost_Gradient$|Snow$)"),
    ("rope_scissors", r"^(Rope|Scissors)"),
    ("powerups", r"^(PowerUp|Powerup|Hammer$)"),
    ("mechanic_icons", r"^Icon_Mechanic_"),
    ("vfx", r"^(Glow|Sparkle|Particle|Smoke|Explosion|FX_|Circle_|Soft$)"),
    ("rewards", r"^Reward_"),
]


def safe(name):
    return re.sub(r'[<>:"/\\|?*\x00-\x1f]', "_", name).strip() or "unnamed"


def source_of(obj):
    f = obj.assets_file
    if f.name.startswith("CAB-"):
        b = os.path.basename(str(getattr(f.parent, "name", "") or ""))
        m = re.match(r"(.+?)_[0-9a-f]{32}\.bundle$", b)
        return "aa:" + (m.group(1) if m else b)
    return f.name


def reset(d):
    if os.path.isdir(d):
        shutil.rmtree(d)
    os.makedirs(d)


print("loading ...")
env = UnityPy.load(DATA, AA)
OBJS = list(env.objects)


def serialized_files():
    out = {}
    def walk(f):
        for k, v in (getattr(f, "files", None) or {}).items():
            if hasattr(v, "objects") and isinstance(getattr(v, "objects"), dict):
                out[os.path.basename(k).lower()] = v
            walk(v)
    for k, v in env.files.items():
        if hasattr(v, "objects") and isinstance(getattr(v, "objects"), dict):
            out[os.path.basename(k).lower()] = v
        walk(v)
    return out


SFILES = serialized_files()


def resolve(af, fid, pid):
    """PPtr (file id, path id) seen from serialized file af -> ObjectReader or None."""
    if pid == 0:
        return None
    if fid == 0:
        tgt = af
    else:
        try:
            tgt = SFILES.get(os.path.basename(af.externals[fid - 1].path).lower())
        except Exception:
            tgt = None
    return tgt.objects.get(pid) if tgt is not None else None


def obj_name(o):
    try:
        return o.read().m_Name
    except Exception:
        return "?"


# ---------------------------------------------------------------- textures
tex_cache = {}


def tex_image(t_reader):
    k = (t_reader.assets_file.name, t_reader.path_id)
    if k not in tex_cache:
        tex_cache[k] = t_reader.read().image.convert("RGBA")
    return tex_cache[k]


def tex_by_name(name):
    for o in OBJS:
        if o.type.name == "Texture2D" and obj_name(o) == name:
            return tex_image(o)
    raise KeyError(name)


def export_textures():
    # images/levels is written by render_levels.py: leave it alone
    for sub in ("game", "engine_sdk", "sprites"):
        reset(os.path.join(OUT_IMG, sub))
    rows, seen, used, errors = [], {}, collections.Counter(), []
    for o in OBJS:
        if o.type.name != "Texture2D":
            continue
        try:
            t = o.read()
            if t.m_Width == 0 or t.m_Height == 0:
                continue
            im = tex_image(o)
        except Exception as e:
            errors.append((obj_name(o), repr(e)))
            continue
        src = source_of(o)
        h = hashlib.md5(im.tobytes() + str(im.size).encode()).hexdigest()
        if h in seen:
            seen[h]["also_in"].add(src)
            continue
        name = t.m_Name
        if src in GAME_FILES and not DEBUG_TEX.match(name):
            sub = next((c for c, rx in GAME_CATEGORIES if re.search(rx, name)), "ui")
            folder = os.path.join("game", sub)
        else:
            sub = ("unity_builtin" if src in ("unity default resources", "unity_builtin_extra") or src.startswith("aa:35b7")
                   else "render_pipeline" if src == "globalgamemanagers.assets"
                   else "debug_tools" if src in GAME_FILES else "sdk_and_tools")
            folder = os.path.join("engine_sdk", sub)
        base = safe(name)
        used[(folder, base.lower())] += 1
        if used[(folder, base.lower())] > 1:
            base = f"{base}__{used[(folder, base.lower())]}"
        rel = os.path.join(folder, base + ".png")
        os.makedirs(os.path.join(OUT_IMG, folder), exist_ok=True)
        im.save(os.path.join(OUT_IMG, rel), optimize=True)
        fmt = str(t.m_TextureFormat).split(".")[-1]
        row = dict(file=rel.replace("\\", "/"), name=name, width=im.width, height=im.height, format=fmt, source=src,
                   also_in=set())
        seen[h] = row
        rows.append(row)
    # launcher icon from the Android resources (not a Unity asset)
    import zipfile
    apk = zipfile.ZipFile(os.path.join(os.path.dirname(ROOT), "Blocked In.apk"))
    for n in ("app_icon", "app_icon_round", "ic_launcher_background", "ic_launcher_foreground"):
        im = Image.open(io.BytesIO(apk.read(f"res/mipmap-xxxhdpi-v4/{n}.png"))).convert("RGBA")
        rel = f"game/branding/{n}.png"
        im.save(os.path.join(OUT_IMG, rel), optimize=True)
        rows.append(dict(file=rel, name=n, width=im.width, height=im.height, format="PNG",
                         source="res/mipmap-xxxhdpi-v4", also_in=set()))
    with open(os.path.join(OUT_IMG, "images_index.csv"), "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, ["file", "name", "width", "height", "format", "source", "also_in"])
        w.writeheader()
        for r in sorted(rows, key=lambda r: r["file"].lower()):
            w.writerow({**r, "also_in": ";".join(sorted(r["also_in"]))})
    print(f"textures: {len(rows)} unique PNG, errors {len(errors)}", errors[:5])
    return rows


def image_sheets():
    """One contact sheet per game image category (checkerboard behind transparency)."""
    try:
        font = ImageFont.truetype("arial.ttf", 12)
    except Exception:
        font = ImageFont.load_default()
    cell, cols = 132, 10
    gdir = os.path.join(OUT_IMG, "game")
    for sub in sorted(os.listdir(gdir)):
        files = sorted(f for f in os.listdir(os.path.join(gdir, sub)) if f.endswith(".png"))
        rows = (len(files) + cols - 1) // cols
        sheet = Image.new("RGB", (cols * cell, rows * (cell + 18)), (24, 28, 40))
        dr = ImageDraw.Draw(sheet)
        chk = Image.new("RGB", (cell - 8, cell - 8), (200, 200, 200))
        cd = ImageDraw.Draw(chk)
        for yy in range(0, cell, 8):
            for xx in range(0, cell, 8):
                if (xx // 8 + yy // 8) % 2:
                    cd.rectangle([xx, yy, xx + 7, yy + 7], fill=(160, 160, 160))
        for i, f in enumerate(files):
            im = Image.open(os.path.join(gdir, sub, f)).convert("RGBA")
            im.thumbnail((cell - 8, cell - 8))
            x, y = (i % cols) * cell, (i // cols) * (cell + 18)
            bg = chk.copy()
            bg.paste(im, ((bg.width - im.width) // 2, (bg.height - im.height) // 2), im)
            sheet.paste(bg, (x + 4, y + 4))
            dr.text((x + 4, y + cell), f[:-4][:20], fill=(225, 230, 240), font=font)
        sheet.save(os.path.join(OUT_IMG, f"contact_sheet_{sub}.png"), optimize=True)


def export_sprites():
    """Only sprites that are a sub-rectangle of a texture (atlas packed or trimmed); whole-texture sprites are
    already in images/ as the texture itself."""
    d = os.path.join(OUT_IMG, "sprites")
    os.makedirs(d, exist_ok=True)
    seen, n = set(), 0
    for o in OBJS:
        if o.type.name != "Sprite":
            continue
        s = o.read()
        try:
            whole = False
            if s.m_RD.texture.path_id:
                t = s.m_RD.texture.read()
                whole = abs(s.m_Rect.width - t.m_Width) <= 1 and abs(s.m_Rect.height - t.m_Height) <= 1
            if whole:
                continue
            im = s.image
        except Exception as e:
            print("  sprite fail", s.m_Name, e)
            continue
        h = hashlib.md5(im.tobytes()).hexdigest()
        if h in seen:
            continue
        seen.add(h)
        im.save(os.path.join(d, safe(s.m_Name) + ".png"))
        n += 1
    print(f"sprites cut from atlases: {n}")


# ---------------------------------------------------------------- scriptable objects (bundles carry type trees)
def walk_pptr(d, path=""):
    if isinstance(d, dict):
        if set(d.keys()) == {"m_FileID", "m_PathID"}:
            yield path, d
        else:
            for k, v in d.items():
                yield from walk_pptr(v, f"{path}.{k}" if path else k)
    elif isinstance(d, list):
        for i, v in enumerate(d):
            yield from walk_pptr(v, f"{path}[{i}]")


def annotate(d, af):
    if isinstance(d, dict):
        if set(d.keys()) == {"m_FileID", "m_PathID"}:
            t = resolve(af, d["m_FileID"], d["m_PathID"])
            if t is not None:
                return {**d, "_ref": f"{t.type.name}:{obj_name(t)}"}
            return d
        return {k: annotate(v, af) for k, v in d.items()}
    if isinstance(d, list):
        return [annotate(v, af) for v in d]
    return d


SO = {}  # class -> list of (name, typetree, reader)
for o in OBJS:
    if o.type.name != "MonoBehaviour" or not o.assets_file.name.startswith("CAB-"):
        continue
    try:
        tt = o.read_typetree()
        if tt.get("m_GameObject", {}).get("m_PathID", 1) != 0:
            continue
        cls = o.read().m_Script.read().m_ClassName
    except Exception:
        continue
    SO.setdefault(cls, []).append((tt.get("m_Name", ""), tt, o))


def so_one(cls):
    return SO[cls][0]


def ref_name(o, field_value):
    t = resolve(o.assets_file, field_value["m_FileID"], field_value["m_PathID"])
    return None if t is None else obj_name(t)


def export_scriptable_objects():
    d = os.path.join(OUT_DATA, "scriptable_objects")
    reset(d)
    seen, n = set(), 0
    for cls, items in SO.items():
        for name, tt, o in items:
            # TMP font assets hold ~10 MB glyph tables each: keep them compact
            js = json.dumps(annotate(tt, o.assets_file), indent=None if cls == "TMP_FontAsset" else 1, ensure_ascii=False, default=str)
            key = (cls, name, hashlib.md5(js.encode()).hexdigest())
            if key in seen:
                continue
            seen.add(key)
            fn = safe(f"{cls}__{name}" if name else cls)
            p = os.path.join(d, fn + ".json")
            k = 2
            while os.path.exists(p):
                p = os.path.join(d, f"{fn}__{k}.json"); k += 1
            with open(p, "w", encoding="utf-8") as fh:
                fh.write(js)
            n += 1
    # audio event table
    rows = []
    for name, tt, o in SO.get("AudioEntry", []):
        clips = [ref_name(o, c) for c in tt.get("clips", [])]
        mg = ref_name(o, tt["mixerGroup"]) if tt.get("mixerGroup") else ""
        rows.append(dict(entry=name, event_id=tt.get("id"), clips=";".join(c for c in clips if c), loop=tt.get("loop"),
                         volume=round(tt.get("volume", 1), 3), pitch_variation=tt.get("pitchVariation"), mixer_group=mg))
    rows = list({r["entry"]: r for r in rows}.values())
    with open(os.path.join(OUT_DATA, "audio_events.csv"), "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, list(rows[0].keys()))
        w.writeheader()
        w.writerows(sorted(rows, key=lambda r: r["entry"]))
    print(f"scriptable objects: {n} json, audio events {len(rows)}")


# ---------------------------------------------------------------- text assets, fonts, animations, shaders
def export_text_and_fonts():
    lv_local, lv_pack, cfg = (os.path.join(OUT_DATA, p) for p in ("levels/local", "levels/builtin", "config"))
    packs = os.path.join(OUT_DATA, "levels", "packs")
    for p in (lv_local, lv_pack, cfg, packs):
        reset(p)
    reset(OUT_FONT)
    local_ids = {}
    for name, tt, o in SO.get("LocalLevelConfig", []):
        local_ids[ref_name(o, tt["_levelJson"])] = tt.get("_id")
    done, levels_index = set(), []
    for o in OBJS:
        if o.type.name == "TextAsset":
            t = o.read()
            raw = t.m_Script.encode("utf-8", "surrogateescape") if isinstance(t.m_Script, str) else bytes(t.m_Script)
            if t.m_Name in done:
                continue
            done.add(t.m_Name)
            if t.m_Name.startswith("levelPack_"):
                with open(os.path.join(packs, t.m_Name + ".jsonl"), "wb") as fh:
                    fh.write(raw)
                for line in raw.decode("utf-8").splitlines():
                    if line.strip():
                        rec = json.loads(line)
                        lay = rec["data"]["layout_text_json"]
                        fn = f"level_{rec['number']:03d}.json"
                        with open(os.path.join(lv_pack, fn), "w", encoding="utf-8") as fh:
                            json.dump(rec, fh, indent=1)
                        levels_index.append(dict(kind="builtin", number=rec["number"], file=f"levels/builtin/{fn}",
                                                 cols=lay["cols"], rows=lay["rows"], time=lay["time"],
                                                 shapes=len(lay["shapes"]), orders=len(lay["orders"]),
                                                 walls=len(lay["walls"]), layout_id=rec["level_layout_id"],
                                                 pack=t.m_Name[:20]))
            elif t.m_Name.startswith("Level_"):
                lay = json.loads(raw.decode("utf-8"))
                fn = t.m_Name + ".json"
                with open(os.path.join(lv_local, fn), "w", encoding="utf-8") as fh:
                    json.dump(lay, fh, indent=1)
                levels_index.append(dict(kind="local", number=local_ids.get(t.m_Name), file=f"levels/local/{fn}",
                                         cols=lay["cols"], rows=lay["rows"], time=lay["time"],
                                         shapes=len(lay["shapes"]), orders=len(lay["orders"]),
                                         walls=len(lay["walls"]), layout_id="", pack=""))
            else:
                try:
                    obj = json.loads(raw.decode("utf-8"))
                    with open(os.path.join(cfg, safe(t.m_Name) + ".json"), "w", encoding="utf-8") as fh:
                        json.dump(obj, fh, indent=1, ensure_ascii=False)
                except Exception:
                    with open(os.path.join(cfg, safe(t.m_Name) + ".txt"), "wb") as fh:
                        fh.write(raw)
        elif o.type.name == "Font":
            f = o.read()
            data = bytes(f.m_FontData) if f.m_FontData else b""
            if not data:
                continue
            ext = ".otf" if data[:4] == b"OTTO" else ".ttf"
            p = os.path.join(OUT_FONT, safe(f.m_Name) + ext)
            if not os.path.exists(p):
                with open(p, "wb") as fh:
                    fh.write(data)
    levels_index.sort(key=lambda r: (r["kind"], r["number"] or 0))
    with open(os.path.join(OUT_DATA, "levels", "levels_index.csv"), "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, list(levels_index[0].keys()))
        w.writeheader()
        w.writerows(levels_index)
    print(f"levels: {sum(r['kind'] == 'builtin' for r in levels_index)} builtin + {sum(r['kind'] == 'local' for r in levels_index)} local; fonts {len(os.listdir(OUT_FONT))}")


def export_animations_and_shaders():
    d = os.path.join(OUT_DATA, "animations")
    reset(d)
    seen, n = set(), 0
    for o in OBJS:
        if o.type.name != "AnimationClip":
            continue
        try:
            tt = o.read_typetree()
        except Exception:
            continue
        js = json.dumps(tt, indent=1, default=str)
        h = hashlib.md5(js.encode()).hexdigest()
        if h in seen:
            continue
        seen.add(h)
        p = os.path.join(d, safe(tt.get("m_Name", "clip")) + ".json")
        k = 2
        while os.path.exists(p):
            p = os.path.join(d, safe(tt.get("m_Name", "clip")) + f"__{k}.json"); k += 1
        with open(p, "w", encoding="utf-8") as fh:
            fh.write(js)
        n += 1
    names = set()
    for o in OBJS:
        if o.type.name == "Shader":
            try:
                names.add(o.read().m_ParsedForm.m_Name)
            except Exception:
                pass
    with open(os.path.join(OUT_DATA, "shaders.txt"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(sorted(names)) + "\n")
    print(f"animation clips: {n}; shaders: {len(names)}")


# ---------------------------------------------------------------- 3D: materials (baked approximations of custom shaders)
def ramp_row(ramp):
    a = np.asarray(ramp.convert("RGB"), np.float32)
    return a[a.shape[0] // 2]


def bake_ramp(gray_img, ramp, alpha=None):
    g = np.asarray(gray_img.convert("RGBA"), np.float32)[..., 0] / 255.0
    row = ramp_row(ramp)
    x = g * (len(row) - 1)
    i0 = np.floor(x).astype(int); i1 = np.minimum(i0 + 1, len(row) - 1); f = (x - i0)[..., None]
    rgb = row[i0] * (1 - f) + row[i1] * f
    a = np.full(g.shape + (1,), 255.0) if alpha is None else alpha[..., None]
    return Image.fromarray(np.concatenate([rgb, a], -1).clip(0, 255).astype(np.uint8), "RGBA")


def col4(c):
    return np.array([c["r"], c["g"], c["b"], c.get("a", 1.0)], np.float32) if isinstance(c, dict) else np.array(c, np.float32)


def bake_rope(tex, bright, dark, highlight):
    a = np.asarray(tex.convert("RGB"), np.float32) / 255.0
    r = (a[..., 0] - a[..., 0].min()) / max(np.ptp(a[..., 0]), 1e-6)
    g = a[..., 1] / max(a[..., 1].max(), 1e-6)
    rgb = dark[:3] * (1 - r[..., None]) + bright[:3] * r[..., None]
    rgb = rgb + highlight[:3] * g[..., None] * 0.6
    return Image.fromarray(np.concatenate([rgb.clip(0, 1) * 255, np.full(r.shape + (1,), 255.0)], -1).astype(np.uint8))


def bake_ice(tex, base):
    a = np.asarray(tex.convert("RGB"), np.float32) / 255.0
    rgb = base[:3] * (0.45 + 0.55 * a[..., 0:1])
    rgb = rgb + (1 - rgb) * a[..., 1:2]
    alpha = np.clip(base[3] * 0.8 + 0.6 * a[..., 1], 0, 1)
    return Image.fromarray(np.concatenate([rgb * 255, alpha[..., None] * 255], -1).clip(0, 255).astype(np.uint8))


MATS = {}
for o in OBJS:
    if o.type.name == "Material":
        n = obj_name(o)
        if n not in MATS:
            MATS[n] = o.read()


def mat_props(m):
    tex = {}
    for k, v in m.m_SavedProperties.m_TexEnvs:
        if v.m_Texture.path_id:
            try:
                tex[k] = v.m_Texture.read()
            except Exception:
                pass
    cols = {k: np.array([v.r, v.g, v.b, v.a], np.float32) for k, v in m.m_SavedProperties.m_Colors}
    try:
        shader = m.m_Shader.read().m_ParsedForm.m_Name
    except Exception:
        shader = "?"
    return shader, tex, cols


PALETTE = {}  # 'Green' -> dict(ramp, symbol, rope colors)
_name, _tt, _o = so_one("BlockOrderPalette")
for e in _tt["_entries"]:
    rn = ref_name(_o, e["_colorRamp"])
    PALETTE[rn.replace("ColorRamp_", "")] = dict(ramp=rn, symbol=ref_name(_o, e["_symbolTexture"]), index=e["_color"],
                                                 accent=col4(e["_accent"]), rope_bright=col4(e["_ropeBrightColor"]),
                                                 rope_dark=col4(e["_ropeDarkColor"]),
                                                 rope_highlight=col4(e["_ropeHighlightColor"]))
BAKED = {}  # name -> PIL image (saved to models/textures)


def baked(name, fn):
    if name not in BAKED:
        BAKED[name] = fn()
    return BAKED[name]


def mat_spec(mname, color=None):
    """Unity material name -> GLB material spec (custom BlockOrder shaders are baked to plain textures)."""
    m = MATS[mname]
    shader, tex, cols = mat_props(m)
    base = cols.get("_BaseColor", cols.get("_Color", np.ones(4, np.float32)))
    main = tex.get("_BaseMap") or tex.get("_MainTex")
    if mname == "PieceBlockMaterial":
        c = color or tex["_ColorRamp"].m_Name.replace("ColorRamp_", "")
        img = baked(f"Block_{c}", lambda: bake_ramp(tex_by_name("Block_GradientMap"), tex_by_name(PALETTE[c]["ramp"])))
        return dict(name=f"PieceBlock_{c}", image=img, unlit=True)
    if shader == "BlockOrder/ScissorsHandle":
        rn = tex["_ColorRamp"].m_Name
        img = baked(f"ScissorsHandle_{rn.replace('ColorRamp_', '')}", lambda: bake_ramp(tex_by_name(main.m_Name), tex_by_name(rn)))
        return dict(name=f"{mname}", image=img, unlit=True)
    if shader == "BlockOrder/BlockIce":
        img = baked("Ice_baked", lambda: bake_ice(tex_by_name(main.m_Name), base))
        return dict(name=mname, image=img, unlit=True, blend=True)
    if shader == "BlockOrder/Rope":
        p = dict(bright=cols["_BrightColor"], dark=cols["_DarkColor"], hl=cols["_HighlightColor"])
        img = baked("Rope_default", lambda: bake_rope(tex_by_name(main.m_Name), p["bright"], p["dark"], p["hl"] * 0.4))
        return dict(name=mname, image=img, unlit=True)
    if shader in ("BlockOrder/Backdrop", "BlockOrder/BoardBlock", "BlockOrder/Block_Silhouette"):
        return dict(name=mname, color=tuple(base), unlit=False)
    if main is not None:
        img = tex_image_for(main)
        blend = shader.startswith("BlockOrder/Glow") or np.asarray(img)[..., 3].min() < 250
        if shader.startswith("BlockOrder/Glow"):
            a = np.asarray(img, np.float32)
            lum = a[..., :3].max(-1) * a[..., 3] / 255.0
            img = Image.fromarray(np.dstack([np.full(lum.shape + (3,), 255.0), lum]).astype(np.uint8))
            img = baked(f"{main.m_Name}_glow", lambda: img)
        return dict(name=mname, image=img, image_key=main.m_Name, color=tuple(base) if "Unlit" in shader else (1, 1, 1, 1),
                    unlit=True, blend=blend)
    return dict(name=mname, color=tuple(base), unlit=False)


def tex_image_for(t):
    return tex_by_name(t.m_Name)


# ---------------------------------------------------------------- 3D: meshes
def mesh_geo(m):
    h = MeshHandler(m)
    h.process()
    V = np.array(h.m_Vertices, np.float64).reshape(-1, 3)
    N = np.array(h.m_Normals, np.float64)[:, :3] if h.m_Normals is not None and len(h.m_Normals) else None
    UV = np.array(h.m_UV0, np.float64)[:, :2] if h.m_UV0 is not None and len(h.m_UV0) else None
    C = np.array(h.m_Colors) if h.m_Colors is not None and len(h.m_Colors) else None
    tris = [np.array(t, np.int64).reshape(-1, 3) for t in h.get_triangles()]
    return dict(V=V, N=N, UV=UV, C=C, tris=tris)


MESHES = {}  # name -> Mesh (game meshes only; built-in primitives kept separately)
BUILTIN_MESH = {}
for o in OBJS:
    if o.type.name == "Mesh":
        m = o.read()
        (BUILTIN_MESH if o.assets_file.name == "unity default resources" else MESHES).setdefault(m.m_Name, m)

PART_SETS = {"PieceBlocksParts": "piece_block", "BoardBlocksParts": "board_wall", "BackdropParts": "board_backdrop",
             "StationaryBlocksParts": "stationary_block", "RopeParts": "rope"}


def part_sets():
    mesh_set, mesh_mats, info = {}, {}, {}
    for cls, setname in PART_SETS.items():
        name, tt, o = so_one(cls)
        fields = {}
        mats = []
        for path, ref in walk_pptr(tt):
            t = resolve(o.assets_file, ref["m_FileID"], ref["m_PathID"])
            if t is None:
                continue
            if t.type.name == "Mesh":
                fields[path] = obj_name(t)
            elif t.type.name == "Material":
                mats.append((path, obj_name(t)))
        info[cls] = dict(set=setname, meshes=fields, materials=dict(mats))
        main = [mn for p, mn in mats if "shadow" not in p.lower() and "secondary" not in p.lower()]
        sec = [mn for p, mn in mats if "secondary" in p.lower()]
        for path, mn in fields.items():
            mesh_set.setdefault(mn, setname)
            if cls == "StationaryBlocksParts":
                mesh_mats.setdefault(mn, main[:1] + sec[:1])
            elif mn == "Tab_Ice":
                mesh_mats.setdefault(mn, ["BlockKnobIce"])
            else:
                mesh_mats.setdefault(mn, main[:1])
    return mesh_set, mesh_mats, info


def renderer_mats():
    """Mesh name -> material names from MeshFilter+MeshRenderer pairs in scenes/prefabs."""
    out = {}
    for o in OBJS:
        if o.type.name != "MeshFilter":
            continue
        mf = o.read()
        if not mf.m_Mesh.path_id:
            continue
        try:
            mesh = mf.m_Mesh.read().m_Name
            go = mf.m_GameObject.read()
        except Exception:
            continue
        for c in go.m_Component:
            cp = c.component if hasattr(c, "component") else c
            if cp.type.name == "MeshRenderer":
                out.setdefault(mesh, [obj_name(mp) if hasattr(mp, "read") else "?" for mp in cp.read().m_Materials])
    return out


def unity_items(geo, specs, xform=None):
    """Geometry in Unity space + material specs -> render3d items."""
    items = []
    V = geo["V"] if xform is None else xform(geo["V"])
    for t, sp in zip(geo["tris"], specs):
        if sp is None or len(t) == 0:
            continue
        items.append(dict(V=V, UV=geo["UV"], tris=t, tex=tex_array(sp.get("image")),
                          color=sp.get("color", (1, 1, 1, 1)) if sp.get("image") is None or sp.get("unlit") else (1, 1, 1, 1),
                          blend=sp.get("blend", False)))
    return items


def export_meshes():
    reset(OUT_MOD)
    mesh_set, mesh_mats, info = part_sets()
    rmats = renderer_mats()
    with open(os.path.join(OUT_DATA, "mesh_part_sets.json"), "w", encoding="utf-8") as fh:
        json.dump(info, fh, indent=1)
    tex_dir = os.path.join(OUT_MOD, "obj", "textures")
    os.makedirs(tex_dir, exist_ok=True)
    rows, previews = [], collections.defaultdict(list)
    for name, m in sorted(MESHES.items()):
        geo = mesh_geo(m)
        setname = mesh_set.get(name) or {"BlockHammer": "powerup_tools", "Scissor_01": "powerup_tools",
                                         "Scissor_02": "powerup_tools", "Rounded_Square": "board_tile"}.get(name, "other")
        mnames = rmats.get(name) or mesh_mats.get(name) or []
        mnames = (mnames + [mnames[-1]] * len(geo["tris"]))[:len(geo["tris"])] if mnames else [None] * len(geo["tris"])
        specs = [mat_spec(mn) if mn else dict(name="default", color=(0.8, 0.8, 0.8, 1)) for mn in mnames]
        # glb
        gdir = os.path.join(OUT_MOD, "glb", setname)
        os.makedirs(gdir, exist_ok=True)
        V, N, tris = to_gltf_space(geo["V"], geo["N"], geo["tris"])
        g = GLB()
        g.node(name, mesh=g.mesh(name, dict(V=V, N=N, UV=geo["UV"], tris=tris), specs), root=True)
        g.save(os.path.join(gdir, name + ".glb"))
        # obj + mtl
        odir = os.path.join(OUT_MOD, "obj", setname)
        os.makedirs(odir, exist_ok=True)
        mtl = []
        for sp in specs:
            mtl += [f"newmtl {sp['name']}", "Ka 0 0 0", "Ks 0 0 0", "illum 1"]
            c = sp.get("color", (1, 1, 1, 1))
            mtl.append(f"Kd {c[0]:.4f} {c[1]:.4f} {c[2]:.4f}")
            if sp.get("image") is not None:
                tn = safe(sp.get("image_key", sp["name"])) + ".png"
                if not os.path.exists(os.path.join(tex_dir, tn)):
                    sp["image"].save(os.path.join(tex_dir, tn))
                mtl.append(f"map_Kd ../textures/{tn}")
                if sp.get("blend"):
                    mtl.append(f"map_d ../textures/{tn}")
            mtl.append("")
        with open(os.path.join(odir, name + ".mtl"), "w", encoding="utf-8", newline="\n") as fh:
            fh.write("\n".join(mtl))
        write_obj(os.path.join(odir, name + ".obj"), name, geo, [sp["name"] for sp in specs], name + ".mtl")
        # preview
        pdir = os.path.join(OUT_MOD, "previews", setname)
        os.makedirs(pdir, exist_ok=True)
        side = setname == "board_backdrop"
        dark = setname in ("board_backdrop", "board_tile")  # navy meshes: light backdrop so they stay visible
        pv = render(unity_items(geo, specs), size=256, elev=20 if side else 55, azim=-30 if not side else 0,
                    shade_textured=0.25, bg=(150, 160, 180) if dark else (14, 28, 66))
        pv.save(os.path.join(pdir, name + ".png"))
        previews[setname].append((name, pv))
        ext = geo["V"].max(0) - geo["V"].min(0)
        rows.append(dict(mesh=name, set=setname, glb=f"glb/{setname}/{name}.glb", obj=f"obj/{setname}/{name}.obj",
                         vertices=len(geo["V"]), triangles=sum(len(t) for t in geo["tris"]), submeshes=len(geo["tris"]),
                         size_x=round(ext[0], 3), size_y=round(ext[1], 3), size_z=round(ext[2], 3),
                         has_uv=geo["UV"] is not None, has_vertex_color=geo["C"] is not None,
                         unity_materials=";".join(m_ or "" for m_ in mnames)))
    with open(os.path.join(OUT_MOD, "models_index.csv"), "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, list(rows[0].keys()))
        w.writeheader()
        w.writerows(sorted(rows, key=lambda r: (r["set"], r["mesh"])))
    print(f"meshes: {len(rows)} -> glb/obj/previews")
    return previews


# ---------------------------------------------------------------- 3D: prefabs (scene hierarchies) and an assembled block
def find_root(go_name):
    best = None
    for o in OBJS:
        if o.type.name == "GameObject" and obj_name(o) == go_name:
            go = o.read()
            tr = go.m_Component[0].component.read() if hasattr(go.m_Component[0], "component") else go.m_Component[0].read()
            if not tr.m_Father.path_id:
                if best is None or source_of(o).startswith("aa:"):
                    best = (go, tr)
    return best


def export_prefab(root_name, out_name):
    found = find_root(root_name)
    if not found:
        print("  prefab not found", root_name)
        return None
    g = GLB()
    items = []

    def comps(go, kind):
        for c in go.m_Component:
            cp = c.component if hasattr(c, "component") else c
            if cp.type.name == kind:
                return cp.read()
        return None

    def visit(tr, parent_mat, is_root):
        go = tr.m_GameObject.read()
        if not go.m_IsActive:
            return None
        p, r, s = tr.m_LocalPosition, tr.m_LocalRotation, tr.m_LocalScale
        pos, rot, sc = (p.x, p.y, p.z), (r.x, r.y, r.z, r.w), (s.x, s.y, s.z)
        if is_root:
            pos, rot = (0, 0, 0), (0, 0, 0, 1)
        M = parent_mat @ trs_matrix(pos, rot, sc)
        mesh_idx = None
        mf, mr = comps(go, "MeshFilter"), comps(go, "MeshRenderer")
        if mf is not None and mr is not None and mf.m_Mesh.path_id and mr.m_Enabled:
            um = mf.m_Mesh.read()
            geo = mesh_geo(um)
            specs = [mat_spec(obj_name(mp)) for mp in mr.m_Materials]
            specs = (specs + [specs[-1]] * len(geo["tris"]))[:len(geo["tris"])]
            V, N, tris = to_gltf_space(geo["V"], geo["N"], geo["tris"])
            mesh_idx = g.mesh(um.m_Name, dict(V=V, N=N, UV=geo["UV"], tris=tris), specs)
            Vw = (np.c_[geo["V"], np.ones(len(geo["V"]))] @ M.T)[:, :3]
            items.extend(unity_items(dict(geo, V=Vw), specs))
        kids = [k for k in (visit(c.read(), M, False) for c in tr.m_Children) if k is not None]
        if mesh_idx is None and not kids:
            return None
        return g.node(go.m_Name, mesh=mesh_idx, trs=to_gltf_trs(pos, rot, sc), children=kids or None)

    root = visit(found[1], np.eye(4), True)
    g.j["scenes"][0]["nodes"].append(root)
    d = os.path.join(OUT_MOD, "prefabs")
    os.makedirs(d, exist_ok=True)
    g.save(os.path.join(d, out_name + ".glb"))
    pv = render(items, size=256, elev=55, azim=-30, shade_textured=0.25)
    pv.save(os.path.join(d, out_name + ".png"))
    return pv


def trs_matrix(p, q, s):
    x, y, z, w = q
    R = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                  [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                  [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
    M = np.eye(4)
    M[:3, :3] = R * np.array(s)[None, :]
    M[:3, 3] = p
    return M


BLOCK_1X1 = ["FrontFace", "Edge_Top", "Edge_Bottom", "Edge_Left", "Edge_Right",
             "Corner_TopLeft", "Corner_TopRight", "Corner_BottomLeft", "Corner_BottomRight"]


def export_assembled_blocks():
    d = os.path.join(OUT_MOD, "assembled")
    os.makedirs(d, exist_ok=True)
    out = []
    for cname in PALETTE:
        g, items = GLB(), []
        spec = mat_spec("PieceBlockMaterial", color=cname)
        kids = []
        for pn in BLOCK_1X1:
            geo = mesh_geo(MESHES[pn])
            V, N, tris = to_gltf_space(geo["V"], geo["N"], geo["tris"])
            kids.append(g.node(pn, mesh=g.mesh(pn, dict(V=V, N=N, UV=geo["UV"], tris=tris), [spec] * len(tris))))
            items.extend(unity_items(geo, [spec] * len(tris)))
        g.node(f"Block_1x1_{cname}", children=kids, root=True)
        g.save(os.path.join(d, f"Block_1x1_{cname}.glb"))
        pv = render(items, size=192, elev=55, azim=-30, shade_textured=0.25)
        pv.save(os.path.join(d, f"Block_1x1_{cname}.png"))
        out.append((cname, pv))
    return out


def contact_sheet(entries, path, cols=6, cell=256, title=None):
    try:
        font = ImageFont.truetype("arial.ttf", 15)
        tfont = ImageFont.truetype("arialbd.ttf", 22)
    except Exception:
        font = tfont = ImageFont.load_default()
    rows = (len(entries) + cols - 1) // cols
    top = 44 if title else 0
    sheet = Image.new("RGB", (cols * cell, top + rows * (cell + 24)), (9, 18, 44))
    dr = ImageDraw.Draw(sheet)
    if title:
        dr.text((12, 10), title, fill=(255, 255, 255), font=tfont)
    for i, (name, im) in enumerate(entries):
        x, y = (i % cols) * cell, top + (i // cols) * (cell + 24)
        sheet.paste(im.convert("RGB").resize((cell, cell)), (x, y))
        dr.text((x + 6, y + cell + 3), name, fill=(220, 230, 255), font=font)
    sheet.save(path, optimize=True)


def main():
    for d in (OUT_DATA,):
        os.makedirs(d, exist_ok=True)
    export_textures()
    image_sheets()
    export_sprites()
    export_scriptable_objects()
    export_text_and_fonts()
    export_animations_and_shaders()
    previews = export_meshes()
    prefabs = []
    for root, out in [("Scissors", "Scissors"), ("PowerUp_Hammer", "PowerUp_Hammer"),
                      ("TileTemplateA", "TileTemplateA"), ("TileTemplateB", "TileTemplateB")]:
        pv = export_prefab(root, out)
        if pv is not None:
            prefabs.append((out, pv))
    blocks = export_assembled_blocks()
    tdir = os.path.join(OUT_MOD, "textures")
    os.makedirs(tdir, exist_ok=True)
    for cname in PALETTE:  # baked block + rope colours for every palette entry
        mat_spec("PieceBlockMaterial", color=cname)
        p = PALETTE[cname]
        baked(f"Rope_{cname}", lambda: bake_rope(tex_by_name("Rope_C"), p["rope_bright"], p["rope_dark"], p["rope_highlight"]))
    for k, im in BAKED.items():
        im.save(os.path.join(tdir, safe(k) + ".png"))
    order = ["piece_block", "board_wall", "board_backdrop", "stationary_block", "rope", "powerup_tools", "board_tile", "other"]
    allp = [(n, im) for s in order for n, im in previews.get(s, [])]
    contact_sheet(allp, os.path.join(OUT_MOD, "contact_sheet_meshes.png"), cols=7, cell=200,
                  title=f"Blocked In - {len(allp)} meshes (piece_block / board_wall / backdrop / stationary / rope / tools / tile)")
    contact_sheet(prefabs + [(f"Block_1x1_{n}", im) for n, im in blocks], os.path.join(OUT_MOD, "contact_sheet_prefabs_blocks.png"),
                  cols=8, cell=192, title="Prefabs + 1x1 piece block in the 12 palette colours")
    print("done")


if __name__ == "__main__":
    main()
