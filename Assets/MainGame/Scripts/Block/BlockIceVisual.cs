using System.Collections;
using UnityEngine;
namespace BlockedIn.Blocks
{
    public sealed class BlockIceVisual : MonoBehaviour
    {
        [SerializeField] MeshFilter shell;
        [SerializeField] MeshRenderer shellRenderer;
        [SerializeField] TextMesh countText;
        Vector3 restScale;
        Coroutine breaking;
        public MeshFilter Shell => shell;
        public TextMesh CountText => countText;
        public void Configure(MeshFilter model, MeshRenderer renderer, TextMesh label)
        { shell=model; shellRenderer=renderer; countText=label; restScale=transform.localScale; }
        void Awake() => restScale=transform.localScale;
        public void ShowCount(int count)
        {
            if(breaking!=null) { StopCoroutine(breaking); breaking=null; }
            if(countText!=null) { countText.text=count.ToString(); countText.gameObject.SetActive(count>0); }
            if(count>0) { gameObject.SetActive(true); transform.localScale=restScale==Vector3.zero ? Vector3.one : restScale; if(shellRenderer!=null) shellRenderer.SetPropertyBlock(null); }
            else if(Application.isPlaying && gameObject.activeInHierarchy) breaking=StartCoroutine(BreakIce());
            else gameObject.SetActive(false);
        }
        IEnumerator BreakIce()
        {
            float elapsed=0; var properties=new MaterialPropertyBlock();
            while(elapsed<.18f)
            {
                elapsed+=Time.deltaTime; float progress=Mathf.Clamp01(elapsed/.18f);
                transform.localScale=restScale*(1+.12f*progress);
                properties.SetColor("_BaseColor",new Color(.8f,.95f,1,.42f*(1-progress)));
                if(shellRenderer!=null) shellRenderer.SetPropertyBlock(properties);
                yield return null;
            }
            transform.localScale=restScale; breaking=null; gameObject.SetActive(false);
        }
    }
    [System.Serializable]
    public struct BlockIceData
    {
        public int colorId;
        [Min(1)] public int count;
    }
}
