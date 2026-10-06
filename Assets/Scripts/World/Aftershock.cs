using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Replica del temblor al entrar en el laboratorio: tiembla, cae polvo del techo y las luces parpadean un momento.
// Solo una vez por partida (al reiniciar con K ya no).
public class Aftershock : MonoBehaviour
{
    [SerializeField] float delay = 1f;
    [SerializeField] float duration = 1.4f;
    [SerializeField] float shakePixels = 2.5f;
    [SerializeField] ParticleSystem dust;
    [SerializeField] Light2D[] lights;

    static bool done;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetDone() => done = false;

    IEnumerator Start()
    {
        if (done) yield break;
        done = true;

        yield return new WaitForSeconds(delay);
        CameraFollow.Shake(shakePixels, duration);
        dust.Play();
        for (float t = 0f; t < duration; t += 0.1f)
        {
            bool on = Random.value < 0.7f;
            foreach (Light2D light in lights) light.enabled = on;
            yield return new WaitForSeconds(0.1f);
        }
        foreach (Light2D light in lights) light.enabled = true;
        dust.Stop();
    }
}
