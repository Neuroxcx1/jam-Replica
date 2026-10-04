using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Cuerpo que dejan el jugador y las replicas. Normalmente solo cae en vertical y no se puede empujar;
// una cinta transportadora si puede arrastrarlo.
public class Body : MonoBehaviour
{
    // todos los cuerpos de la escena (T borra los de la zona actual)
    public static readonly List<Body> All = new List<Body>();

    [SerializeField] float floatSpeed = 4f;

    public float CreatedAt { get; private set; }

    Rigidbody2D rb;
    float carry;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        CreatedAt = Time.time;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

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

        float y = bottomY + transform.position.y - GetComponent<Collider2D>().bounds.min.y;
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
