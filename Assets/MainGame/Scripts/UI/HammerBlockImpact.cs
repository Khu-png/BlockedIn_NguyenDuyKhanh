using System.Collections;
using BlockedIn.Blocks;
using UnityEngine;
public sealed class HammerBlockImpact
{
    struct Pose
    {
        public Transform visual;
        public Vector3 position, scale, outward, up;
        public Quaternion rotation;
    }
    readonly Pose[] poses;
    public HammerBlockImpact(Block[] parts, BlockBoard board)
    {
        poses = new Pose[parts.Length];
        Vector3 center = Vector3.zero;
        var centers = new Vector3[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            foreach (Vector2Int cell in parts[i].Cells)
                centers[i] += board.GridToWorld(parts[i].GridPosition + cell);
            centers[i] /= parts[i].Cells.Length;
            center += centers[i];
        }
        center /= parts.Length;
        for (int i = 0; i < parts.Length; i++)
        {
            Transform visual = parts[i].Visual;
            Vector3 direction = Vector3.ProjectOnPlane(centers[i] - center, board.Layout.transform.up);
            if (direction.sqrMagnitude < .001f) direction = board.Layout.transform.right * (i == 0 ? -1 : 1);
            Transform parent = visual.parent;
            poses[i] = new Pose
            {
                visual = visual, position = visual.localPosition, rotation = visual.localRotation,
                scale = visual.localScale,
                outward = parent.InverseTransformDirection(direction.normalized),
                up = parent.InverseTransformDirection(board.Layout.transform.up)
            };
        }
    }
    public IEnumerator Play()
    {
        float elapsed = 0;
        while (elapsed < .34f)
        {
            elapsed += Time.deltaTime;
            Sample(Mathf.Clamp01(elapsed / .34f));
            yield return null;
        }
        Restore();
    }
    public void Sample(float progress)
    {
        float t = Mathf.Clamp01(progress);
        float recoil = Mathf.Sin(t * Mathf.PI) * (1-t);
        float tremble = Mathf.Sin(t * Mathf.PI * 7) * (1-t) * (1-t);
        float squash = t < .18f ? Mathf.Sin(t / .18f * Mathf.PI) : 0;
        for (int i = 0; i < poses.Length; i++)
        {
            Pose pose = poses[i];
            if (pose.visual == null) continue;
            float sign = pose.outward.x + pose.outward.z * .5f >= 0 ? 1 : -1;
            pose.visual.localPosition = pose.position + pose.outward * (.27f * recoil + .035f * tremble)
                + pose.up * (.15f * Mathf.Sin(t * Mathf.PI));
            pose.visual.localRotation = Quaternion.AngleAxis(sign * (24 * recoil + 4 * tremble), pose.up) * pose.rotation;
            Vector3 scale = new Vector3(1 - .09f * squash + .04f * recoil, 1 - .13f * squash, 1 + .09f * squash + .04f * recoil);
            pose.visual.localScale = Vector3.Scale(pose.scale, scale);
        }
    }
    public void Restore()
    {
        foreach (Pose pose in poses)
        {
            if (pose.visual == null) continue;
            pose.visual.localPosition = pose.position;
            pose.visual.localRotation = pose.rotation;
            pose.visual.localScale = pose.scale;
        }
    }
}
