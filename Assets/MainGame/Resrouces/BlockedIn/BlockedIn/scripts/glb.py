"""Minimal glTF 2.0 binary (.glb) writer + Unity -> glTF/OBJ geometry conversion.

Unity is left-handed (x right, y up, z forward); glTF is right-handed. Like UnityGLTF we negate z on positions and
normals, flip the triangle winding, flip v on texture coordinates and map quaternions (x, y, z, w) -> (-x, -y, z, w).
"""
import io, json, struct
import numpy as np
from PIL import Image

FLOAT, UINT32, UINT16 = 5126, 5125, 5123
ARRAY_BUFFER, ELEMENT_ARRAY_BUFFER = 34962, 34963


def png_bytes(img):
    buf = io.BytesIO()
    img.save(buf, format="PNG", optimize=True)
    return buf.getvalue()


def to_gltf_space(V, N, tris):
    V = V.copy(); V[:, 2] *= -1
    if N is not None:
        N = N.copy(); N[:, 2] *= -1
    return V, N, [t[:, [0, 2, 1]] for t in tris]


def to_gltf_trs(pos, rot, scale):
    return [pos[0], pos[1], -pos[2]], [-rot[0], -rot[1], rot[2], rot[3]], list(scale)


class GLB:
    def __init__(self, generator="BlockedIn/scripts"):
        self.bin = bytearray()
        self.j = {"asset": {"version": "2.0", "generator": generator}, "scene": 0, "scenes": [{"nodes": []}],
                  "nodes": [], "meshes": [], "accessors": [], "bufferViews": [], "materials": [], "textures": [],
                  "images": [], "samplers": [{"magFilter": 9729, "minFilter": 9987, "wrapS": 10497, "wrapT": 10497}]}
        self._mats = {}
        self._imgs = {}
        self.unlit_used = False

    def _view(self, data, target=None):
        while len(self.bin) % 4:
            self.bin.append(0)
        bv = {"buffer": 0, "byteOffset": len(self.bin), "byteLength": len(data)}
        if target:
            bv["target"] = target
        self.bin += data
        self.j["bufferViews"].append(bv)
        return len(self.j["bufferViews"]) - 1

    def _accessor(self, arr, comp, typ, target, minmax=False):
        arr = np.ascontiguousarray(arr)
        acc = {"bufferView": self._view(arr.tobytes(), target), "componentType": comp,
               "count": int(arr.shape[0]) if typ != "SCALAR" else int(arr.size), "type": typ}
        if minmax:
            acc["min"] = [float(x) for x in arr.min(0)]
            acc["max"] = [float(x) for x in arr.max(0)]
        self.j["accessors"].append(acc)
        return len(self.j["accessors"]) - 1

    def _image(self, key, img):
        if key not in self._imgs:
            self.j["images"].append({"name": key, "mimeType": "image/png", "bufferView": self._view(png_bytes(img))})
            self.j["textures"].append({"sampler": 0, "source": len(self.j["images"]) - 1})
            self._imgs[key] = len(self.j["textures"]) - 1
        return self._imgs[key]

    def material(self, spec):
        """spec: dict(name, image=PIL|None, image_key, color=(r,g,b,a), blend=bool, unlit=bool, double=bool)"""
        key = spec["name"]
        if key in self._mats:
            return self._mats[key]
        pbr = {"baseColorFactor": [float(c) for c in spec.get("color", (1, 1, 1, 1))], "metallicFactor": 0.0,
               "roughnessFactor": 0.8}
        if spec.get("image") is not None:
            pbr["baseColorTexture"] = {"index": self._image(spec.get("image_key", key), spec["image"])}
        m = {"name": key, "pbrMetallicRoughness": pbr}
        if spec.get("blend"):
            m["alphaMode"] = "BLEND"
        if spec.get("double", True):
            m["doubleSided"] = True
        if spec.get("unlit"):
            m["extensions"] = {"KHR_materials_unlit": {}}
            self.unlit_used = True
        self.j["materials"].append(m)
        self._mats[key] = len(self.j["materials"]) - 1
        return self._mats[key]

    def mesh(self, name, geo, mats):
        """geo: dict V, N, UV, tris (already in glTF space); mats: material spec per submesh (None = skip submesh)."""
        V, N, UV = geo["V"], geo["N"], geo["UV"]
        attrs = {"POSITION": self._accessor(V.astype(np.float32), FLOAT, "VEC3", ARRAY_BUFFER, True)}
        if N is not None:
            n = N.astype(np.float32)
            n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-8)
            attrs["NORMAL"] = self._accessor(n, FLOAT, "VEC3", ARRAY_BUFFER)
        if UV is not None:
            uv = UV.astype(np.float32).copy()
            uv[:, 1] = 1.0 - uv[:, 1]
            attrs["TEXCOORD_0"] = self._accessor(uv, FLOAT, "VEC2", ARRAY_BUFFER)
        prims = []
        for t, spec in zip(geo["tris"], mats):
            if spec is None or len(t) == 0:
                continue
            idx = t.astype(np.uint32).reshape(-1)
            prims.append({"attributes": attrs, "indices": self._accessor(idx, UINT32, "SCALAR", ELEMENT_ARRAY_BUFFER),
                          "material": self.material(spec)})
        self.j["meshes"].append({"name": name, "primitives": prims})
        return len(self.j["meshes"]) - 1

    def node(self, name, mesh=None, trs=None, children=None, root=False):
        n = {"name": name}
        if mesh is not None:
            n["mesh"] = mesh
        if trs:
            t, r, s = trs
            if any(abs(x) > 1e-7 for x in t): n["translation"] = [float(x) for x in t]
            if abs(r[3] - 1) > 1e-7 or any(abs(x) > 1e-7 for x in r[:3]): n["rotation"] = [float(x) for x in r]
            if any(abs(x - 1) > 1e-7 for x in s): n["scale"] = [float(x) for x in s]
        if children:
            n["children"] = children
        self.j["nodes"].append(n)
        i = len(self.j["nodes"]) - 1
        if root:
            self.j["scenes"][0]["nodes"].append(i)
        return i

    def save(self, path):
        j = {k: v for k, v in self.j.items() if v != [] or k in ("scenes", "nodes")}
        if not j.get("textures"):
            j.pop("samplers", None)
        if self.unlit_used:
            j["extensionsUsed"] = ["KHR_materials_unlit"]
        while len(self.bin) % 4:
            self.bin.append(0)
        j["buffers"] = [{"byteLength": len(self.bin)}]
        js = json.dumps(j, separators=(",", ":")).encode()
        js += b" " * ((4 - len(js) % 4) % 4)
        out = struct.pack("<III", 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(self.bin))
        out += struct.pack("<II", len(js), 0x4E4F534A) + js
        out += struct.pack("<II", len(self.bin), 0x004E4942) + bytes(self.bin)
        with open(path, "wb") as fh:
            fh.write(out)
        return len(out)


