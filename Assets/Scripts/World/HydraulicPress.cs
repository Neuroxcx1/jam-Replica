using System.Collections;
using UnityEngine;

// Prensa compactadora: espera arriba, baja de golpe y vuelve a subir despacio.
// Aplasta al jugador y a las replicas y compacta los cuerpos normales (desaparecen).
// Un cuerpo congelado debajo la atasca: la estasis aguanta. Si te subes encima te lleva como un ascensor.
public class HydraulicPress : MonoBehaviour
{
    [SerializeField] Rigidbody2D head;
    [SerializeField] float travel = 4f;
    [SerializeField] float waitUp = 2f;
    [SerializeField] float waitDown = 0.8f;
    [SerializeField] float slamSpeed = 16f;
    [SerializeField] float riseSpeed = 2.5f;
    [SerializeField] float shakePixels = 3f;
    [SerializeField] GameObject compactEffect;

    [Header("Piston hasta el techo")]
    [SerializeField] SpriteRenderer piston;
    [SerializeField] float ceilingY;

    BoxCollider2D headCol;
    Vector2 top;

    void Awake()
    {
        headCol = head.GetComponent<BoxCollider2D>();
        top = head.position;
    }

    IEnumerator Start()
    {
        while (true)
        {
            yield return new WaitForSeconds(waitUp);
            yield return Slam();
            yield return new WaitForSeconds(waitDown);
            while (head.position.y < top.y)
            {
                head.MovePosition(Vector2.MoveTowards(head.position, top, riseSpeed * Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();
            }
        }
    }

    IEnumerator Slam()
    {
        float bottom = top.y - travel;
        while (head.position.y > bottom)
        {
            float step = Mathf.Min(slamSpeed * Time.fixedDeltaTime, head.position.y - bottom);
            Bounds b = headCol.bounds;
            var below = new Vector2(b.center.x, b.min.y - step / 2f);

            foreach (Collider2D c in Physics2D.OverlapBoxAll(below, new Vector2(b.size.x - 0.1f, step + 0.02f), 0f))
            {
                if (!c.TryGetComponent(out Body body))
                {
                    Hazard.Kill(c.gameObject);
                    continue;
                }

                if (c.attachedRigidbody.bodyType == RigidbodyType2D.Static)
                {
                    // atascada: se queda apoyada en el cuerpo mientras exista
                    head.MovePosition(new Vector2(head.position.x, c.bounds.max.y + head.position.y - b.min.y));
                    CameraFollow.Shake(shakePixels, 0.15f);
                    while (body != null) yield return null;
                    yield break;
                }

                if (compactEffect != null) Instantiate(compactEffect, body.transform.position, Quaternion.identity);
                Destroy(body.gameObject);
            }

            head.MovePosition(head.position + Vector2.down * step);
            yield return new WaitForFixedUpdate();
        }
        CameraFollow.Shake(shakePixels, 0.15f);
    }

    void LateUpdate()
    {
        float headTop = headCol.bounds.max.y;
        piston.size = new Vector2(piston.size.x, Mathf.Max(0f, ceilingY - headTop));
        piston.transform.position = new Vector3(head.position.x, (ceilingY + headTop) / 2f, 0f);
    }
}
