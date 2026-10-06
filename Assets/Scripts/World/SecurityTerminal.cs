using UnityEngine;
using UnityEngine.Rendering.Universal;

// Computador de seguridad: mientras haya algo en la placa de delante (una replica, un cuerpo o tu)
// su puerta de seguridad esta abierta. Para pasar hay que dejar un clon.
// Al abrirse la pantalla se pone verde, el marco y la placa se encienden y el cable del techo lleva la senal a la puerta.
// Abre la puerta de seguridad mas cercana, o la que le pongas en Door. El cable va solo del computador a esa puerta,
// tambien en el editor: al colocarlos se ve cual abre cual. Si el camino no sirve, se quita Auto Cable y se dibuja a mano.
[ExecuteAlways]
public class SecurityTerminal : MonoBehaviour
{
    [Tooltip("La puerta que abre. Vacio = la puerta de seguridad mas cercana")]
    [SerializeField] SecurityDoor door;
    [Tooltip("Quitalo para llevar el cable a mano: en su hijo \"Cable a la puerta\", Line Renderer > Edit Points mueve los puntos en la escena y Create Points anade mas")]
    [SerializeField] bool autoCable = true;
    [Tooltip("Donde hay que dejar el clon, respecto al computador (el recuadro verde)")]
    [SerializeField] Vector2 padOffset = new Vector2(0f, 0.5f);
    [SerializeField] Vector2 padSize = new Vector2(1.4f, 1f);
    [SerializeField] Light2D screenLight;
    [SerializeField] Color lockedColor = new Color(1f, 0.25f, 0.2f);
    [SerializeField] Color openColor = new Color(0.4f, 1f, 0.55f);

    [Header("Aviso visual")]
    [SerializeField] SpriteLoop screen;
    [SerializeField] Sprite[] lockedFrames;
    [SerializeField] Sprite[] openFrames;
    [SerializeField] SpriteRenderer[] marks;   // marco y placa: blancos y en verde al abrir
    [SerializeField] LineRenderer cable;

    bool held;

    void Start()
    {
        if (!Application.isPlaying) return;
        if (door == null) door = NearestDoor();
        LayCable();
        Show();
    }

    // en el editor el cable sigue al computador y a la puerta cuando se mueven
    void Update()
    {
        if (Application.isPlaying) return;
        LayCable();
        if (cable != null) cable.startColor = cable.endColor = lockedColor;
    }

    // el cable sale de la esquina de atras del computador, sube al techo y va por el hasta la puerta
    void LayCable()
    {
        SecurityDoor target = door != null ? door : NearestDoor();
        if (!autoCable || cable == null || target == null) return;
        Vector2 start = (Vector2)transform.position + new Vector2(0.4f, 1f);
        RaycastHit2D roof = Physics2D.Raycast(start, Vector2.up, 20f, LayerMask.GetMask("Ground"));
        float y = (roof ? roof.point.y : start.y + 3f) - 0.15f;
        cable.positionCount = 3;
        cable.SetPositions(new Vector3[] { start, new Vector2(start.x, y), new Vector2(target.transform.position.x, y) });
    }

    void FixedUpdate()
    {
        bool now = false;
        foreach (Collider2D c in Physics2D.OverlapBoxAll((Vector2)transform.position + padOffset, padSize, 0f))
            if (Hazard.IsVictim(c.gameObject) || c.TryGetComponent(out Body _)) now = true;

        if (door != null) door.SetOpen(now);
        if (now == held) return;
        held = now;
        Show();
        if (held) CameraFollow.Shake(1.5f, 0.1f);
    }

    void Show()
    {
        Color color = held ? openColor : lockedColor;
        screenLight.color = color;
        if (screen != null && openFrames.Length > 0) screen.Play(held ? openFrames : lockedFrames, true);
        foreach (SpriteRenderer mark in marks) mark.color = held ? openColor : Color.white;
        if (cable != null) cable.startColor = cable.endColor = color;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position + padOffset, padSize);
        SecurityDoor target = door != null ? door : NearestDoor();
        if (target != null) Gizmos.DrawLine(transform.position, target.transform.position);
    }

    SecurityDoor NearestDoor()
    {
        SecurityDoor nearest = null;
        foreach (SecurityDoor candidate in FindObjectsByType<SecurityDoor>())
            if (nearest == null || Vector2.Distance(candidate.transform.position, transform.position) < Vector2.Distance(nearest.transform.position, transform.position))
                nearest = candidate;
        return nearest;
    }
}
