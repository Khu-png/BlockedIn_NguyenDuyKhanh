using System.Collections;
using UnityEngine;
public sealed class HammerFeedback : MonoBehaviour
{
    [SerializeField] Transform head;
    [SerializeField] ParticleSystem impact;
    [SerializeField] Material impactBurstMaterial;
    HammerImpactBurst impactBurst;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip hitSound;
    [SerializeField] AudioClip breakSound;
    [SerializeField, Range(0, 1)] float breakVolume = .45f;
    [SerializeField, Min(.1f)] float flightDuration = .52f;
    [SerializeField, Min(.1f)] float swingDuration = .2f;
    [SerializeField, Min(.1f)] float returnDuration = .5f;
    [SerializeField, Range(0,.15f)] float anticipationDuration=.065f;
    Vector3 launchPoint;
    float fullSize;
    static readonly Vector3 Grip = new Vector3(0, -1.15f, 0);
    static Quaternion Raised(Quaternion basis) => basis * Quaternion.Euler(-30, -15, -100);
    static Quaternion Contact(Quaternion basis) => basis * Quaternion.Euler(30, 20, -10);
    void Awake()
    {
        if (impactBurstMaterial != null) impactBurst = new HammerImpactBurst(impactBurstMaterial);
    }
    void Update() => impactBurst?.Tick(Time.deltaTime);
    void OnDestroy() => impactBurst?.Dispose();
    public IEnumerator Fly(Vector3 start, Vector3 target, Quaternion rotation, float size)
    {
        launchPoint = start;
        fullSize = size;
        Vector3 grip = target - Contact(rotation) * ((head.localPosition - Grip) * size);
        Vector3 destination = grip + Raised(rotation) * ((head.localPosition - Grip) * size);
        Vector3 bend = (start + destination) * .5f - rotation * Vector3.right * 1.25f;
        float elapsed = 0;
        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / flightDuration);
            float t = Mathf.SmoothStep(0, 1, progress);
            Vector3 point = (1-t)*(1-t)*start + 2*(1-t)*t*bend + t*t*destination;
            transform.localScale = Vector3.one * size * Mathf.Lerp(.32f, 1, t);
            Quaternion tumble = Quaternion.Euler(Mathf.Sin(progress*Mathf.PI)*210,
                (1-t)*360, (1-t)*540);
            PoseHead(point, Raised(rotation) * tumble);
            yield return null;
        }
        transform.localScale = Vector3.one * size;
        PoseHead(destination, Raised(rotation));
    }
    public IEnumerator Swing(Vector3 target, Quaternion rotation)
    {
        Vector3 grip = target - Contact(rotation) * ((head.localPosition - Grip) * fullSize);
        float anticipation=0;
        while(anticipation<anticipationDuration)
        {
            anticipation+=Time.deltaTime;
            float t=anticipationDuration>0?Mathf.Clamp01(anticipation/anticipationDuration):1;
            Quaternion pose=Raised(rotation)*Quaternion.Euler(0,0,-9*Mathf.Sin(t*Mathf.PI*.5f));
            transform.rotation=pose;transform.position=grip-pose*(Grip*fullSize);
            yield return null;
        }
        float elapsed = 0;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / swingDuration);
            Quaternion ready=Raised(rotation)*Quaternion.Euler(0,0,-9);
            Quaternion pose = Quaternion.Slerp(ready, Contact(rotation), t*t*t);
            transform.rotation = pose;
            transform.position = grip - pose * (Grip * fullSize);
            if (t < 1) yield return null;
        }
        PoseHead(target, Contact(rotation));
    }
    public void Hit(Vector3 point, Quaternion facing, float radius)
    {
        impactBurst?.Play(point, facing, radius);
        if (impact != null)
        {
            impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            impact.transform.position = point;
            var main = impact.main;
            main.startSize = new ParticleSystem.MinMaxCurve(.13f, .28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.4f, 4.8f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(.18f, .38f);
            impact.Play();
            impact.Emit(24);
        }
        if (SoundManager.Ins != null)
        {
            SoundManager.Ins.PlayEffect(hitSound);
            SoundManager.Ins.PlayEffect(breakSound, breakVolume);
        }
        else if (audioSource != null)
        {
            if (hitSound != null) audioSource.PlayOneShot(hitSound);
            if (breakSound != null) audioSource.PlayOneShot(breakSound, breakVolume);
        }
    }
    public IEnumerator Retreat(Quaternion rotation)
    {
        Vector3 start = head.position;
        Vector3 bend = (start + launchPoint) * .5f + rotation * new Vector3(.7f, -.7f, 0);
        float elapsed = 0;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            float travel = Mathf.SmoothStep(0,1,t);
            Vector3 point = (1-travel)*(1-travel)*start + 2*(1-travel)*travel*bend + travel*travel*launchPoint;
            point += rotation * Vector3.up * (Mathf.Sin(Mathf.Clamp01(t/.3f)*Mathf.PI)*.18f);
            transform.localScale = Vector3.one * fullSize * Mathf.Lerp(1, .24f, travel);
            Quaternion roll = Quaternion.Euler(t*210, Mathf.Sin(t*Mathf.PI)*90, t*400);
            PoseHead(point, Contact(rotation) * roll);
            yield return null;
        }
    }
    public void ResetForReuse()
    {
        impactBurst?.Clear();
        if (impact != null) impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (audioSource != null) audioSource.Stop();
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.localScale = Vector3.one;
        launchPoint = Vector3.zero;
        fullSize = 1f;
    }
    void PoseHead(Vector3 position, Quaternion rotation)
    {
        transform.rotation = rotation;
        transform.position += position - head.position;
    }
}
