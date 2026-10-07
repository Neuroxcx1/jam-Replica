using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Puerta entre zonas (checkpoint). Esta abierta hasta que la cruzas, en cualquier sentido: entonces se cierra detras de ti, 
// el otro lado pasa a ser el checkpoint y se recargan las replicas.
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

    [Header("Checkpoint")]
    [Tooltip("ID unico de este checkpoint. Ejemplo: Nivel1_Zona01")]
    [SerializeField] string checkpointId;

    bool closed;
    float enteredFrom;

    public string CheckpointId => checkpointId;

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

        Vector3 checkpointPosition = spawnPoint != null ? spawnPoint.position : transform.position + new Vector3(side * 1.5f, 0.5f);
        
        player.SetCheckpoint(checkpointPosition);
        
        
        if (!string.IsNullOrEmpty(checkpointId)) GameProgress.SaveCheckpoint(checkpointId);

        
        StartCoroutine(Close());
    }

    float Side(Player player) { return Mathf.Sign(player.transform.position.x - transform.position.x); }

    IEnumerator Close()
    {
        barrier.Play(closingFrames, false);
        yield return new WaitUntil(() => barrier.Finished);

        barrier.Play(closedFrames, true);
        
        if (doorLight != null) doorLight.color = closedColor;

        CameraFollow.Shake(shakePixels, 0.2f);
    }

    // Se utiliza cuando cargamos una partida guardada.
    // La puerta aparece directamente cerrada, sin reproducir la animacion
    // ni provocar el temblor de camara.
    public void RestoreCheckpoint(Player player)
    {
       if (player == null)
            return;

        if (spawnPoint == null)
        {
            Debug.LogWarning(
                $"La ZoneDoor '{name}' no tiene Spawn Point. " +
                "No se puede restaurar correctamente el checkpoint."
            );
            return;
        }

        // Cerramos primero la puerta para que los triggers no intenten volver a procesar al jugador mientras lo colocamos.

        closed = true;
        blocker.enabled = true;

        // Mostrar directamente el estado cerrado.
        barrier.Play(closedFrames, true);

        if (doorLight != null)
            doorLight.color = closedColor;
        
        Vector3 position = spawnPoint.position;

        // Convertimos este punto en el checkpoint del Player.
        player.SetCheckpoint(position);

        // Colocar exactamente al jugador allí.
        player.transform.position = position;
        player.Rb.position = position;
        player.Rb.linearVelocity = Vector2.zero;
        player.ClearJumpTimers();

        Debug.Log($"Checkpoint restaurado: {checkpointId}");
 
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
