using UnityEngine;

// Muestra de mutageno: al cogerla tienes una replica mas, tambien en el maximo (hasta reiniciar el nivel).
// Necesita un collider trigger.
public class ReplicaPickup : MonoBehaviour
{
    [Tooltip("Cuantas replicas de mas da")]
    [SerializeField] int amount = 1;
    [SerializeField] Transform visual;
    [SerializeField] GameObject collectEffect;

    [Header("Audio")]
    [Tooltip("Sonido que se reproduce al recolectar la muestra")]
    [SerializeField] AudioClip sonidoRecoger;

    void Update()
    {
        // flota arriba y abajo para que se vea que se puede coger
        visual.localPosition = Vector3.up * Mathf.Sin(Time.time * 2.5f) * 0.12f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Player player)) return;

        // --- AUDIO RECOLECTAR ---
        // Se llama al gestor global antes de destruir el objeto
        if (sonidoRecoger != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.ReproducirSonido(sonidoRecoger);
        }
        // ------------------------

        player.AddReplicas(amount);
        if (collectEffect != null) Instantiate(collectEffect, player.transform.position, Quaternion.identity);
        CameraFollow.Shake(2f, 0.12f);

        Destroy(gameObject);
    }
}