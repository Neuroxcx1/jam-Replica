using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Salida del laboratorio. No hay puntuacion: al cruzarla todo se funde a negro y ya.
public class FinalDoor : MonoBehaviour
{
    [SerializeField] float fadeTime = 2.5f;
    [SerializeField] string message = "Escapaste.";

    float fade;

    void Awake()
    {
        // apagado hasta llegar, asi OnGUI no se ejecuta cada frame
        enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (enabled || !other.TryGetComponent(out Player player)) return;

        enabled = true;
        player.SetAlive(false);
        // el jugador ya no hace nada (tampoco reiniciar la zona); la T la mira esta puerta
        player.enabled = false;
    }

    void Update()
    {
        fade = Mathf.Min(1f, fade + Time.deltaTime / fadeTime);
        if (fade >= 1f && InputSystem.actions.FindAction("Restart").WasPressedThisFrame())
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnGUI()
    {
        GUI.color = new Color(0f, 0f, 0f, fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fade * 2f - 1f));
        var style = new GUIStyle(GUI.skin.label) { fontSize = Screen.height / 16, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), message, style);

        style.fontSize = Screen.height / 40;
        GUI.Label(new Rect(0, Screen.height * 0.6f, Screen.width, Screen.height * 0.1f), "T para empezar de nuevo", style);
    }
}
