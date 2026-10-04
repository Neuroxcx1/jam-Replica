using UnityEngine;

// Al morir, una bolita de luz vuela en arco desde el cuerpo hasta el checkpoint.
public class SoulOrb : MonoBehaviour
{
    [SerializeField] float arcHeight = 2f;
    [SerializeField] SpriteRenderer sprite;

    Vector3 from;
    Vector3 to;
    Vector3 control;
    float duration = 0.5f;
    float t;
    bool flying;

    public void Fly(Vector3 from, Vector3 to, float duration)
    {
        this.from = from;
        this.to = to;
        this.duration = Mathf.Max(0.05f, duration);
        control = (from + to) / 2 + Vector3.up * (arcHeight + Vector3.Distance(from, to) * 0.15f);
        transform.position = from;
        flying = true;
    }

    void Update()
    {
        if (!flying) return;

        t += Time.deltaTime / duration;
        float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        // curva de bezier: sube y cae sobre el checkpoint
        Vector3 a = Vector3.Lerp(from, control, k);
        Vector3 b = Vector3.Lerp(control, to, k);
        transform.position = Vector3.Lerp(a, b, k);

        if (t >= 1f)
        {
            flying = false;
            sprite.enabled = false;
            Destroy(gameObject, 0.3f);
        }
    }
}
