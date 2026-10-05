using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Salida del laboratorio. No hay puntuacion: al cruzarla se para el jugador y sale el panel de victoria.
public class FinalDoor : MonoBehaviour
{
    [Header("Victory UI")]
    [SerializeField] GameObject victoryPanel;
    [SerializeField] Animator victoryAnimator;

    bool completed = false;

    void Awake()
    {
        victoryPanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (completed) return;

        if (!other.TryGetComponent<Player>(out Player player))
            return;

        completed = true;

        // Detener al jugador
        player.SetAlive(false);
        player.enabled = false;

        // Mostrar el panel
        victoryPanel.SetActive(true);

        // Reproducir la animacion de apertura
        victoryAnimator.SetTrigger("Open");
    }

    void Update()
    {
        if (!completed)
            return;

        // el jugador ya no hace nada, asi que la K (reiniciar) la mira esta puerta
        if (InputSystem.actions.FindAction("Restart").WasPressedThisFrame())
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
