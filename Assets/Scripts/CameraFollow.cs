using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);
    [SerializeField] float smoothTime = 0.25f;

    // mientras el jugador este dentro de esta zona la camara no se mueve
    [SerializeField] Vector2 deadZone = new Vector2(3f, 2.5f);

    static CameraFollow current;

    Vector3 focus;
    Vector3 velocity;
    bool recentering;
    float shakePixels;
    float shakeTime;
    float shakeTimer;

    // temblor en pixeles del juego (16 por unidad)
    public static void Shake(float pixels, float duration)
    {
        if (current == null) return;
        float remaining = current.shakeTimer > 0f ? current.shakePixels * current.shakeTimer / current.shakeTime : 0f;
        if (pixels < remaining) return;
        current.shakePixels = pixels;
        current.shakeTime = duration;
        current.shakeTimer = duration;
    }

    void Awake()
    {
        current = this;
        focus = transform.position;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 wanted = target.position + (Vector3)offset;
            float dx = wanted.x - focus.x;
            float dy = wanted.y - focus.y;

            // si el jugador aparece lejos (reaparecer en el checkpoint) se centra en el suavemente
            if (Mathf.Abs(dx) > deadZone.x * 2f || Mathf.Abs(dy) > deadZone.y * 2f) recentering = true;

            if (recentering)
            {
                focus = Vector3.SmoothDamp(focus, wanted, ref velocity, smoothTime);
                if ((wanted - focus).sqrMagnitude < 0.0004f) recentering = false;
            }
            else
            {
                // zona muerta: solo se mueve cuando el jugador empuja el borde, y entonces va pegada a el
                // (sin suavizado, asi el personaje no tiembla en pantalla al caminar)
                if (dx > deadZone.x) focus.x = wanted.x - deadZone.x;
                else if (dx < -deadZone.x) focus.x = wanted.x + deadZone.x;
                if (dy > deadZone.y) focus.y = wanted.y - deadZone.y;
                else if (dy < -deadZone.y) focus.y = wanted.y + deadZone.y;
            }
        }

        // temblor en pixeles enteros (16 por unidad) para que siempre se note, aunque sea leve.
        // Usa tiempo real para que tambien se note durante la pausa al congelarse
        Vector2 shake = Vector2.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            float pixels = Mathf.Max(1f, Mathf.Round(shakePixels * Mathf.Clamp01(shakeTimer / shakeTime)));
            Vector2 dir = Random.insideUnitCircle.normalized;
            shake = new Vector2(Mathf.Round(dir.x * pixels), Mathf.Round(dir.y * pixels)) / 16f;
        }

        transform.position = new Vector3(focus.x + shake.x, focus.y + shake.y, transform.position.z);
    }
}
