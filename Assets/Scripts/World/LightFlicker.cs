using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Fluorescente viejo: casi siempre encendido y de vez en cuando titila.
// Con pulseSpeed > 0 no titila sino que respira (luces de emergencia).
// Con Sound (el sonido Luces) el titileo sigue sus chasquidos: en cada uno el tubo se enciende.
public class LightFlicker : MonoBehaviour
{
    [SerializeField] Light2D lamp;
    [SerializeField] SpriteRenderer tube;
    [SerializeField] Vector2 waitRange = new Vector2(3f, 9f);
    [SerializeField] float pulseSpeed;
    [Tooltip("El sonido de encenderse (Sounds/Prfabs/Luces): si esta, la luz titila con sus chasquidos")]
    [SerializeField] AudioSource sound;

    // los chasquidos del sonido Luces (segundos dentro del archivo): el primero es el de prender
    public static readonly float[] Clicks = { 1.62f, 1.96f, 2.02f, 2.15f, 2.22f, 2.36f };
    // empieza a sonar un poco antes del primer chasquido, con la luz ya apagada
    const float Lead = 0.15f;

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
            if (sound != null && sound.clip != null)
            {
                yield return SwitchOn(sound, on => Set(on ? 1f : Random.Range(0f, 0.3f)));
                yield return new WaitForSeconds(0.5f);
                sound.Stop();
                continue;
            }
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

    // hace sonar Luces desde justo antes del primer chasquido y enciende (set(true)) en cada uno, con un apagon cortito
    // entre uno y otro; al acabar queda encendida. Lo usa tambien la cinematica al volver la corriente
    public static IEnumerator SwitchOn(AudioSource sound, System.Action<bool> set)
    {
        float start = Clicks[0] - Lead;
        sound.time = start;
        sound.Play();
        set(false);
        float zero = Time.time - start;
        for (int i = 0; i < Clicks.Length; i++)
        {
            yield return new WaitForSeconds(Clicks[i] - (Time.time - zero));
            set(true);
            if (i == Clicks.Length - 1) break;
            yield return new WaitForSeconds(0.03f);
            set(false);
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
