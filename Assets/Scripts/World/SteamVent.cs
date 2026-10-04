using UnityEngine;

// Tuberia rota: suelta vapor a ratos hacia transform.up. Quema al jugador y a las replicas;
// lo que tenga delante (pared, caja, cuerpo) corta el chorro.
public class SteamVent : MonoBehaviour
{
    [SerializeField] float length = 4f;
    [SerializeField] float width = 0.7f;
    [SerializeField] float onTime = 1f;
    [SerializeField] float offTime = 2f;
    [SerializeField] float startDelay;
    [SerializeField] LayerMask blockers;
    [SerializeField] ParticleSystem steam;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        bool on = (timer + startDelay) % (onTime + offTime) < onTime;
        var emission = steam.emission;
        emission.enabled = on;
        if (!on) return;

        Vector2 origin = transform.position;
        Vector2 direction = transform.up;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, length, blockers);
        float reach = hit ? hit.distance : length;

        // las particulas viven lo justo para llegar hasta donde se corta el chorro
        var main = steam.main;
        main.startLifetime = reach / main.startSpeed.constant;

        foreach (Collider2D c in Physics2D.OverlapBoxAll(origin + direction * reach / 2f, new Vector2(width, reach), transform.eulerAngles.z))
            Hazard.Kill(c.gameObject);
    }
}
