using UnityEngine;

// Suelo de cristal de observacion: aguanta hasta maxLoad de carga (ver Load).
// Al llegar al limite se raja (aviso) y con mas carga se rompe y todo cae.
public class FragileGlass : MonoBehaviour
{
    [SerializeField] int maxLoad = 1;
    [SerializeField] SpriteRenderer glass;
    [SerializeField] Sprite cracked;
    [SerializeField] ParticleSystem shards;
    [SerializeField] float shakePixels = 4f;

    Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        int load = Load.On(col);
        if (load >= maxLoad) glass.sprite = cracked;
        if (load <= maxLoad) return;

        col.enabled = false;
        glass.enabled = false;
        shards.Play();
        CameraFollow.Shake(shakePixels, 0.2f);
        enabled = false;
    }
}
