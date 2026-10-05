using System.Collections;
using UnityEngine;

// Efecto para un cuerpo que se queda en el nivel. Sirve para cualquier sprite:
// le pone un material, lo hace destellar y, si se configura, hace crecer una marca detras
// (la escarcha), tiembla la camara y congela el juego un instante.
// Congelarse (Ctrl) y morir usan este mismo script con distintos valores.
public class BodyEffect : MonoBehaviour
{
    [SerializeField] Material bodyMaterial;
    [SerializeField] SpriteRenderer decal;
    [SerializeField] float decalGrowTime = 0.3f;
    [SerializeField] float flashTime = 0.15f;
    [SerializeField] float shakePixels = 0f;
    [SerializeField] float shakeTime = 0.25f;
    [SerializeField] float hitStop = 0f;

    static readonly int ProgressId = Shader.PropertyToID("_Progress");
    static readonly int FlashId = Shader.PropertyToID("_Flash");

    SpriteRenderer[] bodyRenderers = new SpriteRenderer[0];
    MaterialPropertyBlock block;
    float timer;

    void Awake()
    {
        block = new MaterialPropertyBlock();
        if (decal != null) SetFloat(decal, ProgressId, 0f);
    }

    public void Attach(GameObject body)
    {
        bodyRenderers = body.GetComponentsInChildren<SpriteRenderer>();
        if (bodyMaterial != null)
            foreach (SpriteRenderer sr in bodyRenderers) sr.sharedMaterial = bodyMaterial;

        // se queda con el cuerpo (y desaparece con el al reiniciar)
        transform.SetParent(body.transform, true);

        if (shakePixels > 0f) CameraFollow.Shake(shakePixels, shakeTime);
        if (hitStop > 0f) StartCoroutine(HitStop());
    }

    // el juego se para un instante: da la sensacion de golpe / congelacion
    IEnumerator HitStop()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStop);
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        // por si se reinicia el nivel justo durante la pausa
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (decal != null)
        {
            float grow = Mathf.Clamp01(timer / decalGrowTime);
            SetFloat(decal, ProgressId, 1f - (1f - grow) * (1f - grow));
        }

        float flash = 1f - Mathf.Clamp01(timer / flashTime);
        foreach (SpriteRenderer sr in bodyRenderers) SetFloat(sr, FlashId, flash);

        if (timer > decalGrowTime && timer > flashTime) enabled = false;
    }

    void SetFloat(Renderer target, int id, float value)
    {
        target.GetPropertyBlock(block);
        block.SetFloat(id, value);
        target.SetPropertyBlock(block);
    }
}
