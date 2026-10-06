using UnityEngine;

// Suelo de cristal de observacion: aguanta hasta maxLoad de carga (ver Load).
// Al llegar al limite se raja (aviso) y con mas carga se rompe y todo cae.
// Con Reset On Respawn, al morir y volver al checkpoint vuelve a estar entero.
public class FragileGlass : MonoBehaviour
{
    [SerializeField] int maxLoad = 1;
    [Tooltip("Marcalo para que el cristal se recomponga cuando mueres y vuelves al checkpoint")]
    [SerializeField] bool resetOnRespawn;
    [SerializeField] SpriteRenderer glass;
    [SerializeField] Sprite cracked;
    [SerializeField] ParticleSystem shards;
    [SerializeField] float shakePixels = 4f;

    Collider2D col;
    Sprite intact;
    Player player;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        intact = glass.sprite;
    }

    void Start()
    {
        player = FindAnyObjectByType<Player>();
        if (resetOnRespawn && player != null) player.Respawned += Restore;
    }

    void OnDestroy()
    {
        if (player != null) player.Respawned -= Restore;
    }

    void Restore()
    {
        col.enabled = true;
        glass.enabled = true;
        glass.sprite = intact;
        enabled = true;
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
