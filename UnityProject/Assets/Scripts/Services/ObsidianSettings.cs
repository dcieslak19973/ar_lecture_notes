using UnityEngine;

/// <summary>
/// Persists the Obsidian vault path using PlayerPrefs.
/// Default resolves to /storage/emulated/0/Documents/ObsidianVault/AR Lecture Notes on Android.
/// </summary>
public static class ObsidianSettings
{
    private const string VaultPathKey = "ObsidianVaultPath";

    public static string VaultPath
    {
        get
        {
            var saved = PlayerPrefs.GetString(VaultPathKey, string.Empty);
            if (!string.IsNullOrEmpty(saved)) return saved;
            return DefaultVaultPath;
        }
        set
        {
            PlayerPrefs.SetString(VaultPathKey, value);
            PlayerPrefs.Save();
        }
    }

    private static string DefaultVaultPath
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var env = new AndroidJavaClass("android.os.Environment");
                using var docDir = env.CallStatic<AndroidJavaObject>(
                    "getExternalStoragePublicDirectory",
                    env.GetStatic<AndroidJavaObject>("DIRECTORY_DOCUMENTS").Call<string>("getAbsolutePath"));
                var docs = env.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory",
                    "Documents");
                var path = docs.Call<string>("getAbsolutePath");
                return path + "/ObsidianVault/AR Lecture Notes";
            }
            catch
            {
                return "/storage/emulated/0/Documents/ObsidianVault/AR Lecture Notes";
            }
#else
            return System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                "ObsidianVault", "AR Lecture Notes");
#endif
        }
    }
}
