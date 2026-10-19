using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameplayResultFeedback : MonoBehaviour
{
    [SerializeField] RectTransform timerRoot;
    [SerializeField] Graphic timerDigits;
    Vector3 scale;
    Color color;
    bool captured;
    void Cache()
    {
        if(captured)return;captured=true;
        if(timerRoot!=null)scale=timerRoot.localScale;
        if(timerDigits!=null)color=timerDigits.color;
    }
    public IEnumerator Play(bool win)
    {
        Cache();float duration=win?.1f:.32f,elapsed=0;
        while(elapsed<duration)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01(elapsed/duration);
            if(!win)
            {
                float pulse=Mathf.Sin(t*Mathf.PI);
                if(timerRoot!=null)timerRoot.localScale=scale*(1+.075f*pulse);
                if(timerDigits!=null)timerDigits.color=Color.Lerp(color,new Color(1,.28f,.3f),pulse);
            }
            yield return null;
        }
        Restore();
    }
    public void Restore()
    {
        if(!captured)return;
        if(timerRoot!=null)timerRoot.localScale=scale;
        if(timerDigits!=null)timerDigits.color=color;
    }
    void OnDisable()=>Restore();
}
