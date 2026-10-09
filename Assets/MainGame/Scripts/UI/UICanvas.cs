using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICanvas : MonoBehaviour
{
    //public bool IsAvoidBackKey = false;
    public bool IsDestroyOnClose = false;
    public bool IsHandlingRabbitEars = false;
    public bool IsWidescreenProcessing = false;

    [SerializeField] protected RectTransform m_RectTransform;
    [SerializeField] private Animator m_Animator;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private RectTransform[] safeAreaElements;
    private Coroutine closing;
    private Vector2[] safeAreaAnchorMin;
    private Vector2[] safeAreaAnchorMax;
    private Rect appliedSafeArea;
    private bool safeAreaAnchorsCached;

    private void Start()
    {
        CacheSafeAreaAnchors();
        ApplySafeArea();
        appliedSafeArea = Screen.safeArea;
        OnInit();
    }

    protected virtual void Update()
    {
        if (Screen.safeArea == appliedSafeArea) return;
        ApplySafeArea();
        appliedSafeArea = Screen.safeArea;
    }

    //Init default Canvas
    //khoi tao gia tri canvas
    protected void OnInit()
    {
        //Set parent cho popup child
        //cai nay tien cho viec truy suat truc tiep tu thang con ve thang cha quan ly
        for (int i = 0; i < popups.Length; i++)
        {
            if (popups[i] != null) popups[i].ParentsPopup = this;
        }
    }

    private void CacheSafeAreaAnchors()
    {
        if (safeAreaAnchorsCached) return;
        int count = safeAreaElements != null ? safeAreaElements.Length : 0;
        safeAreaAnchorMin = new Vector2[count];
        safeAreaAnchorMax = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            if (safeAreaElements[i] == null) continue;
            safeAreaAnchorMin[i] = safeAreaElements[i].anchorMin;
            safeAreaAnchorMax[i] = safeAreaElements[i].anchorMax;
        }
        safeAreaAnchorsCached = true;
    }

    private void ApplySafeArea()
    {
        if (Screen.width <= 0 || Screen.height <= 0 || safeAreaElements == null) return;
        if (safeAreaAnchorMin == null || safeAreaAnchorMin.Length != safeAreaElements.Length)
            CacheSafeAreaAnchors();

        Rect safeArea = Screen.safeArea;
        Vector2 safeMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
        Vector2 safeMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
        Vector2 safeSize = safeMax - safeMin;
        for (int i = 0; i < safeAreaElements.Length; i++)
        {
            RectTransform element = safeAreaElements[i];
            if (element == null) continue;
            element.anchorMin = safeMin + Vector2.Scale(safeAreaAnchorMin[i], safeSize);
            element.anchorMax = safeMin + Vector2.Scale(safeAreaAnchorMax[i], safeSize);
        }
    }

    //Setup canvas to avoid flash UI
    //set up mac dinh cho UI de tranh truong hop bi nhay' hinh
    public virtual void Setup()
    {
        UIManager.Ins.AddBackUI(this);
        UIManager.Ins.PushBackAction(this, BackKey);
    }


    //back key in android device
    //back key danh cho android
    public virtual void BackKey()
    {

    }

    //Open canvas
    //mo canvas
    public virtual void Open()
    {
        CancelCloseAnimation();
        gameObject.SetActive(true);
        ApplySafeArea();
        if (panelGroup != null) panelGroup.interactable = true;
        if (m_Animator != null)
        {
            m_Animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            m_Animator.Play("Open", 0, 0f);
            m_Animator.Update(0f);
        }
    }

    //close canvas directly
    //dong truc tiep, ngay lap tuc
    public virtual void CloseDirectly()
    {
        CancelCloseAnimation();
        UIManager.Ins.RemoveBackUI(this);
        gameObject.SetActive(false);
        if (IsDestroyOnClose)
        {
            Destroy(gameObject);
        }

    }

    public void CloseAnimated(System.Action onClosed = null)
    {
        if (closing != null || !gameObject.activeInHierarchy) return;
        if (m_Animator == null)
        {
            CloseDirectly();
            onClosed?.Invoke();
            return;
        }
        closing = StartCoroutine(PlayCloseAnimation(onClosed));
    }

    private IEnumerator PlayCloseAnimation(System.Action onClosed)
    {
        if (panelGroup != null) panelGroup.interactable = false;
        m_Animator.Play("Close", 0, 0f);
        m_Animator.Update(0f);
        do { yield return null; }
        while (m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f);
        closing = null;
        CloseDirectly();
        onClosed?.Invoke();
    }

    private void CancelCloseAnimation()
    {
        CancelInvoke(nameof(CloseDirectly));
        if (closing != null) StopCoroutine(closing);
        closing = null;
    }

    //close canvas with delay time, used to anim UI action
    //dong canvas sau mot khoang thoi gian delay
    public virtual void Close(float delayTime)
    {
        Debug.Log("Close");
        Invoke(nameof(CloseDirectly), delayTime);
    }


    #region Popup
    [Header("Popup Child")]
    [SerializeField] UICanvas[] popups = System.Array.Empty<UICanvas>();
    public UICanvas ParentsPopup { get; set; }

    public T GetPopup<T>() where T: UICanvas
    {
        T ui = null;
        for (int i = 0; i < popups.Length; i++)
        {
            if (popups[i] is T)
            {
                ui = popups[i] as T;
                break;
            }
        }

        return ui;
    }

    public T OpenPopup<T>() where T: UICanvas
    {
        T ui = GetPopup<T>();
        if (ui == null) return null;
        ui.Setup();
        ui.Open();
        return ui;
    }

    public bool IsOpenedPopup<T>() where T : UICanvas
    {
        T ui = GetPopup<T>();
        return ui != null && ui.gameObject.activeSelf;
    }


    public void ClosePopup<T>(float delayTime) where T: UICanvas
    {
        GetPopup<T>()?.Close(delayTime);
    }

    public void ClosePopupDirect<T>() where T: UICanvas
    {
        GetPopup<T>()?.CloseDirectly();
    }

    public void CloseAllPopup()
    {
        for (int i = 0; i < popups.Length; i++)
        {
            if (popups[i] != null) popups[i].CloseDirectly();
        }
    }

    #endregion
}
