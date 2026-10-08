using UnityEngine;
using UnityEngine.Rendering.Universal;

// Suelo electrificado de los corrales de especimenes: con corriente mata al que lo pisa.
// Los cuerpos no conducen, asi que subido a uno no te pasa nada. La corriente va a pulsos.
// Necesita un Hazard y un collider trigger (una franja fina pegada al suelo) en el mismo objeto.
public class ElectricFloor : MonoBehaviour
{
    [Tooltip("Segundos con corriente")]
    [SerializeField] float onTime = 2.5f;
    [Tooltip("Segundos sin corriente (0 = siempre con corriente)")]
    [SerializeField] float offTime = 1.5f;
    [SerializeField] SpriteRenderer arcs;
    [SerializeField] Light2D glow;

    Collider2D zone;
    float timer;

    // Referencia al AudioSource que agregaste en el Inspector
    AudioSource audioSource;

    void Awake()
    {
        zone = GetComponent<Collider2D>();
        // Obtenemos el componente al iniciar
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        bool on = timer % (onTime + offTime) < onTime;
        zone.enabled = on;
        arcs.enabled = on;
        glow.enabled = on;

        // --- CONTROL DE AUDIO ---
        if (audioSource != null)
        {
            // Si la trampa está encendida y el sonido NO está sonando, reprodúcelo
            if (on && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
            // Si la trampa está apagada y el sonido SÍ está sonando, detenlo
            else if (!on && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}