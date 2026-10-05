using UnityEngine;

public class Goal : MonoBehaviour
{
    [SerializeField] GameObject victoryPanel;
    [SerializeField] Animator victoryAnimator;
    bool completed = false;

    void Awake()
    {
        // apagado hasta llegar, asi OnGUI no se ejecuta cada frame
        //enabled = false;
        victoryPanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //if (other.TryGetComponent(out Player _)) enabled = true;
        if (completed) return;

        if (other.TryGetComponent<Player>(out _))
        {
            completed = true;

            victoryPanel.SetActive(true);
            victoryAnimator.SetTrigger("Open");
        }
    }

    /*void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "¡Nivel completado!", style);
    }*/
}
