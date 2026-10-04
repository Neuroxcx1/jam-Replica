using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Fluorescente viejo: casi siempre encendido y de vez en cuando titila.
// Con pulseSpeed > 0 no titila sino que respira (luces de emergencia).
public class LightFlicker : MonoBehaviour
{
    [SerializeField] Light2D lamp;
    [SerializeField] SpriteRenderer tube;
    [SerializeField] Vector2 waitRange = new Vector2(3f, 9f);
    [SerializeField] float pulseSpeed;

    float baseIntensity;
    Color tubeColor;

    void Awake()
    {
        baseIntensity = lamp.intensity;
        if (tube != null) tubeColor = tube.color;
    }

    IEnumerator Start()
    {
        if (pulseSpeed > 0f) yield break;

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(waitRange.x, waitRange.y));
            int flicks = Random.Range(2, 6);
            for (int i = 0; i < flicks; i++)
            {
                Set(Random.Range(0f, 0.4f));
                yield return new WaitForSeconds(Random.Range(0.03f, 0.09f));
                Set(1f);
                yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
            }
        }
    }

    void Update()
    {
        if (pulseSpeed > 0f) Set(0.55f + 0.45f * Mathf.Sin(Time.time * pulseSpeed));
    }

    void Set(float amount)
    {
        lamp.intensity = baseIntensity * amount;
        if (tube != null) tube.color = new Color(tubeColor.r * amount, tubeColor.g * amount, tubeColor.b * amount, tubeColor.a);
    }
}