def write_obj(path, name, geo_unity, mat_names, mtl_name):
    """OBJ in right-handed space (z negated); UVs keep the Unity/OBJ bottom-left origin."""
    V, N, tris = to_gltf_space(geo_unity["V"], geo_unity["N"], geo_unity["tris"])
    UV = geo_unity["UV"]
    L = [f"# {name} - exported from Blocked In (Unity mesh, z negated to right-handed)", f"mtllib {mtl_name}", f"o {name}"]
    L += [f"v {x:.6f} {y:.6f} {z:.6f}" for x, y, z in V]
    if UV is not None:
        L += [f"vt {u:.6f} {v:.6f}" for u, v in UV[:, :2]]
    if N is not None:
        L += [f"vn {x:.5f} {y:.5f} {z:.5f}" for x, y, z in N[:, :3]]
    for si, (t, mn) in enumerate(zip(tris, mat_names)):
        if mn is None or len(t) == 0:
            continue
        L += [f"g {name}_sub{si}", f"usemtl {mn}"]
        for a, b, c in t + 1:
            if UV is not None and N is not None:
                L.append(f"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}")
            elif N is not None:
                L.append(f"f {a}//{a} {b}//{b} {c}//{c}")
            else:
                L.append(f"f {a} {b} {c}")
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(L) + "\n")
