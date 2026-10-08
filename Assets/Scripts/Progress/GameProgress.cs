using UnityEngine;
using UnityEngine.SceneManagement;

// Guarda únicamente el progreso del jugador:
// escena actual + último checkpoint alcanzado.
// No guarda enemigos, objetos, puzzles, replicas, etc.

public static class GameProgress
{
    const string HasSaveKey = "Game.HasSave";
    const string SceneKey = "Game.Scene";
    const string CheckpointKey = "Game.Checkpoint";
    const string MaxReplicasKey = "Game.MaxReplicas";

    static bool restoring;

    // Nos aseguramos de registrar el evento una sola vez al iniciar el juego.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        restoring = false;
    }

    public static bool HasSave =>
        PlayerPrefs.GetInt(HasSaveKey, 0) == 1;

    public static void SaveCheckpoint(string checkpointId,  Player player)
    {
        PlayerPrefs.SetInt(HasSaveKey, 1);
        PlayerPrefs.SetString(SceneKey, SceneManager.GetActiveScene().name);
        PlayerPrefs.SetString(CheckpointKey, checkpointId);
        
        if (player != null) PlayerPrefs.SetInt(MaxReplicasKey, player.MaxReplicas);
        
        PlayerPrefs.Save();

        Debug.Log($"Progreso guardado: {SceneManager.GetActiveScene().name} / {checkpointId}");
    }

    // Se usa al entrar a una escena nueva mediante FinalDoor.
    // Todavía no hay checkpoint dentro de esa escena, así que el jugador
    // continuará desde el inicio de la escena.
    public static void SaveSceneStart(string sceneName)
    {
        PlayerPrefs.SetInt(HasSaveKey, 1);
        PlayerPrefs.SetString(SceneKey, sceneName);
        PlayerPrefs.DeleteKey(CheckpointKey);
        PlayerPrefs.Save();

        Debug.Log($"Progreso guardado al inicio de escena: {sceneName}");
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(HasSaveKey);
        PlayerPrefs.DeleteKey(SceneKey);
        PlayerPrefs.DeleteKey(CheckpointKey);
        PlayerPrefs.DeleteKey(MaxReplicasKey);
        PlayerPrefs.Save();

        Debug.Log("Progreso eliminado.");
    }

    public static void ContinueGame()
    {
        if (!HasSave)
            return;

        string sceneName = PlayerPrefs.GetString(SceneKey, "");

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Hay un guardado, pero no tiene escena.");
            return;
        }

        restoring = true;
        Time.timeScale = 1f;

        SceneManager.LoadScene(sceneName);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!restoring)
            return;

        restoring = false;

        string checkpointId = PlayerPrefs.GetString(CheckpointKey, "");

        // Si no hay checkpoint, simplemente dejamos al jugador en el inicio normal de la escena.
        if (string.IsNullOrEmpty(checkpointId))
            return;

        Player player = Object.FindAnyObjectByType<Player>();

        if (player == null)
        {
            Debug.LogWarning("No se encontró Player al restaurar el checkpoint.");
            return;
        }

        int savedMaxReplicas = PlayerPrefs.GetInt(
            MaxReplicasKey,
            player.MaxReplicas
        );

        player.RestoreMaxReplicas(savedMaxReplicas);

        ZoneDoor[] doors = Object.FindObjectsByType<ZoneDoor>(FindObjectsInactive.Include);

        foreach (ZoneDoor door in doors)
        {
            if (door.CheckpointId == checkpointId)
            {
                door.RestoreCheckpoint(player);
                return;
            }
        }


        Debug.LogWarning(
            $"No se encontró la ZoneDoor con checkpoint ID '{checkpointId}' " +
            $"en la escena '{scene.name}'."
        );
    }

}
    

