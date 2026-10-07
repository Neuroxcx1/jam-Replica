using UnityEngine;
using UnityEngine.Rendering.Universal;

// Torreta de seguridad: apunta a lo mas cercano que vea delante (el jugador o una replica),
// carga un momento y dispara. Paredes, cajas y cuerpos la tapan; las replicas sirven de señuelo.
// Esta dibujada mirando a la izquierda: Facing Right la voltea (o Scale X en -1). Al seleccionarla se ve su cono.
[ExecuteAlways]
public class Turret : MonoBehaviour
{
    enum Mode { Idle, Charging, Locked, Cooldown }

    [SerializeField] Transform muzzle;
    [Tooltip("Marcalo para que mire y dispare a la derecha")]
    [SerializeField] bool facingRight;
    [SerializeField] float range = 13f;
    [SerializeField] float maxAngle = 50f;
    [SerializeField] float chargeTime = 0.8f;
    // los ultimos instantes deja de seguirte: da tiempo a esconderse
    [SerializeField] float lockTime = 0.2f;
    [SerializeField] float cooldown = 1f;
    [SerializeField] LayerMask sightMask;
    [SerializeField] LayerMask targetMask;

    [Header("Visual")]
    [SerializeField] SpriteRenderer body;
    [SerializeField] Sprite idleSprite;
    [SerializeField] Sprite[] chargeFrames;
    [SerializeField] LineRenderer sight;
    [SerializeField] LineRenderer bolt;
    [SerializeField] Light2D flash;
    [SerializeField] Color sightColor = new Color(1f, 0.2f, 0.15f, 0.7f);
    [SerializeField] float shakePixels = 2f;

    Mode mode;
    float timer;
    float boltTimer;
    Vector2 aim;

    void Awake()
    {
        Face();
    }

    void Update()
    {
        // en el editor solo se voltea, para ver hacia donde mira al colocarla
        if (!Application.isPlaying)
        {
            Face();
            return;
        }

        timer -= Time.deltaTime;
        boltTimer -= Time.deltaTime;
        bolt.enabled = boltTimer > 0f;
        flash.enabled = boltTimer > 0f;

        switch (mode)
        {
            case Mode.Idle:
                if (FindTarget() != null) Change(Mode.Charging, chargeTime);
                break;

            case Mode.Charging:
                Collider2D target = FindTarget();
                if (target == null)
                {
                    Change(Mode.Idle, 0f);
                    break;
                }
                aim = ((Vector2)target.bounds.center - (Vector2)muzzle.position).normalized;
                if (timer <= lockTime) mode = Mode.Locked;
                break;

            case Mode.Locked:
                if (timer <= 0f)
                {
                    Fire();
                    Change(Mode.Cooldown, cooldown);
                }
                break;

            case Mode.Cooldown:
                if (timer <= 0f) Change(Mode.Idle, 0f);
                break;
        }

        DrawSight();
    }

    void Change(Mode next, float time)
    {
        mode = next;
        timer = time;
    }

    // el dibujo y el cañon al lado que mira
    void Face()
    {
        if (body == null || muzzle == null) return;
        body.flipX = facingRight;
        Vector3 p = muzzle.localPosition;
        muzzle.localPosition = new Vector3(facingRight ? Mathf.Abs(p.x) : -Mathf.Abs(p.x), p.y, p.z);
    }

    // hacia donde mira en el mundo: cuenta Facing Right y tambien si la han volteado o girado
    Vector2 Forward() => transform.TransformVector(facingRight ? Vector3.right : Vector3.left).normalized;

    void OnDrawGizmosSelected()
    {
        if (muzzle == null) return;
        Gizmos.color = Color.red;
        Vector3 reach = Forward() * range;
        Gizmos.DrawLine(muzzle.position, muzzle.position + Quaternion.Euler(0f, 0f, maxAngle) * reach);
        Gizmos.DrawLine(muzzle.position, muzzle.position + Quaternion.Euler(0f, 0f, -maxAngle) * reach);
    }

    Collider2D FindTarget()
    {
        Vector2 forward = Forward();
        Collider2D best = null;
        float bestDistance = range;

        foreach (Collider2D candidate in Physics2D.OverlapCircleAll(muzzle.position, range, targetMask))
        {
            if (!Hazard.IsVictim(candidate.gameObject)) continue;

            Vector2 toTarget = (Vector2)candidate.bounds.center - (Vector2)muzzle.position;
            if (Vector2.Angle(forward, toTarget) > maxAngle || toTarget.magnitude > bestDistance) continue;

            // solo si no hay nada en medio
            RaycastHit2D hit = Physics2D.Raycast(muzzle.position, toTarget.normalized, range, sightMask);
            if (hit.collider != candidate) continue;

            best = candidate;
            bestDistance = toTarget.magnitude;
        }
        return best;
    }

    RaycastHit2D Shoot()
    {
        return Physics2D.Raycast(muzzle.position, aim, range * 1.5f, sightMask);
    }

    Vector2 EndPoint(RaycastHit2D hit)
    {
        return hit ? hit.point : (Vector2)muzzle.position + aim * range * 1.5f;
    }

    void Fire()
    {
        RaycastHit2D hit = Shoot();
        if (hit) Hazard.Kill(hit.collider.gameObject);

        DrawBolt(muzzle.position, EndPoint(hit));
        boltTimer = 0.1f;
        CameraFollow.Shake(shakePixels, 0.12f);
    }

    void DrawSight()
    {
        bool aiming = mode == Mode.Charging || mode == Mode.Locked;
        sight.enabled = aiming;

        if (!aiming)
        {
            body.sprite = idleSprite;
            return;
        }

        float progress = 1f - Mathf.Clamp01(timer / chargeTime);
        body.sprite = chargeFrames[Mathf.Min((int)(progress * chargeFrames.Length), chargeFrames.Length - 1)];

        sight.SetPosition(0, muzzle.position);
        sight.SetPosition(1, EndPoint(Shoot()));
        // al bloquear la punteria la linea parpadea en blanco: aviso de que va a disparar
        bool blink = mode == Mode.Locked && Time.time % 0.08f < 0.04f;
        Color color = blink ? Color.white : sightColor;
        sight.startColor = sight.endColor = color;
    }

    // rayo electrico en zigzag
    void DrawBolt(Vector2 from, Vector2 to)
    {
        int points = Mathf.Max(2, (int)(Vector2.Distance(from, to) * 3f));
        Vector2 normal = Vector2.Perpendicular(to - from).normalized;
        bolt.positionCount = points;
        for (int i = 0; i < points; i++)
        {
            float jitter = i == 0 || i == points - 1 ? 0f : Random.Range(-0.18f, 0.18f);
            bolt.SetPosition(i, Vector2.Lerp(from, to, i / (points - 1f)) + normal * jitter);
        }
    }
}
