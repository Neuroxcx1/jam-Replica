using UnityEngine;

// Al recuperar una replica (Q): el cuerpo se convierte en una copia holografica que vuelve volando hacia ti,
// arranca despacio, acelera y se encoge, y al llegar te la "absorbes".
public class RecallGhost : MonoBehaviour
{
    [SerializeField] float duration = 0.4f;
    [SerializeField] float arcHeight = 1.5f;
    [SerializeField] GameObject absorbEffect;
    [SerializeField] float shakePixels = 2f;

    SpriteRenderer sr;
    Transform target;
    Vector3 from;
    float t;

    // copia la forma del cuerpo (el material es el de las replicas, puesto en el prefab)
    public void Fly(SpriteRenderer source, Transform target)
    {
        this.target = target;
        from = source.transform.position;
        transform.position = from;

        sr = GetComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.drawMode = source.drawMode;
        sr.size = source.size;
    }

    void Update()
    {
        t += Time.deltaTime / duration;
        float k = Mathf.Clamp01(t) * Mathf.Clamp01(t);
        Vector3 to = target != null ? target.position : from;

        // curva que sube un poco y cae sobre ti
        Vector3 control = (from + to) / 2f + Vector3.up * arcHeight;
        Vector3 a = Vector3.Lerp(from, control, k);
        Vector3 b = Vector3.Lerp(control, to, k);
        transform.position = Vector3.Lerp(a, b, k);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, k);

        if (t < 1f) return;
        if (absorbEffect != null) Instantiate(absorbEffect, to, Quaternion.identity);
        CameraFollow.Shake(shakePixels, 0.08f);
        Destroy(gameObject);
    }
}
