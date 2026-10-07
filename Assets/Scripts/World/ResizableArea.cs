using UnityEngine;
using UnityEngine.Rendering.Universal;

// Para los prefabs que se estiran en el editor con la herramienta Rect (tecla T) sobre el sprite:
// el collider, los otros sprites y la luz siguen el tamaño del sprite principal.
// Va antes que los demas scripts para que al empezar ya tengan el collider con su tamaño.
[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class ResizableArea : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] BoxCollider2D area;
    [SerializeField] SpriteRenderer[] alsoResize;
    [SerializeField] Light2D glow;
    // lo que va pegado al borde de abajo (las gotas al final de un chorro)
    [SerializeField] Transform bottom;
    [Tooltip("Solo se estira a lo ancho: cada parte se queda con su alto (suelos electricos)")]
    [SerializeField] bool widthOnly;

    void Awake() => Sync();

    void Update()
    {
        if (!Application.isPlaying) Sync();
    }

    Vector2 Fit(Vector2 current, Vector2 size) => widthOnly ? new Vector2(size.x, current.y) : size;

    public void Sync()
    {
        if (sprite == null) return;
        Vector2 size = sprite.size;
        if (area != null) area.size = Fit(area.size, size);
        foreach (SpriteRenderer other in alsoResize)
            if (other != null) other.size = Fit(other.size, size);
        if (glow != null) glow.pointLightOuterRadius = Mathf.Max(size.x, size.y) / 2f + 1.5f;
        if (bottom != null) bottom.localPosition = new Vector3(0f, -size.y / 2f, 0f);
    }
}
