///
/// Create by linh soi - Abi Game studio
/// mentor Minh tito - CTO Abi Game studio
///
/// Manage list UI canvas for easy to use
/// Member nen inherit UI canvas
///
/// Update: 09-10-2020
///             manage UI with Generic
///         09-10-2021
///             Open, Close UI with Typeof(T)
///         28/11/2022
///             Close All UI
///             Close delay time
///

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UIManager : Singleton<UIManager>
{
    //list from resource
    //list load ui resource
    [SerializeField] private UICanvas[] uiResources;

    //dict for quick query UI prefab
    //dict dung de lu thong tin prefab canvas truy cap cho nhanh
    private Dictionary<System.Type, UICanvas> uiCanvasPrefab = new Dictionary<System.Type, UICanvas>();
    //dict for UI active
    //dict luu cac ui dang dung
    private Dictionary<System.Type, UICanvas> uiCanvas = new Dictionary<System.Type, UICanvas>();

    //canvas container, it should be a canvas - root
    //canvas chua dung cac canvas con, nen la mot canvas - root de chua cac canvas nay
    public Transform CanvasParentTF;

#if UNITY_EDITOR
    private bool defaultsQueued;
    private void OnValidate()
    {
        if (Application.isPlaying || defaultsQueued) return;
        defaultsQueued = true;
        UnityEditor.EditorApplication.delayCall += AssignDefaultPrefabs;
    }
    private void AssignDefaultPrefabs()
    {
        defaultsQueued = false;
        if (this == null || Application.isPlaying) return;
        var items = new List<UICanvas>(uiResources ?? System.Array.Empty<UICanvas>());
        foreach (string name in new[] { "Gameplay", "Win", "Lose" })
        {
            if (items.Exists(item => item != null && item.GetType().Name == name)) continue;
            string path = "Assets/MainGame/Prefabs/UI/" + name + ".prefab";
            foreach (UnityEngine.Object asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is UICanvas canvas && canvas.GetType().Name == name)
                {
                    items.Add(canvas);
                    break;
                }
        }
        if (items.Count == (uiResources?.Length ?? 0)) return;
        uiResources = items.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void Awake()
    {
        RegisterSingleton(this);
        if (CanvasParentTF == null) CanvasParentTF = transform;
        if (uiResources == null) return;
        foreach (UICanvas canvas in uiResources)
        {
            if (canvas == null || !canvas.gameObject.scene.IsValid()) continue;
            uiCanvas[canvas.GetType()] = canvas;
            canvas.CloseDirectly();
        }
    }

    #region Canvas

    //open UI
    //mo UI canvas
    public T OpenUI<T>() where T : UICanvas
    {
        UICanvas canvas = GetUI<T>();
        if (canvas == null) return null;

        canvas.Setup();
        canvas.Open();

        return canvas as T;
    }

    //close UI directly
    //dong UI canvas ngay lap tuc
    public void CloseUI<T>() where T : UICanvas
    {
        if (IsOpened<T>())
        {
            GetUI<T>().CloseDirectly();
        }
    }

    //close UI with delay time
    //dong ui canvas sau delay time
    public void CloseUI<T>(float delayTime) where T : UICanvas
    {
        if (IsOpened<T>())
        {
            GetUI<T>().Close(delayTime);
        }
    }

    //check UI is Opened
    //kiem tra UI dang duoc mo len hay khong
    public bool IsOpened<T>() where T : UICanvas
    {
        return IsLoaded<T>() && uiCanvas[typeof(T)].gameObject.activeInHierarchy;
    }

    //check UI is loaded
    //kiem tra UI da duoc khoi tao hay chua
    public bool IsLoaded<T>() where T : UICanvas
    {
        System.Type type = typeof(T);
        return uiCanvas.ContainsKey(type) && uiCanvas[type] != null;
    }

    //Get component UI
    //lay component cua UI hien tai
    public T GetUI<T>() where T : UICanvas
    {
        //Debug.LogError(typeof(T));
        if (!IsLoaded<T>())
        {
            T prefab = GetUIPrefab<T>();
            if (prefab == null) return null;
            UICanvas canvas = prefab.gameObject.scene.IsValid() ? prefab : Instantiate(prefab, CanvasParentTF);
            uiCanvas[typeof(T)] = canvas;
        }

        return uiCanvas[typeof(T)] as T;
    }

    //Close all UI
    //dong tat ca UI ngay lap tuc -> tranh truong hop dang mo UI nao dong ma bi chen 2 UI cung mot luc
    public void CloseAll()
    {
        foreach (var item in uiCanvas)
        {
            if (item.Value != null && item.Value.gameObject.activeInHierarchy)
            {
                item.Value.CloseDirectly();
            }
        }
    }

    //Get prefab from resource
    //lay prefab tu Resources/UI
    private T GetUIPrefab<T>() where T : UICanvas
    {
        if (uiResources == null) return null;
        if (!uiCanvasPrefab.ContainsKey(typeof(T)))
        {
            for (int i = 0; i < uiResources.Length; i++)
            {
                if (uiResources[i] is T)
                {
                    uiCanvasPrefab[typeof(T)] = uiResources[i];
                    break;
                }
            }
        }

        if (!uiCanvasPrefab.ContainsKey(typeof(T))) return null;
        return uiCanvasPrefab[typeof(T)] as T;
    }


    #endregion

    #region Back Button

    private Dictionary<UICanvas, UnityAction> BackActionEvents = new Dictionary<UICanvas, UnityAction>();
    private List<UICanvas> backCanvas = new List<UICanvas>();
    UICanvas BackTopUI {
        get
        {
            UICanvas canvas = null;
            if (backCanvas.Count > 0)
            {
                canvas = backCanvas[backCanvas.Count - 1];
            }

            return canvas;
        }
    }


    private void LateUpdate()
    {
#if ENABLE_INPUT_SYSTEM
        bool backPressed = UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        bool backPressed = Input.GetKeyDown(KeyCode.Escape);
#else
        bool backPressed = false;
#endif
        if (backPressed && BackTopUI != null)
        {
            BackActionEvents[BackTopUI]?.Invoke();
        }
    }

    public void PushBackAction(UICanvas canvas, UnityAction action)
    {
        if (!BackActionEvents.ContainsKey(canvas))
        {
            BackActionEvents.Add(canvas, action);
        }
    }

    public void AddBackUI(UICanvas canvas)
    {
        if (!backCanvas.Contains(canvas))
        {
            backCanvas.Add(canvas);
        }
    }

    public void RemoveBackUI(UICanvas canvas)
    {
        backCanvas.Remove(canvas);
    }

    /// <summary>
    /// CLear backey when comeback index UI canvas
    /// </summary>
    public void ClearBackKey()
    {
        backCanvas.Clear();
    }

    #endregion
}
