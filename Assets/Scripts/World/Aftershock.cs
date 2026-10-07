using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Replica del temblor al entrar en el laboratorio: tiembla, cae polvo del techo y las luces parpadean un momento;
// luego vuelven con los chasquidos de los fluorescentes. Solo una vez por partida (al reiniciar con K ya no).
public class Aftershock : MonoBehaviour
{
    [Tooltip("Empieza cuando el jugador pasa de esta x (y luego Delay). Muy a la izquierda = nada mas entrar")]
    [SerializeField] float triggerX = float.NegativeInfinity;
    [SerializeField] float delay = 1f;
    [SerializeField] float duration = 1.4f;
    [SerializeField] float shakePixels = 2.5f;
    [SerializeField] ParticleSystem dust;
    [SerializeField] Light2D[] lights;
    [SerializeField] SoundMix quakeSound;
    [Tooltip("El sonido de las luces al volver (Luces)")]
    [SerializeField] AudioSource lightsSound;

    static bool done;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetDone() => done = false;

    IEnumerator Start()
    {
        if (done) yield break;
        done = true;

        Player player = FindAnyObjectByType<Player>();
        while (player != null && player.transform.position.x < triggerX) yield return null;
        yield return new WaitForSeconds(delay);
        CameraFollow.Shake(shakePixels, duration);
        if (quakeSound != null) quakeSound.Play();
        dust.Play();
        for (float t = 0f; t < duration; t += 0.1f)
        {
            bool on = Random.value < 0.7f;
            foreach (Light2D light in lights) light.enabled = on;
            yield return new WaitForSeconds(0.1f);
        }
        dust.Stop();
        if (lightsSound != null && lightsSound.clip != null)
            yield return LightFlicker.SwitchOn(lightsSound, on => { foreach (Light2D light in lights) light.enabled = on; });
        foreach (Light2D light in lights) light.enabled = true;
    }
}
