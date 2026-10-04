using UnityEngine;

public class Hazard : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D collision) => Kill(collision.gameObject);
    void OnTriggerEnter2D(Collider2D other) => Kill(other.gameObject);

    void Kill(GameObject target)
    {
        if (target.TryGetComponent(out Player player)) player.Die();
        else if (target.TryGetComponent(out Clone clone)) clone.Die();
    }
}
