using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);
    [SerializeField] float pixelsPerUnit = 32f;

    // mientras el jugador este dentro de esta zona la camara no se mueve
    [SerializeField] Vector2 deadZone = new Vector2(3f, 2.5f);

    // al reaparecer lejos: rapido al principio y frena al llegar, pero nunca mas lento que esto
    // (tiene que ser mas rapido que el jugador para que siempre lo alcance)
    [SerializeField] float recenterSpeed = 12f;

    static CameraFollow current;

    Vector2 focus;
    bool recentering;
    float shakePixels;
    float shakeTime;
    float shakeTimer;

    // temblor en pixeles del juego. Menos de 1 = solo tiembla en algunos frames (mas suave)
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

    // a la rejilla de pixeles, igual que el sprite del jugador (PixelSnap), para que no tiemble en pantalla
    float Snap(float value) => Mathf.Round(value * pixelsPerUnit) / pixelsPerUnit;

    void LateUpdate()
    {
        if (target != null)
        {
            Vector2 wanted = new Vector2(Snap(target.position.x), Snap(target.position.y)) + offset;
            Vector2 diff = wanted - focus;

            // si el jugador aparece lejos (reaparecer en el checkpoint) se centra en el
            if (Mathf.Abs(diff.x) > deadZone.x * 2f || Mathf.Abs(diff.y) > deadZone.y * 2f) recentering = true;

            if (recentering)
            {
                float speed = Mathf.Max(recenterSpeed, diff.magnitude * 6f);
                focus = Vector2.MoveTowards(focus, wanted, speed * Time.deltaTime);
                if (focus == wanted) recentering = false;
            }
            else
            {
                // zona muerta: solo se mueve cuando el jugador empuja el borde, y entonces va pegada a el
                if (diff.x > deadZone.x) focus.x = wanted.x - deadZone.x;
                else if (diff.x < -deadZone.x) focus.x = wanted.x + deadZone.x;
                if (diff.y > deadZone.y) focus.y = wanted.y - deadZone.y;
                else if (diff.y < -deadZone.y) focus.y = wanted.y + deadZone.y;
            }
        }

        // temblor en pixeles enteros. Usa tiempo real para que tambien se note en la pausa al congelarse
        Vector2 shake = Vector2.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            float amount = shakePixels * Mathf.Clamp01(shakeTimer / shakeTime);
            int pixels = Mathf.FloorToInt(amount);
            if (Random.value < amount - pixels) pixels++;
            Vector2 dir = Random.insideUnitCircle.normalized;
            shake = new Vector2(Mathf.Round(dir.x * pixels), Mathf.Round(dir.y * pixels)) / pixelsPerUnit;
        }

        transform.position = new Vector3(Snap(focus.x) + shake.x, Snap(focus.y) + shake.y, transform.position.z);
    }
}
