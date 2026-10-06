using UnityEngine;

// Montacargas de contrapeso: la cabina sube cuando en el contrapeso hay mas carga que en ella
// y baja cuando no (la cabina pesa mas que el contrapeso vacio). La carga la cuenta Load.
// Al bajar, como la prensa: un cuerpo debajo la atasca (hasta que lo recuperas con Q) y a ti o a un clon os aplasta.
// El hielo (las copias congeladas) que toca la cabina o el contrapeso se rompe y vuelve como replica:
// asi no se puede subir por el hueco con hielo en vez de usar el montacargas.
// Lo que sube lo marca Car Travel (no el tamaño): al seleccionarlo, el recuadro verde es hasta donde llega la cabina.
public class CounterweightLift : MonoBehaviour
{
    [SerializeField] Rigidbody2D car;
    [SerializeField] Rigidbody2D counterweight;
    [Tooltip("Cuanto sube la cabina (en casillas)")]
    [SerializeField] float carTravel = 5f;
    [Tooltip("Cuanto baja el contrapeso: su foso tiene que ser una casilla mas hondo")]
    [SerializeField] float counterweightTravel = 2f;
    [Tooltip("Velocidad de la cabina")]
    [SerializeField] float speed = 2f;

    [Header("Cables")]
    [SerializeField] Transform pulley;
    // donde se engancha el cable a la cabina (si no hay, al centro)
    [SerializeField] Transform carHook;
    [SerializeField] LineRenderer carCable;
    [SerializeField] LineRenderer counterweightCable;

    Collider2D carCol;
    Collider2D counterweightCol;
    Player player;
    Vector2 carBottom;
    Vector2 counterweightTop;
    float lift;   // 0 = cabina abajo, 1 = cabina arriba

    void Awake()
    {
        carCol = car.GetComponent<Collider2D>();
        counterweightCol = counterweight.GetComponent<Collider2D>();
        carBottom = car.position;
        counterweightTop = counterweight.position;
        player = FindAnyObjectByType<Player>();
    }

    void FixedUpdate()
    {
        BreakIce(carCol);
        BreakIce(counterweightCol);
        bool up = Load.On(counterweightCol) > Load.On(carCol);
        if (!up && Jammed()) return;
        lift = Mathf.MoveTowards(lift, up ? 1f : 0f, speed / carTravel * Time.fixedDeltaTime);
        car.MovePosition(carBottom + Vector2.up * carTravel * lift);
        counterweight.MovePosition(counterweightTop + Vector2.down * counterweightTravel * lift);
    }

    void BreakIce(Collider2D platform)
    {
        Bounds b = platform.bounds;
        foreach (Collider2D c in Physics2D.OverlapBoxAll(b.center, b.size + new Vector3(0.1f, 0.16f), 0f))
        {
            if (!c.TryGetComponent(out Body body) || !body.Frozen) continue;
            if (player != null) player.Return(body.gameObject);
            else Destroy(body.gameObject);
        }
    }

    // lo que hay justo debajo de la cabina cuando baja
    bool Jammed()
    {
        Bounds b = carCol.bounds;
        var below = new Vector2(b.center.x, b.min.y - 0.06f);
        foreach (Collider2D c in Physics2D.OverlapBoxAll(below, new Vector2(b.size.x - 0.05f, 0.1f), 0f))
        {
            if (c.TryGetComponent(out Body _)) return true;
            Hazard.Kill(c.gameObject);
        }
        return false;
    }

    // los fosos que hay que cavar en el suelo (recuadros rojos): el de la cabina de 1 y el del contrapeso mas hondo
    void OnDrawGizmos()
    {
        if (car == null || counterweight == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.3f);
        var carPit = car.GetComponent<Collider2D>();
        var weightPit = counterweight.GetComponent<Collider2D>();
        if (carPit != null)
            Gizmos.DrawWireCube(new Vector3(carPit.bounds.center.x, car.position.y - 0.5f), new Vector3(carPit.bounds.size.x, 1f));
        if (weightPit != null)
        {
            float depth = counterweightTravel + 1f;
            Gizmos.DrawWireCube(new Vector3(weightPit.bounds.center.x, counterweight.position.y - depth / 2f), new Vector3(weightPit.bounds.size.x, depth));
        }
    }

    void OnDrawGizmosSelected()
    {
        if (car == null || !car.TryGetComponent(out Collider2D carShape)) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(carShape.bounds.center + Vector3.up * carTravel, carShape.bounds.size);
    }

    void LateUpdate()
    {
        carCable.SetPosition(0, carHook != null ? carHook.position : car.transform.position);
        carCable.SetPosition(1, pulley.position);
        counterweightCable.SetPosition(0, counterweight.transform.position);
        counterweightCable.SetPosition(1, pulley.position);
    }
}
