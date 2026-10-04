using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Puerta entre zonas. Esta abierta hasta que la cruzas: entonces se cierra detras de ti
// y el principio de la zona nueva pasa a ser el checkpoint (y recarga las replicas).
public class ZoneDoor : MonoBehaviour
{
    [SerializeField] Collider2D blocker;
    [SerializeField] Transform spawnPoint;
    [SerializeField] SpriteLoop barrier;
    [SerializeField] Sprite[] closingFrames;
    [SerializeField] Sprite[] closedFrames;
    [SerializeField] Light2D doorLight;
    [SerializeField] Color closedColor = new Color(1f, 0.25f, 0.2f);
    [SerializeField] float shakePixels = 3f;

    bool closed;

    void Awake()
    {
        blocker.enabled = false;
    }

    // el trigger esta al otro lado de la puerta, asi solo se cierra cuando ya has pasado
    void OnTriggerEnter2D(Collider2D other)
    {
        if (closed || !other.TryGetComponent(out Player player)) return;

        closed = true;
        blocker.enabled = true;
        player.SetCheckpoint(spawnPoint.position);
        StartCoroutine(Close());
    }

    IEnumerator Close()
    {
        barrier.Play(closingFrames, false);
        yield return new WaitUntil(() => barrier.Finished);

        barrier.Play(closedFrames, true);
        if (doorLight != null) doorLight.color = closedColor;
        CameraFollow.Shake(shakePixels, 0.2f);
    }
}
