using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] SpriteRenderer flag;
    [SerializeField] Color activeColor = new Color(0.4f, 1f, 0.5f);

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Player player)) return;

        player.SetCheckpoint(transform.position);
        flag.color = activeColor;
    }
}
