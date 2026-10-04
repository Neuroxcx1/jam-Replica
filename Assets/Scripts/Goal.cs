using UnityEngine;

public class Goal : MonoBehaviour
{
    void Awake()
    {
        // apagado hasta llegar, asi OnGUI no se ejecuta cada frame
        enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Player _)) enabled = true;
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "¡Nivel completado!", style);
    }
}
