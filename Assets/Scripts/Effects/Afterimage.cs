using UnityEngine;

// Deja copias de colores que se desvanecen mientras se mueve (estilo Katana Zero, en rojo y color piel).
public class Afterimage : MonoBehaviour
{
    [SerializeField] SpriteRenderer source;
    // sin luz: las copias brillan aunque la sala este a oscuras
    [SerializeField] Material material;
    [SerializeField] float interval = 0.06f;
    [SerializeField] float fadeTime = 0.22f;
    [SerializeField] Color[] colors = { new Color(1f, 0.25f, 0.25f, 0.5f), new Color(1f, 0.82f, 0.7f, 0.5f) };

    float timer;
    float boostTimer;
    int next;
    Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    // durante un rato deja las copias mucho mas seguidas (cuando el clon sale disparado)
    public void Boost(float duration) => boostTimer = duration;

    void Update()
    {
        timer -= Time.deltaTime;
        boostTimer -= Time.deltaTime;
        if (timer > 0) return;
        timer = boostTimer > 0 ? interval / 3f : interval;

        // solo deja rastro si se esta moviendo
        bool moved = (transform.position - lastPosition).sqrMagnitude > 0.0004f;
        lastPosition = transform.position;
        if (moved) SpawnGhost();
    }

    void SpawnGhost()
    {
        var ghost = new GameObject("Afterimage").AddComponent<SpriteRenderer>();
        ghost.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        ghost.transform.localScale = source.transform.lossyScale;
        ghost.sprite = source.sprite;
        ghost.drawMode = source.drawMode;
        ghost.size = source.size;
        ghost.sortingOrder = source.sortingOrder - 1;
        if (material != null) ghost.sharedMaterial = material;
        ghost.color = colors[next++ % colors.Length];
        ghost.gameObject.AddComponent<FadeOut>().duration = fadeTime;
    }
}
