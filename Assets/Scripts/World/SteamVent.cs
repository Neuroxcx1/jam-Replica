using UnityEngine;

// Tuberia rota: suelta vapor a ratos hacia transform.up. Quema al jugador y a las replicas;
// lo que tenga delante (pared, caja, cuerpo) corta el chorro.
// Solo quema donde ya ha llegado el vapor: el chorro avanza desde la boca y al cerrarse se aleja.
public class SteamVent : MonoBehaviour
{
    [Tooltip("Hasta donde llega como mucho; si antes hay una pared o algo solido, se corta ahi")]
    [SerializeField] float length = 4f;
    [SerializeField] float width = 0.7f;
    [Tooltip("Segundos echando vapor")]
    [SerializeField] float onTime = 1f;
    [Tooltip("Segundos parado")]
    [SerializeField] float offTime = 2f;
    [Tooltip("Retraso: con varios seguidos, ponles retrasos distintos para que no salgan a la vez")]
    [SerializeField] float startDelay;
    [SerializeField] float jetSpeed = 8f;
    // antes de salir suelta unos soplidos de aviso
    [SerializeField] float warningTime = 0.6f;
    [SerializeField] LayerMask blockers;
    [SerializeField] ParticleSystem steam;

    [Header("Audio")]
    [Tooltip("Velocidad a la que el sonido se desvanece (Fade Out) al apagarse")]
    [SerializeField] float fadeOutSpeed = 2f;

    float timer;
    AudioSource audioSource;
    float maxVolume; // Guardará el volumen original establecido en el Inspector

    void Awake()
    {
        var main = steam.main;
        main.startSpeed = jetSpeed;

        // Obtenemos el componente y guardamos el volumen original
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            maxVolume = audioSource.volume;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = (timer + startDelay) % (onTime + offTime);
        bool on = t < onTime;
        bool warning = !on && t > onTime + offTime - warningTime;

        // --- CONTROL DE AUDIO CON FADE OUT ---
        if (audioSource != null)
        {
            if (on)
            {
                // Restaura el volumen máximo y reproduce si no estaba sonando
                audioSource.volume = maxVolume;
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
            else
            {
                // Si el vapor se detuvo pero el audio sigue sonando, reducimos el volumen gradualmente
                if (audioSource.isPlaying)
                {
                    audioSource.volume -= fadeOutSpeed * Time.deltaTime;

                    // Cuando el volumen llega a 0, detenemos el clip por completo
                    if (audioSource.volume <= 0f)
                    {
                        audioSource.Stop();
                        audioSource.volume = maxVolume; // Lo preparamos para el siguiente ciclo
                    }
                }
            }
        }
        // -------------------------------------

        Vector2 origin = transform.position;
        Vector2 direction = transform.up;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, length, blockers);
        float reach = hit ? hit.distance : length;

        // el chorro de verdad llega hasta donde se corta; el aviso son soplidos cortos en la boca
        var emission = steam.emission;
        emission.enabled = on || warning;
        emission.rateOverTime = on ? 45f : 8f;
        var main = steam.main;
        main.startLifetime = on ? reach / jetSpeed : 0.12f;

        // tramo que quema: desde la cola (sale de la boca al cerrarse) hasta el frente (avanza al abrirse)
        float front = Mathf.Min(reach, jetSpeed * t);
        float tail = Mathf.Min(reach, jetSpeed * Mathf.Max(0f, t - onTime));
        if (front - tail < 0.05f) return;

        Vector2 center = origin + direction * (front + tail) / 2f;
        foreach (Collider2D c in Physics2D.OverlapBoxAll(center, new Vector2(width, front - tail), transform.eulerAngles.z))
            Hazard.Kill(c.gameObject);
    }
}