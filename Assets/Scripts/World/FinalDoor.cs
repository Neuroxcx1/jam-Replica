using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Salida del nivel: al cruzarla se pasa a la escena de Next Scene. En la ultima puerta del juego Next Scene se deja
// vacio: se para el jugador y sale el panel de victoria (el "Ganaste" del HUD: si no se enlaza a mano, lo busca solo).
// Va en un hueco de 2 de alto en una pared (el recuadro rojo de la escena).
public class FinalDoor : MonoBehaviour
{
    [Tooltip("El nombre de la escena siguiente (tiene que estar en Build Settings). Vacio = final del juego")]
    [SerializeField] string nextScene;

    [Header("Victory UI")]
    [SerializeField] GameObject victoryPanel;
    [SerializeField] Animator victoryAnimator;

    bool completed = false;

    void Awake()
    {
        if (victoryPanel == null)
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name == "Ganaste") victoryPanel = t.gameObject;
        if (victoryPanel != null)
        {
            if (victoryAnimator == null) victoryAnimator = victoryPanel.GetComponent<Animator>();
            victoryPanel.SetActive(false);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f);
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1f, 2f, 0f));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (completed) return;

        if (!other.TryGetComponent<Player>(out Player player))
            return;

        completed = true;

        if (!string.IsNullOrEmpty(nextScene))
        {
            SceneDoor.Go(nextScene);
            return;
        }

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

        // el jugador ya no hace nada, asi que la K (reiniciar) la mira esta puerta
        if (InputSystem.actions.FindAction("Restart").WasPressedThisFrame())
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
