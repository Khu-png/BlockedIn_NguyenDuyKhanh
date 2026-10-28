using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed class BlockRopeEffects : MonoBehaviour
    {
        const int RingPoints = 32;
        [SerializeField] TrailRenderer trail;
        [SerializeField] MeshRenderer glow;
        [SerializeField] Transform[] sparks;
        MaterialPropertyBlock properties;
        TrailRenderer[] flightTrails, ropeTrails;
        LineRenderer flightOrbit, cutRing, cutRune, slash;
        Material effectMaterial;
        bool prepared;

        public void Configure(TrailRenderer ribbon, MeshRenderer halo, Transform[] stars)
        {
            trail = ribbon; glow = halo; sparks = stars;
            trail.emitting = false;
            foreach (Transform spark in sparks) spark.gameObject.SetActive(false);
        }

        public void StartFlight()
        {
            Prepare();
            if (!prepared) return;
            SetTrail(trail, true, new Color(.22f, .9f, 1f, .92f), .26f, .15f);
            for (int i = 0; i < flightTrails.Length; i++) flightTrails[i].emitting = true;
            flightOrbit.enabled = true;
            cutRing.enabled = cutRune.enabled = slash.enabled = false;
        }

        public void PrepareRopeTrails(Transform[] halves)
        {
            Prepare();
            if (!prepared) return;
            if (ropeTrails != null || halves == null) return;
            ropeTrails = new TrailRenderer[halves.Length];
            for (int i = 0; i < halves.Length; i++)
            {
                if (halves[i] == null) continue;
                Color color = i == 0 ? new Color(.2f, .9f, 1f, .8f) : new Color(.72f, .3f, 1f, .8f);
                ropeTrails[i] = CreateTrail("Rope Magic Trail", halves[i], color, .4f, .075f,
                    new Vector3(0, .025f, 0));
            }
        }

        public void Flight(float progress, Quaternion facing)
        {
            if (!prepared || flightOrbit == null || glow == null) return;
            float pulse = .5f + .5f * Mathf.Sin(progress * Mathf.PI * 6f);
            float radius = Mathf.Lerp(.2f, .46f, Mathf.Sin(progress * Mathf.PI)) + pulse * .035f;
            DrawRing(flightOrbit, glow.transform.position, facing, radius, progress * 260f);
            Color color = Color.Lerp(new Color(.15f, .85f, 1f, .62f),
                new Color(.68f, .28f, 1f, .7f), pulse);
            SetLineColor(flightOrbit, color, new Color(1f, .72f, .22f, .3f));
        }

        public void Glow(float strength, Quaternion facing)
        {
            if (glow == null) return;
            glow.transform.rotation = facing;
            properties ??= new MaterialPropertyBlock();
            float pulse = .5f + .5f * Mathf.Sin(Time.time * 9f);
            Color color = Color.Lerp(new Color(.2f, .8f, 1f), new Color(.72f, .32f, 1f), pulse);
            color.a = Mathf.Clamp01(strength * 1.8f);
            properties.SetColor("_BaseColor", color);
            glow.SetPropertyBlock(properties);
        }

        public void Burst(float progress, Vector3 position, Quaternion facing)
        {
            Prepare();
            if (!prepared) return;
            float ease = 1f - Mathf.Pow(1f - progress, 2f);
            DrawRing(cutRing, position, facing, Mathf.Lerp(.12f, .72f, ease), progress * 55f);
            DrawRing(cutRune, position, facing, Mathf.Lerp(.2f, .5f, ease), -progress * 95f);
            DrawSlash(position, facing, progress);
            float alpha = 1f - Mathf.SmoothStep(.35f, 1f, progress);
            SetLineColor(cutRing, new Color(.15f, .9f, 1f, alpha), new Color(.7f, .3f, 1f, alpha));
            SetLineColor(cutRune, new Color(1f, .78f, .18f, alpha * .9f),
                new Color(.3f, .85f, 1f, alpha * .45f));
            SetLineColor(slash, new Color(1f, .94f, .7f, alpha), new Color(.45f, .75f, 1f, alpha));
            cutRing.enabled = cutRune.enabled = slash.enabled = progress < 1f;
            SetRopeTrails(progress < .82f);
            AnimateSparks(progress, position, facing);
        }

        public void StopFlight()
        {
            if (trail != null) trail.emitting = false;
            if (flightTrails != null)
                foreach (TrailRenderer item in flightTrails) if (item != null) item.emitting = false;
            if (flightOrbit != null) flightOrbit.enabled = false;
            if (cutRing != null) cutRing.enabled = cutRune.enabled = slash.enabled = false;
            SetRopeTrails(false);
        }

        void Prepare()
        {
            if (prepared || trail == null) return;
            prepared = true;
            effectMaterial = trail.sharedMaterial;
            Transform tool = trail.transform.parent;
            flightTrails = new[]
            {
                CreateTrail("Violet Echo", tool, new Color(.66f,.25f,1f,.75f), .32f, .1f,
                    new Vector3(-.07f,.02f,.03f)),
                CreateTrail("Golden Echo", tool, new Color(1f,.72f,.18f,.58f), .2f, .055f,
                    new Vector3(.075f,.015f,-.025f))
            };
            flightOrbit = CreateLine("Mystic Orbit", true, .035f, RingPoints);
            cutRing = CreateLine("Cut Shock Ring", true, .055f, RingPoints);
            cutRune = CreateLine("Cut Magic Seal", true, .025f, RingPoints);
            slash = CreateLine("Cut Crescent", false, .085f, 18);
            StopFlight();
        }

        TrailRenderer CreateTrail(string name, Transform parent, Color color, float time,
            float width, Vector3 offset)
        {
            var item = new GameObject(name); item.layer = gameObject.layer;
            item.transform.SetParent(parent, false); item.transform.localPosition = offset;
            TrailRenderer result = item.AddComponent<TrailRenderer>();
            result.sharedMaterial = effectMaterial; result.time = time; result.minVertexDistance = .018f;
            result.startWidth = width; result.endWidth = 0; result.startColor = color;
            result.endColor = new Color(color.r, color.g, color.b, 0);
            result.numCornerVertices = 3; result.numCapVertices = 2; result.emitting = false;
            result.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return result;
        }

        LineRenderer CreateLine(string name, bool loop, float width, int count)
        {
            var item = new GameObject(name); item.layer = gameObject.layer;
            item.transform.SetParent(transform, false);
            LineRenderer result = item.AddComponent<LineRenderer>();
            result.sharedMaterial = effectMaterial; result.useWorldSpace = true; result.loop = loop;
            result.positionCount = count; result.startWidth = width; result.endWidth = width * .35f;
            result.numCornerVertices = 4; result.numCapVertices = 3; result.enabled = false;
            result.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return result;
        }

        static void SetTrail(TrailRenderer item, bool emitting, Color color, float time, float width)
        {
            if (item == null) return;
            item.Clear(); item.emitting = emitting; item.time = time;
            item.startWidth = width; item.endWidth = 0; item.startColor = color;
            item.endColor = new Color(color.r, color.g, color.b, 0);
        }

        static void DrawRing(LineRenderer line, Vector3 center, Quaternion facing, float radius, float phase)
        {
            Vector3 right = facing * Vector3.right, up = facing * Vector3.up;
            for (int i = 0; i < RingPoints; i++)
            {
                float angle = (i / (float)RingPoints * 360f + phase) * Mathf.Deg2Rad;
                float ripple = 1f + Mathf.Sin(angle * 3f + phase * Mathf.Deg2Rad) * .055f;
                line.SetPosition(i, center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius * ripple);
            }
        }

        void DrawSlash(Vector3 center, Quaternion facing, float progress)
        {
            Vector3 right = facing * Vector3.right, up = facing * Vector3.up;
            float radius = Mathf.Lerp(.18f, .68f, 1f - Mathf.Pow(1f - progress, 2f));
            for (int i = 0; i < slash.positionCount; i++)
            {
                float angle = Mathf.Lerp(-72f, 72f, i / (float)(slash.positionCount - 1)) * Mathf.Deg2Rad;
                slash.SetPosition(i, center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius);
            }
        }

        void AnimateSparks(float progress, Vector3 position, Quaternion facing)
        {
            Vector3 right = facing * Vector3.right, up = facing * Vector3.up;
            for (int i = 0; i < sparks.Length; i++)
            {
                Transform spark = sparks[i]; if (spark == null) continue;
                spark.gameObject.SetActive(progress < 1f);
                float angle = i * Mathf.PI * 2f / sparks.Length + progress * 1.4f;
                float distance = (.25f + i * .06f) * (1f - Mathf.Pow(1f - progress, 2f));
                spark.position = position + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * distance;
                spark.rotation = facing * Quaternion.AngleAxis(progress * 190f + i * 20f, Vector3.forward);
                spark.localScale = Vector3.one * ((.14f + i * .016f) * Mathf.Sin(progress * Mathf.PI));
            }
        }

        void SetRopeTrails(bool value)
        {
            if (ropeTrails == null) return;
            foreach (TrailRenderer item in ropeTrails) if (item != null) item.emitting = value;
        }

        static void SetLineColor(LineRenderer line, Color start, Color end)
        {
            line.startColor = start; line.endColor = end;
        }
    }
}
