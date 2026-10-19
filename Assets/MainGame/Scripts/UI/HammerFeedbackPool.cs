using System;
using UnityEngine;

public sealed class HammerFeedbackPool : IDisposable
{
    readonly HammerFeedback instance;
    bool rented, disposed;

    public HammerFeedbackPool(HammerFeedback prefab)
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));
        instance = UnityEngine.Object.Instantiate(prefab);
        instance.ResetForReuse();
        instance.gameObject.SetActive(false);
    }
    public HammerFeedback Rent()
    {
        if (rented) throw new InvalidOperationException("The hammer feedback is already in use.");
        if (disposed || instance == null) throw new ObjectDisposedException(nameof(HammerFeedbackPool));
        rented = true;
        instance.ResetForReuse();
        instance.gameObject.SetActive(true);
        return instance;
    }
    public void Return(HammerFeedback feedback)
    {
        if (disposed || feedback == null) return;
        if (feedback != instance) throw new ArgumentException("The hammer belongs to another pool.", nameof(feedback));
        instance.ResetForReuse();
        instance.gameObject.SetActive(false);
        rented = false;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (instance != null) UnityEngine.Object.Destroy(instance.gameObject);
        rented = false;
    }
}
