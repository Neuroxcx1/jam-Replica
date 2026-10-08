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

    [Header("Audio")]
    [Tooltip("Sonido cuando el cristal se rompe y cae")]
    [SerializeField] AudioClip sonidoRomper;
    [Tooltip("Opcional: Sonido de advertencia cuando el cristal se raja")]
    [SerializeField] AudioClip sonidoRajar;

    Collider2D col;
    Sprite intact;
    Player player;
    
    // Variable para controlar que el sonido de crujido suene solo una vez
    bool yaRajado; 

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
        yaRajado = false; // Reiniciamos el estado para que pueda volver a sonar si se raja
        enabled = true;
    }

    void FixedUpdate()
    {
        int load = Load.On(col);
        
        // --- CONTROL DE ESTADO RAJADO ---
        if (load >= maxLoad && !yaRajado)
        {
            glass.sprite = cracked;
            yaRajado = true;
            
            // Reproducir sonido de crujido (si asignaste uno en el Inspector)
            if (sonidoRajar != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.ReproducirSonido(sonidoRajar);
            }
        }
        
        if (load <= maxLoad) return;

        // --- CONTROL DE ROTURA TOTAL ---
        // Se llama al gestor global justo en el frame en que se quiebra todo
        if (sonidoRomper != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.ReproducirSonido(sonidoRomper);
        }
        // -------------------------------

        col.enabled = false;
        glass.enabled = false;
        shards.Play();
        CameraFollow.Shake(shakePixels, 0.2f);
        enabled = false;
    }
}