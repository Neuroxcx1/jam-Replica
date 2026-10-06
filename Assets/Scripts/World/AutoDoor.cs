using UnityEngine;

// Puerta automatica: se abre cuando hay alguien o algo delante (jugador, replica o cuerpo) y se cierra sola.
// Un cuerpo dentro del sensor la deja atascada abierta. Cerrada tapa disparos y laseres.
public class AutoDoor : MonoBehaviour
{
    [SerializeField] Transform panel;
    [SerializeField] Collider2D blocker;
    [SerializeField] Vector2 sensorSize = new Vector2(5f, 3f);
    [SerializeField] float height = 3f;
    // rapida: si no, una replica que sale disparada se estampa contra ella antes de que se abra
    [SerializeField] float speed = 24f;

    float open;

    void FixedUpdate()
    {
        open = Mathf.MoveTowards(open, SomethingInFront() ? 1f : 0f, speed / height * Time.fixedDeltaTime);
        // la hoja sube y se mete en el techo
        panel.localPosition = new Vector3(0f, open * height, 0f);
        blocker.enabled = open < 0.95f;
    }

    bool SomethingInFront()
    {
        Vector2 center = (Vector2)transform.position + new Vector2(0f, height / 2f);
        foreach (Collider2D c in Physics2D.OverlapBoxAll(center, sensorSize, 0f))
            if (Hazard.IsVictim(c.gameObject) || c.TryGetComponent(out Body _)) return true;
        return false;
    }
}
