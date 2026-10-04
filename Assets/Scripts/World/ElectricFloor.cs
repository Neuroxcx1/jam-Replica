using UnityEngine;
using UnityEngine.Rendering.Universal;

// Suelo electrificado de los corrales de especimenes: con corriente mata al que lo pisa.
// Los cuerpos no conducen, asi que subido a uno no te pasa nada. La corriente va a pulsos.
// Necesita un Hazard y un collider trigger (una franja fina pegada al suelo) en el mismo objeto.
public class ElectricFloor : MonoBehaviour
{
    [SerializeField] float onTime = 2.5f;
    [SerializeField] float offTime = 1.5f;
    [SerializeField] SpriteRenderer arcs;
    [SerializeField] Light2D glow;

    Collider2D zone;
    float timer;

    void Awake()
    {
        zone = GetComponent<Collider2D>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        bool on = timer % (onTime + offTime) < onTime;
        zone.enabled = on;
        arcs.enabled = on;
        glow.enabled = on;
    }
}
