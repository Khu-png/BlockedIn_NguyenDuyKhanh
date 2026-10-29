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
            SetTrail(trail, true, new Color(.25f, 2.4f, 3.2f, 1f), .32f, .24f);
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
                ropeTrails[i] = CreateTrail("Rope Magic Trail", halves[i], color * 2.4f, .48f, .13f,
                    new Vector3(0, .025f, 0));
            }
        }

        public void Flight(float progress, Quaternion facing)
        {
            if (!prepared || flightOrbit == null || glow == null) return;
            float pulse = .5f + .5f * Mathf.Sin(progress * Mathf.PI * 6f);
            float radius = Mathf.Lerp(.25f, .58f, Mathf.Sin(progress * Mathf.PI)) + pulse * .05f;
            DrawRing(flightOrbit, glow.transform.position, facing, radius, progress * 260f);
            Color color = Color.Lerp(new Color(.2f, 2.2f, 3f, .9f), new Color(2.1f, .35f, 3f, .9f), pulse);
            SetLineColor(flightOrbit, color, new Color(3f, 1.5f, .25f, .65f));
        }

        public void Glow(float strength, Quaternion facing)
        {
            if (glow == null) return;
            glow.transform.rotation = facing;
            properties ??= new MaterialPropertyBlock();
            float pulse = .5f + .5f * Mathf.Sin(Time.time * 9f);
            Color color = Color.Lerp(new Color(.2f, 2.6f, 3.6f), new Color(2.3f, .35f, 3.5f), pulse);
            color.a = Mathf.Clamp01(strength * 1.8f);
            properties.SetColor("_BaseColor", color);
            glow.SetPropertyBlock(properties);
        }

        public void Burst(float progress, Vector3 position, Quaternion facing)
        {
            Prepare();
            if (!prepared) return;
            float ease = 1f - Mathf.Pow(1f - progress, 2f);
            DrawRing(cutRing, position, facing, Mathf.Lerp(.12f, 1.12f, ease), progress * 55f);
            DrawRing(cutRune, position, facing, Mathf.Lerp(.22f, .78f, ease), -progress * 95f);
            DrawSlash(position, facing, progress);
            float alpha = 1f - Mathf.SmoothStep(.35f, 1f, progress);
            SetLineColor(cutRing, new Color(.2f, 3.2f, 4f, alpha), new Color(2.8f, .5f, 3.8f, alpha));
            SetLineColor(cutRune, new Color(4f, 2f, .25f, alpha),
                new Color(.35f, 2.8f, 3.8f, alpha * .8f));
            SetLineColor(slash, new Color(5f, 4.5f, 2f, alpha), new Color(.5f, 2.8f, 4f, alpha));
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
            ExpandSparks();
            effectMaterial = trail.sharedMaterial;
            Transform tool = trail.transform.parent;
            flightTrails = new[]
            {
                CreateTrail("Violet Echo", tool, new Color(2.4f,.35f,3.8f,.9f), .34f, .14f,
                    new Vector3(-.07f,.02f,.03f)),
                CreateTrail("Golden Echo", tool, new Color(4f,1.8f,.2f,.8f), .22f, .075f,
                    new Vector3(.075f,.015f,-.025f))
            };
            flightOrbit = CreateLine("Mystic Orbit", true, .055f, RingPoints);
            cutRing = CreateLine("Cut Shock Ring", true, .115f, RingPoints);
            cutRune = CreateLine("Cut Magic Seal", true, .065f, RingPoints);
            slash = CreateLine("Cut Crescent", false, .18f, 18);
            StopFlight();
        }

        void ExpandSparks()
        {
            if (sparks == null || sparks.Length == 0 || sparks.Length >= 12) return;
            Transform[] expanded = new Transform[12];
            for (int i = 0; i < expanded.Length; i++)
            {
                if (i < sparks.Length) { expanded[i] = sparks[i]; continue; }
                GameObject copy = Instantiate(sparks[i % sparks.Length].gameObject, transform);
                copy.name = "CutSpark_" + i; copy.SetActive(false); expanded[i] = copy.transform;
            }
            sparks = expanded;
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
            float radius = Mathf.Lerp(.18f, 1.05f, 1f - Mathf.Pow(1f - progress, 2f));
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
                spark.localScale = Vector3.one * ((.22f + i * .018f) * Mathf.Sin(progress * Mathf.PI));
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
