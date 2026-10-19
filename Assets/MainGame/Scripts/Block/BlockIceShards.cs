using UnityEngine;
using UnityEngine.Rendering;

namespace BlockedIn.Blocks
{
    public sealed class BlockIceShards : MonoBehaviour
    {
        const int Capacity = 48;
        struct Shard
        {
            public Vector3 position, velocity, rotation, angularVelocity;
            public float age, lifetime, size;
            public int mesh;
        }
        readonly Shard[] shards = new Shard[Capacity];
        MaterialPropertyBlock properties;
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        Mesh[] meshes;
        Material material;
        BlockIcePerimeter perimeter;
        Transform emitter;
        int cursor;
        public int ActiveCount { get; private set; }

        public void Configure(Transform origin, Mesh shell, Mesh[] pieces, Material surface)
        {
            properties = new MaterialPropertyBlock();
            emitter = origin;meshes = pieces;material = surface;
            perimeter = shell != null ? new BlockIcePerimeter(shell) : null;
            enabled = false;
        }

        public void Emit(bool finalBreak)
        {
            if (emitter == null || perimeter == null || perimeter.Length <= 0 || material == null || meshes == null || meshes.Length == 0) return;
            int count = finalBreak ? 24 : 12;
            float offset = Random.value * perimeter.Length;
            for (int i = 0; i < count; i++)
            {
                float distance = offset + (i + Random.value * .6f) * perimeter.Length / count;
                perimeter.Sample(distance, out Vector3 point, out Vector3 outward);
                Vector3 direction = emitter.TransformDirection(outward).normalized;
                Vector3 tangent = Vector3.Cross(Vector3.up, direction);
                shards[cursor] = new Shard
                {
                    position = emitter.TransformPoint(point),
                    velocity = direction * Random.Range(.7f, 1.35f) * (finalBreak ? 1.2f : 1)
                        + tangent * Random.Range(-.18f, .18f) + Vector3.up * Random.Range(.5f, .9f),
                    lifetime = Random.Range(.58f, .82f), size = Random.Range(.13f, .29f) * (finalBreak ? 1.2f : 1),
                    rotation = new Vector3(Random.Range(-25,25), Random.Range(0,360), Random.Range(-25,25)),
                    angularVelocity = new Vector3(Random.Range(-140,140),Random.Range(-220,220),Random.Range(-140,140)),
                    mesh = cursor % meshes.Length
                };
                cursor = (cursor + 1) % Capacity;
            }
            enabled = true;
        }

        void LateUpdate() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            ActiveCount = 0;
            for (int i = 0; i < Capacity; i++)
            {
                Shard shard = shards[i];
                if (shard.lifetime <= 0 || shard.age >= shard.lifetime) continue;
                shard.age += deltaTime;
                shard.velocity += new Vector3(0, -2.6f, -3.8f) * deltaTime;
                shard.position += shard.velocity * deltaTime;
                shard.rotation += shard.angularVelocity * deltaTime;
                shards[i] = shard;
                float remaining = 1 - shard.age / shard.lifetime;
                if (remaining <= 0) continue;
                ActiveCount++;
                properties.SetColor(ColorId, new Color(.9f, .98f, 1, Mathf.Clamp01(remaining * 4)));
                var matrix = Matrix4x4.TRS(shard.position, Quaternion.Euler(shard.rotation),
                    Vector3.one * shard.size * Mathf.Min(1, remaining * 5));
                Graphics.DrawMesh(meshes[shard.mesh], matrix, material, gameObject.layer, null, 0, properties,
                    ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
            }
            if (ActiveCount == 0) enabled = false;
        }

        void OnDisable()
        {
            for (int i = 0; i < Capacity; i++) shards[i].lifetime = 0;
            ActiveCount = 0;
        }
    }
}
