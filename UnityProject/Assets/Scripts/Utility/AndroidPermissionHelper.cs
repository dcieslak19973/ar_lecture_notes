using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Requests Android runtime permissions and returns a Task that resolves
/// when the user has responded. Falls through immediately on non-Android builds.
/// </summary>
public static class AndroidPermissionHelper
{
    public static Task<bool> RequestPermissionAsync(string permission)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(permission))
            return Task.FromResult(true);

        var tcs = new TaskCompletionSource<bool>();
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted      += _ => tcs.TrySetResult(true);
        callbacks.PermissionDenied       += _ => tcs.TrySetResult(false);
        callbacks.PermissionDeniedAndDontAskAgain += _ => tcs.TrySetResult(false);
        Permission.RequestUserPermission(permission, callbacks);
        return tcs.Task;
#else
        return Task.FromResult(true);
#endif
    }

    public static bool RequiresExternalStoragePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using var version = new AndroidJavaClass("android.os.Build$VERSION");
        var sdkInt = version.GetStatic<int>("SDK_INT");
        return sdkInt < 29;
#else
        return false;
#endif
    }
}
