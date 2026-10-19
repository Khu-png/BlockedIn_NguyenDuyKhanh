using System;
using System.Collections;
using UnityEngine;

public sealed class ResultPopupSequence : MonoBehaviour
{
    [Serializable] struct Part
    {
        public RectTransform rect;
        public CanvasGroup group;
        public float delay;
        public bool action;
    }
    [SerializeField] Part[] parts=Array.Empty<Part>();
    Vector2[] positions;
    Vector3[] scales;
    Coroutine routine;
    public bool IsReady { get; private set; }

    public void Play(bool win)
    {
        Stop();Cache();IsReady=false;
        Sample(0,win);
        routine=StartCoroutine(Reveal(win));
    }
    void Cache()
    {
        if(positions!=null)return;
        positions=new Vector2[parts.Length];scales=new Vector3[parts.Length];
        for(int i=0;i<parts.Length;i++)if(parts[i].rect!=null)
        { positions[i]=parts[i].rect.anchoredPosition;scales[i]=parts[i].rect.localScale; }
    }
    IEnumerator Reveal(bool win)
    {
        float elapsed=0,duration=0;
        foreach(Part part in parts)duration=Mathf.Max(duration,part.delay+.25f);
        while(elapsed<duration)
        {
            elapsed+=Time.unscaledDeltaTime;Sample(elapsed,win);yield return null;
        }
        Restore();IsReady=true;routine=null;
    }
    public void Sample(float elapsed,bool win)
    {
        Cache();
        for(int i=0;i<parts.Length;i++)
        {
            Part part=parts[i];float t=Mathf.Clamp01((elapsed-part.delay)/.25f);
            float ease=1-Mathf.Pow(1-t,3);
            if(part.group!=null)
            {
                part.group.alpha=ease;
                part.group.interactable=part.group.blocksRaycasts=part.action&&t>=1;
            }
            if(part.rect==null)continue;
            float pop=win?Mathf.Sin(t*Mathf.PI)*.045f:0;
            float start=win?.88f:.97f;
            part.rect.localScale=scales[i]*(Mathf.Lerp(start,1,ease)+pop);
            part.rect.anchoredPosition=positions[i]+Vector2.up*((win?-24:14)*(1-ease));
        }
    }
    public void Stop()
    {
        if(routine!=null)StopCoroutine(routine);routine=null;
        Restore();IsReady=false;
    }
    void Restore()
    {
        if(positions==null)return;
        for(int i=0;i<parts.Length;i++)
        {
            Part part=parts[i];
            if(part.rect!=null){part.rect.anchoredPosition=positions[i];part.rect.localScale=scales[i];}
            if(part.group!=null){part.group.alpha=1;part.group.interactable=part.group.blocksRaycasts=part.action;}
        }
    }
    void OnDisable()=>Stop();
}
