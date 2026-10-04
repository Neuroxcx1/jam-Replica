using UnityEngine;

// Destello blanco al aparecer. Necesita un shader con la propiedad _Flash.
public class SpriteFlash : MonoBehaviour
{
    [SerializeField] float duration = 0.15f;

    static readonly int FlashId = Shader.PropertyToID("_Flash");

    SpriteRenderer[] renderers;
    MaterialPropertyBlock block;
    float timer;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        block = new MaterialPropertyBlock();
        timer = duration;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        float amount = Mathf.Clamp01(timer / duration);
        foreach (SpriteRenderer sr in renderers)
        {
            sr.GetPropertyBlock(block);
            block.SetFloat(FlashId, amount);
            sr.SetPropertyBlock(block);
        }
        if (timer <= 0) enabled = false;
    }
}
