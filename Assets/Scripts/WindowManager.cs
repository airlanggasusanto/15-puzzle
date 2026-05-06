using UnityEngine;

public static class WindowManager
{
    private const int MinWidth = 480;
    private const int MinHeight = 480;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
    #if !UNITY_EDITOR && UNITY_STANDALONE_WIN

        WindowAPI.Set(MinWidth, MinHeight);

        WindowAPI.SetAspectRatio(1, 1);

        Application.quitting += OnQuit;
    #endif
    }

    private static void OnQuit()
    {
    #if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        WindowAPI.Reset();
        WindowAPI.ClearAspectRatio();
    #endif
    }
}