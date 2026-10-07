using UnityEngine;

// Ventana de la sala de observacion: la cinematica se ve a traves de su cristal. Con el temblor se raja y revienta.
public class ObservationWindow : MonoBehaviour
{
    [SerializeField] SpriteRenderer glass;
    [SerializeField] Sprite cracked;
    [SerializeField] Sprite broken;
    [SerializeField] ParticleSystem shards;
    [SerializeField] SoundMix crackSound;
    [SerializeField] SoundMix shatterSound;

    public void Crack()
    {
        glass.sprite = cracked;
        if (crackSound != null) crackSound.Play();
        CameraFollow.Shake(4f, 0.3f);
    }

    public void Shatter()
    {
        glass.sprite = broken;
        shards.Play();
        if (shatterSound != null) shatterSound.Play();
        CameraFollow.Shake(6f, 0.5f);
    }
}
