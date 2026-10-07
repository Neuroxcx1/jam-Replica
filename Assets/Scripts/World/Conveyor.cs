using UnityEngine;

// Cinta transportadora: arrastra todo lo que tenga encima (jugador, replicas y cuerpos sin congelar)
public class Conveyor : MonoBehaviour
{
    [Tooltip("Velocidad: positiva lleva a la derecha, negativa a la izquierda")]
    [SerializeField] float speed = 2.5f;

    Collider2D belt;

    void Awake()
    {
        belt = GetComponent<Collider2D>();
        // con velocidad negativa va hacia la izquierda: los listones se dibujan al reves
        if (TryGetComponent(out SpriteRenderer sprite)) sprite.flipX = speed < 0f;
    }

    void FixedUpdate()
    {
        Bounds b = belt.bounds;
        var top = new Vector2(b.center.x, b.max.y + 0.08f);
        foreach (Collider2D c in Physics2D.OverlapBoxAll(top, new Vector2(b.size.x, 0.12f), 0f))
        {
            if (c.TryGetComponent(out Player player)) player.Carry(speed);
            else if (c.TryGetComponent(out Clone clone)) clone.Carry(speed);
            else if (c.TryGetComponent(out Body body)) body.Carry(speed);
        }
    }
}
