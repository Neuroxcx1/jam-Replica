using UnityEngine;

// Ascensor: se pisa por el techo como una plataforma normal, pero cuando hay algo encima (tu, una replica o un cuerpo)
// baja un poco, y vuelve a subir cuando se queda vacio. Los cables suben hasta lo primero que haya encima.
public class SinkingPlatform : MonoBehaviour
{
    [Tooltip("Cuanto baja cuando hay algo encima (en casillas)")]
    [SerializeField] float dip = 0.3f;
    [Tooltip("A que velocidad baja y sube")]
    [SerializeField] float speed = 1.5f;
    [SerializeField] LineRenderer[] cables;

    Rigidbody2D rb;
    Collider2D col;
    Vector2 top;
    float[] ceilings;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        top = rb.position;
    }

    void Start()
    {
        ceilings = new float[cables.Length];
        for (int i = 0; i < cables.Length; i++)
        {
            Vector2 anchor = Anchor(i);
            RaycastHit2D hit = Physics2D.Raycast(anchor + Vector2.up * 0.05f, Vector2.up, 40f, LayerMask.GetMask("Ground"));
            ceilings[i] = hit ? hit.point.y : anchor.y + 10f;
        }
    }

    void FixedUpdate()
    {
        Vector2 target = Load.On(col) > 0 ? top + Vector2.down * dip : top;
        rb.MovePosition(Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime));
    }

    void LateUpdate()
    {
        for (int i = 0; i < cables.Length; i++)
        {
            Vector2 anchor = Anchor(i);
            cables[i].SetPosition(0, anchor);
            cables[i].SetPosition(1, new Vector2(anchor.x, ceilings[i]));
        }
    }

    // los cables salen del techo de la cabina, cerca de las esquinas
    Vector2 Anchor(int i)
    {
        Bounds b = col.bounds;
        float x = cables.Length == 1 ? b.center.x : Mathf.Lerp(b.min.x + 0.35f, b.max.x - 0.35f, i / (cables.Length - 1f));
        return new Vector2(x, b.max.y);
    }
}
