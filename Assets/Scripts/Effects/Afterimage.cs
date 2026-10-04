using UnityEngine;

// Deja copias de colores que se desvanecen mientras se mueve (estilo Katana Zero, en rojo y color piel).
public class Afterimage : MonoBehaviour
{
    [SerializeField] SpriteRenderer source;
    [SerializeField] float interval = 0.06f;
    [SerializeField] float fadeTime = 0.22f;
    [SerializeField] Color[] colors = { new Color(1f, 0.25f, 0.25f, 0.5f), new Color(1f, 0.82f, 0.7f, 0.5f) };

    float timer;
    int next;
    Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0) return;
        timer = interval;

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
        ghost.color = colors[next++ % colors.Length];
        ghost.gameObject.AddComponent<FadeOut>().duration = fadeTime;
    }
}
