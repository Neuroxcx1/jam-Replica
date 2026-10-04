using UnityEngine;

// Montacargas de contrapeso: la cabina sube cuando en el contrapeso hay mas carga que en ella
// y baja cuando no (la cabina pesa mas que el contrapeso vacio). La carga la cuenta Load.
public class CounterweightLift : MonoBehaviour
{
    [SerializeField] Rigidbody2D car;
    [SerializeField] Rigidbody2D counterweight;
    [SerializeField] float carTravel = 5f;
    [SerializeField] float counterweightTravel = 2f;
    [SerializeField] float speed = 2f;

    [Header("Cables")]
    [SerializeField] Transform pulley;
    [SerializeField] LineRenderer carCable;
    [SerializeField] LineRenderer counterweightCable;

    Collider2D carCol;
    Collider2D counterweightCol;
    Vector2 carBottom;
    Vector2 counterweightTop;
    float lift;   // 0 = cabina abajo, 1 = cabina arriba

    void Awake()
    {
        carCol = car.GetComponent<Collider2D>();
        counterweightCol = counterweight.GetComponent<Collider2D>();
        carBottom = car.position;
        counterweightTop = counterweight.position;
    }

    void FixedUpdate()
    {
        bool up = Load.On(counterweightCol) > Load.On(carCol);
        lift = Mathf.MoveTowards(lift, up ? 1f : 0f, speed / carTravel * Time.fixedDeltaTime);
        car.MovePosition(carBottom + Vector2.up * carTravel * lift);
        counterweight.MovePosition(counterweightTop + Vector2.down * counterweightTravel * lift);
    }

    void LateUpdate()
    {
        carCable.SetPosition(0, car.transform.position);
        carCable.SetPosition(1, pulley.position);
        counterweightCable.SetPosition(0, counterweight.transform.position);
        counterweightCable.SetPosition(1, pulley.position);
    }
}
