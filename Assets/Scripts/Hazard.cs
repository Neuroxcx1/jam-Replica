using UnityEngine;

// Mata al jugador o a la replica que lo toque.
// Lo usan las sierras y cuchillas, y los laseres, torretas y desechos llaman a Kill.
public class Hazard : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D collision) => Kill(collision.gameObject);
    void OnTriggerEnter2D(Collider2D other) => Kill(other.gameObject);

    public static bool IsVictim(GameObject target)
    {
        return target.TryGetComponent(out Player _) || target.TryGetComponent(out Clone _);
    }

    public static void Kill(GameObject target)
    {
        if (target.TryGetComponent(out Player player)) player.Die();
        else if (target.TryGetComponent(out Clone clone)) clone.Die();
    }
}
