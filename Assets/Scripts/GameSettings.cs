using UnityEngine;

public static class GameSettings
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        // sin vsync va a mil fps sin ritmo fijo y la pantalla hace tearing al mover la camara
        QualitySettings.vSyncCount = 1;
    }
}
