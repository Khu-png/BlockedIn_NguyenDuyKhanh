using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    sealed class MatchSweepPool : IDisposable
    {
        public const float Duration = .32f;
        const int Capacity = 8;
        static readonly int JoinLine = Shader.PropertyToID("_JoinLine");
        static readonly int Radius = Shader.PropertyToID("_SweepRadius");
        static readonly int Progress = Shader.PropertyToID("_SweepProgress");
        static readonly int Tint = Shader.PropertyToID("_SweepTint");
        readonly Slot[] slots = new Slot[Capacity];
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();

        sealed class Slot
        {
            public GameObject root;
            public Mesh mesh;
            public MeshRenderer renderer;
            public List<Block> members;
            public Vector4 line;
            public Color tint;
            public float age, radius;
            public bool active;
        }

        public MatchSweepPool(Transform board, int columns, int rows, Material material)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var root = new GameObject("Connection Light Sweep");
                root.transform.SetParent(board, false);
                root.transform.localPosition = new Vector3(-(columns - 1) * .5f, .57f, (rows - 1) * .5f);
                Mesh mesh = new Mesh { name = "Connection Light Surface" };
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                root.SetActive(false);
                slots[i] = new Slot { root = root, mesh = mesh, renderer = renderer };
            }
        }

        public bool Play(Block first, Block second, IReadOnlyList<Block> blocks = null)
        {
            if (!BlockConnectionRules.CanMatch(first, second) || first.IsFrozen || second.IsFrozen
                || first.IsRoped || second.IsRoped || !MatchSweepShape.TryJoin(first, second, out Vector4 line)) return false;
            List<Block> members = MatchSweepShape.Connected(first, second, blocks);
            foreach (Slot slot in slots)
            {
                if (!slot.active || !slot.members.Exists(members.Contains)) continue;
                if (slot.age == 0 && slot.members.Count == members.Count
                    && slot.members.TrueForAll(members.Contains)) return true;
                slot.active = false;
                slot.root.SetActive(false);
            }
            Slot available = null, oldest = slots[0];
            foreach (Slot slot in slots)
            {
                if (!slot.active && available == null) available = slot;
                if (slot.age > oldest.age) oldest = slot;
            }
            Slot target = available ?? oldest;
            Vector2Int[] cells = MatchSweepShape.Cells(members);
            BlockPlacementMesh.Build(cells, target.mesh);
            target.members = members;
            target.line = line;
            target.radius = MatchSweepShape.Radius(cells, line);
            Color source = first.ColorData.Material != null && first.ColorData.Material.HasProperty("_BaseColor")
                ? first.ColorData.Material.GetColor("_BaseColor") : Color.white;
            Color.RGBToHSV(source, out float hue, out float saturation, out _);
            if (saturation > .02f) saturation = Mathf.Max(.65f, saturation);
            target.tint = Color.Lerp(Color.HSVToRGB(hue, saturation, 1), Color.white, .18f);
            target.age = 0;
            target.active = true;
            Apply(target);
            target.root.SetActive(true);
            return true;
        }

        public void Tick(float deltaTime)
        {
            foreach (Slot slot in slots)
            {
                if (!slot.active) continue;
                slot.age += Mathf.Max(0, deltaTime);
                if (slot.age >= Duration || HasMovingMember(slot.members))
                { slot.active = false; slot.root.SetActive(false); continue; }
                Apply(slot);
            }
        }

        static bool HasMovingMember(List<Block> members)
        {
            foreach (Block block in members)
                if (block == null || block.IsDragging) return true;
            return false;
        }

        void Apply(Slot slot)
        {
            properties.SetVector(JoinLine, slot.line);
            properties.SetFloat(Radius, slot.radius);
            properties.SetFloat(Progress, slot.age / Duration);
            properties.SetColor(Tint, slot.tint);
            slot.renderer.SetPropertyBlock(properties);
        }

        public void Dispose()
        {
            foreach (Slot slot in slots)
            {
                UnityEngine.Object.Destroy(slot.mesh);
                if (slot.root != null) UnityEngine.Object.Destroy(slot.root);
            }
        }
    }
}
