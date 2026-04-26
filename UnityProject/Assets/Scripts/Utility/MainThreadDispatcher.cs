using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that allows code running on non-Unity threads
/// to enqueue actions and coroutines for execution on the main thread.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private readonly Queue<Action> _queue = new Queue<Action>();
    private readonly object _lock = new object();

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MainThreadDispatcher");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<MainThreadDispatcher>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        List<Action> toInvoke = null;
        lock (_lock)
        {
            if (_queue.Count > 0)
            {
                toInvoke = new List<Action>(_queue);
                _queue.Clear();
            }
        }
        if (toInvoke != null)
            foreach (var action in toInvoke)
                action?.Invoke();
    }

    public void Enqueue(Action action)
    {
        lock (_lock) { _queue.Enqueue(action); }
    }

    public new Coroutine StartCoroutine(IEnumerator routine)
    {
        return base.StartCoroutine(routine);
    }
}
