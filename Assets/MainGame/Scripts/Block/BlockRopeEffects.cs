using UnityEngine;

namespace BlockedIn.Blocks
{
    public sealed class BlockRopeEffects : MonoBehaviour
    {
        [SerializeField] TrailRenderer trail;
        [SerializeField] MeshRenderer glow;
        [SerializeField] Transform[] sparks;
        MaterialPropertyBlock properties;
        public void Configure(TrailRenderer ribbon, MeshRenderer halo, Transform[] stars)
        {
            trail = ribbon; glow = halo; sparks = stars;
            trail.emitting = false;
            foreach (Transform spark in sparks) spark.gameObject.SetActive(false);
        }
        public void StartFlight()
        {
            if (trail != null) { trail.Clear(); trail.emitting = true; }
        }
        public void Glow(float strength, Quaternion facing)
        {
            if (glow == null) return;
            glow.transform.rotation = facing;
            properties ??= new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", new Color(1f, .9f, .65f, strength));
            glow.SetPropertyBlock(properties);
        }
        public void Burst(float progress, Vector3 position, Quaternion facing)
        {
            Vector3 right = facing * Vector3.right, up = facing * Vector3.up;
            for (int i = 0; i < sparks.Length; i++)
            {
                Transform spark = sparks[i];
                if (spark == null) continue;
                spark.gameObject.SetActive(progress < 1f);
                float angle = i * Mathf.PI * 2f / sparks.Length;
                float distance = (.3f + i * .045f) * (1f - Mathf.Pow(1f - progress, 2f));
                spark.position = position + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * distance;
                spark.rotation = facing * Quaternion.AngleAxis(progress * 150f, Vector3.forward);
                spark.localScale = Vector3.one * ((.1f + i * .012f) * (1f - progress));
            }
        }
        public void StopFlight()
        {
            if (trail != null) trail.emitting = false;
        }
    }
}
