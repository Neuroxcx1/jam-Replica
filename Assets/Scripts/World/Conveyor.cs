using UnityEngine;

// Cinta transportadora: arrastra todo lo que tenga encima (jugador, replicas y cuerpos sin congelar)
public class Conveyor : MonoBehaviour
{
    [SerializeField] float speed = 2.5f;

    Collider2D belt;

    void Awake()
    {
        belt = GetComponent<Collider2D>();
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
