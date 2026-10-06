using UnityEngine;

// Tuberia rota: suelta vapor a ratos hacia transform.up. Quema al jugador y a las replicas;
// lo que tenga delante (pared, caja, cuerpo) corta el chorro.
// Solo quema donde ya ha llegado el vapor: el chorro avanza desde la boca y al cerrarse se aleja.
public class SteamVent : MonoBehaviour
{
    [SerializeField] float length = 4f;
    [SerializeField] float width = 0.7f;
    [SerializeField] float onTime = 1f;
    [SerializeField] float offTime = 2f;
    [SerializeField] float startDelay;
    [SerializeField] float jetSpeed = 8f;
    // antes de salir suelta unos soplidos de aviso
    [SerializeField] float warningTime = 0.6f;
    [SerializeField] LayerMask blockers;
    [SerializeField] ParticleSystem steam;

    float timer;

    void Awake()
    {
        var main = steam.main;
        main.startSpeed = jetSpeed;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = (timer + startDelay) % (onTime + offTime);
        bool on = t < onTime;
        bool warning = !on && t > onTime + offTime - warningTime;

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
