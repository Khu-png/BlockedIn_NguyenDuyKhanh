using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T ins;

    public static T Ins => ins;

    protected void RegisterSingleton(T instance)
    {
        if (ins == null) ins = instance;
    }

    protected virtual void OnDestroy()
    {
        if (ins == this) ins = null;
    }
}
