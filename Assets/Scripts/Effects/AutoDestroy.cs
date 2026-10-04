using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [SerializeField] float lifetime = 1f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
