using UnityEngine;

public class Clone : MonoBehaviour
{
    [SerializeField] float speed = 4f;
    [SerializeField] float lifeTime = 3f;
    [SerializeField] GameObject bodyPrefab;
    [SerializeField] Transform visual;
    [SerializeField] GameObject solidifyEffect;

    Rigidbody2D rb;
    Collider2D col;
    Collider2D owner;
    int direction = 1;
    float lifeTimer;
    float carry;
    bool hitWall;
    bool dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        lifeTimer = lifeTime;
    }

    public void Init(int direction, Collider2D owner)
    {
        this.direction = direction;
        this.owner = owner;
        visual.localScale = new Vector3(direction, 1, 1);
        Physics2D.IgnoreCollision(col, owner);
    }

    void FixedUpdate()
    {
        lifeTimer -= Time.fixedDeltaTime;

        // si el jugador esta dentro de la replica espera a que salga, si no el cuerpo le aparece encima
        bool playerInside = col.Distance(owner).distance < -0.3f;
        if ((lifeTimer <= 0 || hitWall) && !playerInside)
        {
            Die();
            return;
        }

        // solo va hacia delante. Si cae, cae recto para que los cuerpos queden juntos
        bool falling = rb.linearVelocity.y < -0.1f;
        rb.linearVelocity = new Vector2((falling ? 0 : direction * speed) + carry, rb.linearVelocity.y);
        carry = 0f;
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
        if (dead) return;
        dead = true;
        Instantiate(bodyPrefab, transform.position, Quaternion.identity);
        if (solidifyEffect != null) Instantiate(solidifyEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
