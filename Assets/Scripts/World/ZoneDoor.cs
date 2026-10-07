using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Puerta entre zonas (checkpoint). Esta abierta hasta que la cruzas, en cualquier sentido: entonces se cierra
// detras de ti, el otro lado pasa a ser el checkpoint y se recargan las replicas.
// Va en un hueco de 2 de alto en una pared (el recuadro rojo de la escena marca las casillas que hay que borrar).
public class ZoneDoor : MonoBehaviour
{
    [SerializeField] Collider2D blocker;
    [Tooltip("Opcional: donde reapareces si mueres en la zona nueva. Si lo dejas vacio, justo pasada la puerta")]
    [SerializeField] Transform spawnPoint;
    [SerializeField] SpriteLoop barrier;
    [SerializeField] Sprite[] closingFrames;
    [SerializeField] Sprite[] closedFrames;
    [SerializeField] Light2D doorLight;
    [SerializeField] Color closedColor = new Color(1f, 0.25f, 0.2f);
    [SerializeField] float shakePixels = 3f;

    bool closed;
    float enteredFrom;

    void Awake()
    {
        blocker.enabled = false;
    }

    // el trigger ocupa la puerta y una casilla a cada lado: se cierra cuando sales de el por el otro lado
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!closed && other.TryGetComponent(out Player player)) enteredFrom = Side(player);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (closed || !other.TryGetComponent(out Player player)) return;
        float side = Side(player);
        if (side == enteredFrom) return;

        closed = true;
        blocker.enabled = true;
        player.SetCheckpoint(spawnPoint != null ? spawnPoint.position : transform.position + new Vector3(side * 1.5f, 0.5f));
        StartCoroutine(Close());
    }

    float Side(Player player) => Mathf.Sign(player.transform.position.x - transform.position.x);

    IEnumerator Close()
    {
        barrier.Play(closingFrames, false);
        yield return new WaitUntil(() => barrier.Finished);

        barrier.Play(closedFrames, true);
        if (doorLight != null) doorLight.color = closedColor;
        CameraFollow.Shake(shakePixels, 0.2f);
    }

    void OnDrawGizmos()
    {
        // el hueco que tiene que tener la pared
        Gizmos.color = new Color(1f, 0.3f, 0.3f);
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1f, 2f, 0f));
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);
        }
    }
}
