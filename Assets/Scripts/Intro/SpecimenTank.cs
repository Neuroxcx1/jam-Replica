using UnityEngine;
using UnityEngine.Rendering.Universal;

// Tanque donde esta el especimen (tu): liquido verde con burbujas y su propia luz. Se raja y revienta.
public class SpecimenTank : MonoBehaviour
{
    [SerializeField] SpriteRenderer glass;
    [SerializeField] Sprite cracked;
    [SerializeField] Sprite broken;
    [SerializeField] SpriteRenderer liquid;
    [SerializeField] ParticleSystem bubbles;
    [SerializeField] AudioSource bubbleSound;
    [SerializeField] ParticleSystem shards;
    [SerializeField] ParticleSystem splash;
    [SerializeField] Light2D glow;

    public void Crack()
    {
        glass.sprite = cracked;
        CameraFollow.Shake(2f, 0.25f);
    }

    // quiet: sin cristales ni temblor (al saltarse la cinematica o al reiniciar)
    public void Break(bool quiet)
    {
        glass.sprite = broken;
        liquid.enabled = false;
        bubbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (bubbleSound != null) bubbleSound.Stop();
        glow.intensity *= 0.35f;
        if (quiet) return;

        shards.Play();
        splash.Play();
        CameraFollow.Shake(6f, 0.4f);
    }
}
