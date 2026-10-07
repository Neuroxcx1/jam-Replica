using UnityEngine;
using static UnityEngine.GraphicsBuffer;

// Desechos toxicos: deshacen al jugador y a las replicas.
// Los cuerpos no se hunden: flotan quietos en la superficie y sirven de balsa para cruzar.
public class WastePoolEstrange : MonoBehaviour
{
    [Tooltip("Cuanto se hunden los cuerpos que flotan")]
    [SerializeField] float sink = 0.35f;

    static readonly int SurfaceId = Shader.PropertyToID("_SurfaceY");
    float surface;

    void Awake()
    {
        surface = GetComponent<Collider2D>().bounds.max.y;

        // el shader dibuja las olas a partir de esta altura
        var sr = GetComponent<SpriteRenderer>();
        var block = new MaterialPropertyBlock();
        sr.GetPropertyBlock(block);
        block.SetFloat(SurfaceId, surface);
        sr.SetPropertyBlock(block);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Body body)) body.FloatAt(surface - sink);
        else
        {
            if (other.gameObject.TryGetComponent(out Player player)) player.Die();
            else if (other.gameObject.TryGetComponent(out Clone clone)) clone.lifestended();
        }
    }
}
