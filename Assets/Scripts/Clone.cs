using UnityEngine;

public class Clone : MonoBehaviour
{
    [SerializeField] float speed = 5f;
    // sale disparado como un golpe y enseguida frena hasta la velocidad de andar
    [SerializeField] float launchSpeed = 20f;
    [SerializeField] float launchTime = 0.25f;
    [SerializeField] float lifeTime = 3f;
    [SerializeField] GameObject bodyPrefab;
    [SerializeField] Transform visual;

    [Header("Efectos")]
    [SerializeField] GameObject solidifyEffect;
    [SerializeField] GameObject impactEffect;
    [SerializeField] ParticleSystem speedLines;
    [SerializeField] float impactShake = 3f;

    Rigidbody2D rb;
    Collider2D col;
    Player owner;
    Collider2D ownerCol;
    int direction = 1;
    float age;
    float carry;
    bool hitWall;
    bool dead;

    bool Launching => age < launchTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void Init(int direction, Player owner)
    {
        this.direction = direction;
        this.owner = owner;
        ownerCol = owner.GetComponent<Collider2D>();
        visual.localScale = new Vector3(direction, 1, 1);
        Physics2D.IgnoreCollision(col, ownerCol);

        if (TryGetComponent(out Afterimage trail)) trail.Boost(launchTime);
        if (speedLines != null)
        {
            var velocity = speedLines.velocityOverLifetime;
            velocity.x = -direction * 3f;
        }
    }

    void FixedUpdate()
    {
        age += Time.fixedDeltaTime;

        // si el jugador esta dentro de la replica espera a que salga, si no el cuerpo le aparece encima
        bool playerInside = col.Distance(ownerCol).distance < -0.3f;
        if ((age >= lifeTime || hitWall) && !playerInside)
        {
            Die();
            return;
        }

        // solo va hacia delante. Si cae (ya andando), cae recto para que los cuerpos queden juntos
        float run = Mathf.Lerp(launchSpeed, speed, age / launchTime);
        bool falling = rb.linearVelocity.y < -0.1f && !Launching;
        rb.linearVelocity = new Vector2((falling ? 0 : direction * run) + carry, rb.linearVelocity.y);
        carry = 0f;

        if (speedLines != null)
        {
            var emission = speedLines.emission;
            emission.enabled = Launching;
        }
    }

    // la cinta transportadora la arrastra
    public void Carry(float speed) => carry = speed;

    // si choca de frente con algo (pared, caja, otro cuerpo) muere ahi mismo.
    // Stay tambien cuenta: el suelo y las paredes del mapa son un solo collider y chocar con la pared no es un contacto nuevo
    void OnCollisionEnter2D(Collision2D collision) => CheckWall(collision);
    void OnCollisionStay2D(Collision2D collision) => CheckWall(collision);

    void CheckWall(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.x * direction < -0.9f)
                hitWall = true;
            
        }
    }

    public void Die()
    {
        Debug.Log("entro en Die");
        if (dead) return;
        dead = true;

        // el cubo es mas bajo que el clon: aparece con la base donde tenia los pies
        float half = bodyPrefab.GetComponent<BoxCollider2D>().size.y / 2f;
        Vector3 at = transform.position + Vector3.down * (col.bounds.extents.y - half);
        GameObject body = Instantiate(bodyPrefab, at, Quaternion.identity);
        if (owner != null) owner.Replace(gameObject, body);

        // si se estampa contra algo nada mas salir, golpe fuerte
        bool impact = hitWall && age < launchTime + 0.15f;
        GameObject effect = impact && impactEffect != null ? impactEffect : solidifyEffect;
        if (effect != null) Instantiate(effect, transform.position, Quaternion.identity);
        if (impact) CameraFollow.Shake(impactShake, 0.12f);
        Destroy(gameObject);
    }

    public void lifestended()
    {
        lifeTime = lifeTime + 10f;
    }
}
