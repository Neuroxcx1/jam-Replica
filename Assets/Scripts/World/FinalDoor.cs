using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Salida del nivel: al cruzarla el jugador se para, se funde a negro y carga la escena siguiente.
// En la ultima puerta del juego se deja sin escena: sale el panel de victoria (el "Ganaste" del HUD, lo busca solo).
// Va en un hueco de 2 de alto en una pared (el recuadro rojo de la escena).
public class FinalDoor : MonoBehaviour
{
#if UNITY_EDITOR
    [Tooltip("Arrastra aqui la escena siguiente (se anade sola a Build Settings). Vacio = final del juego")]
    [SerializeField] UnityEditor.SceneAsset nextSceneAsset;
#endif
    [SerializeField, HideInInspector] string nextScene;

    [Header("Victory UI")]
    [SerializeField] GameObject victoryPanel;
    [SerializeField] Animator victoryAnimator;

    bool completed = false;

#if UNITY_EDITOR
    void OnValidate()
    {
        nextScene = nextSceneAsset != null ? nextSceneAsset.name : "";
        if (nextSceneAsset == null) return;

        // sin estar en Build Settings la escena no se puede cargar
        string path = UnityEditor.AssetDatabase.GetAssetPath(nextSceneAsset);
        var scenes = new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == path)) return;
        scenes.Add(new UnityEditor.EditorBuildSettingsScene(path, true));
        UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
    }
#endif

    void Awake()
    {
        // apagado hasta llegar, asi OnGUI no se ejecuta cada frame
        victoryPanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (completed) return;

        if (!other.TryGetComponent<Player>(out Player player))
            return;

        // a otra escena: el jugador se queda quieto mientras se funde a negro
        if (!string.IsNullOrEmpty(nextScene))
        {
            if (!SceneDoor.Go(nextScene)) return;
            completed = true;
            player.enabled = false;
            player.Rb.linearVelocity = Vector2.zero;
            player.Rb.simulated = false;
            return;
        }

        completed = true;

        // Detener al jugador
        player.SetAlive(false);
        player.enabled = false;

        // Mostrar el panel (el del HUD de la escena: hay que enlazarlo en el inspector)
        if (victoryPanel != null) victoryPanel.SetActive(true);

        // Reproducir la animacion de apertura
        if (victoryAnimator != null) victoryAnimator.SetTrigger("Open");
    }

    void Update()
    {
        if (!completed || !string.IsNullOrEmpty(nextScene))
            return;

        // Reiniciar el nivel al pulsar la acción Restart.
        var restart = InputSystem.actions.FindAction("Restart");

        if (restart != null && restart.WasPressedThisFrame())
        {
            SceneManager.LoadScene(
                SceneManager.GetActiveScene().buildIndex
            );
        }
    }
}
