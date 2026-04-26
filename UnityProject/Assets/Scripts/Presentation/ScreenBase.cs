using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Base class for all screen controllers.
/// Handles the async show/hide lifecycle and provides safe UI dispatch.
/// </summary>
public abstract class ScreenBase : MonoBehaviour
{
    protected virtual async void Start()
    {
        try
        {
            await OnShowAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] Error in OnShowAsync: {ex}");
        }
    }

    /// <summary>Called when the screen becomes visible. Load data here.</summary>
    protected virtual Task OnShowAsync() => Task.CompletedTask;

    /// <summary>Run an async task and show an error if it fails.</summary>
    protected async void RunAsync(Func<Task> action, string errorContext = "")
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] {errorContext}: {ex.Message}");
        }
    }
}
