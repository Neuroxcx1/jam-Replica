using System.Collections;
using UnityEngine;

// Cuerpo que dejan el jugador y las replicas. Normalmente solo cae en vertical y no se puede empujar;
// una cinta transportadora si puede arrastrarlo.
public class Body : MonoBehaviour
{
    [SerializeField] float floatSpeed = 4f;

    Rigidbody2D rb;
    Collider2D col;
    float carry;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    // el que la crea (el jugador al congelarse) la atraviesa hasta separarse; despues ya choca con ella
    public void IgnoreUntilApart(Collider2D other) => StartCoroutine(Ignore(other));

    IEnumerator Ignore(Collider2D other)
    {
        Physics2D.IgnoreCollision(col, other, true);
        while (other != null && col.Distance(other).distance < 0.02f) yield return new WaitForFixedUpdate();
        if (other != null) Physics2D.IgnoreCollision(col, other, false);
    }

    public void Carry(float speed) => carry = speed;

    void FixedUpdate()
    {
        if (rb.bodyType != RigidbodyType2D.Dynamic) return;

        // solo se mueve en horizontal mientras algo lo arrastra
        bool carried = carry != 0f;
        rb.constraints = carried
            ? RigidbodyConstraints2D.FreezeRotation
            : RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        if (carried) rb.linearVelocity = new Vector2(carry, rb.linearVelocity.y);
        carry = 0f;
    }

    // en un liquido se queda flotando quieto con la parte de abajo en bottomY, como una balsa
    public void FloatAt(float bottomY)
    {
        if (rb.bodyType == RigidbodyType2D.Static) return;

        float y = bottomY + transform.position.y - col.bounds.min.y;
        StartCoroutine(Float(y));
    }

    IEnumerator Float(float y)
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        while (Mathf.Abs(rb.position.y - y) > 0.001f)
        {
            rb.MovePosition(new Vector2(rb.position.x, Mathf.MoveTowards(rb.position.y, y, floatSpeed * Time.fixedDeltaTime)));
            yield return new WaitForFixedUpdate();
        }
        rb.bodyType = RigidbodyType2D.Static;
    }
}
