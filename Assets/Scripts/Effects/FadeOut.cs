using UnityEngine;

public class FadeOut : MonoBehaviour
{
    public float duration = 0.2f;

    SpriteRenderer sr;
    Color startColor;
    float t;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        startColor = sr.color;
    }

    void Update()
    {
        t += Time.deltaTime;
        Color c = startColor;
        c.a *= 1f - Mathf.Clamp01(t / duration);
        sr.color = c;
        if (t >= duration) Destroy(gameObject);
    }
}
