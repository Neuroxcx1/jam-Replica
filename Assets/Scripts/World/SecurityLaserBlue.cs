using UnityEngine;
using UnityEngine.Rendering.Universal;

// Laser de seguridad. Dispara hacia transform.up y se corta en lo primero que toca.
// Si es el jugador o una replica la mata, y el cuerpo que deja tapa el rayo.
public class SecurityLaserBlue : MonoBehaviour
{
    [SerializeField] float maxLength = 20f;
    [SerializeField] LayerMask blockers;
    [SerializeField] SpriteRenderer beam;
    [SerializeField] Transform impact;
    [SerializeField] Light2D beamLight;
    [SerializeField] Transform receiver;

    [Header("Intermitente (onTime 0 = siempre encendido)")]
    [Tooltip("Segundos encendido (0 = siempre encendido)")]
    [SerializeField] float onTime;
    [Tooltip("Segundos apagado")]
    [SerializeField] float offTime = 1f;
    [Tooltip("Retraso: con varios seguidos, ponles retrasos distintos")]
    [SerializeField] float startDelay;
    [Tooltip("Estado del laser")]
    [SerializeField] bool on;

    float timer;

    // el receptor va donde el rayo choca con la pared: asi el prefab vale para cualquier altura
    void Start()
    {
        if (receiver == null) return;
        RaycastHit2D wall = Physics2D.Raycast(transform.position, transform.up, maxLength, LayerMask.GetMask("Ground"));
        if (wall) receiver.position = wall.point;
    }

    void Update()
    {
        //timer += Time.deltaTime;
        //on = onTime <= 0f || (timer + startDelay) % (onTime + offTime) < onTime;
        beam.enabled = on;
        impact.gameObject.SetActive(on);
        beamLight.enabled = on;
        if (!on) return;

        Vector2 origin = transform.position;
        Vector2 direction = transform.up;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxLength, blockers);
        float length = hit ? hit.distance : maxLength;

        if (hit && Hazard.IsVictim(hit.collider.gameObject))
        {
            // lo centra en el rayo antes de matarlo para que su cuerpo caiga justo encima del emisor
            Transform victim = hit.collider.transform;
            Vector2 onBeam = origin + direction * Vector2.Dot((Vector2)victim.position - origin, direction);
            victim.position = new Vector3(onBeam.x, onBeam.y, victim.position.z);
            Hazard.Kill(victim.gameObject);
        }

        beam.size = new Vector2(beam.size.x, length);
        beam.transform.localPosition = new Vector3(0f, length / 2f, 0f);
        impact.localPosition = new Vector3(0f, length, 0f);
        beamLight.transform.localPosition = beam.transform.localPosition;
        beamLight.pointLightOuterRadius = length / 2f + 1f;
    }

    public void SetTurnOff(bool now)
    {
        on = !now;
    }
}
