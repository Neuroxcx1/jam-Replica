using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector2 offset = new Vector2(0f, 1.5f);
    [SerializeField] float pixelsPerUnit = 32f;

    // mientras el jugador este dentro de esta zona la camara no se mueve
    [SerializeField] Vector2 deadZone = new Vector2(1.5f, 3f);
    // se adelanta hacia donde corres para ver antes lo que viene
    [SerializeField] float lookAhead = 4f;
    [SerializeField] float lookAheadSpeed = 8f;
    // pisando suelo se vuelve a centrar en vertical: asi siempre ves lo que hay debajo
    [SerializeField] float groundRecenterSpeed = 8f;
    // al reaparecer lejos: rapido al principio y frena al llegar, pero nunca mas lento que esto
    // (tiene que ser mas rapido que el jugador para que siempre lo alcance)
    [SerializeField] float recenterSpeed = 16f;
    // x minima y maxima del centro de la camara: asi no se ve la roca de fuera del nivel
    [SerializeField] Vector2 limitsX = new Vector2(float.NegativeInfinity, float.PositiveInfinity);

    static CameraFollow current;

    Player player;
    Vector2? shot;
    Vector2 focus;
    float ahead;
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
        // el mando vibra con el temblor: nada en los pequeños y fuerte en los grandes (lanzar un clon, morir)
        Rumble.Pulse(Mathf.Clamp01((pixels - 1f) / 5f), duration);
    }

    // plano fijo para las cinematicas: la camara se queda en ese punto en vez de seguir al jugador (null = seguirlo)
    public static void SetShot(Vector2? point)
    {
        if (current != null) current.shot = point;
    }

    // a donde miraria la camara siguiendo al jugador (para que una cinematica acabe justo ahi, sin saltos)
    public static Vector2 FollowPoint()
    {
        Vector2 point = (Vector2)current.target.position + current.offset;
        point.x = Mathf.Clamp(point.x, current.limitsX.x, current.limitsX.y);
        return point;
    }

    void Awake()
    {
        current = this;
        focus = transform.position;
        if (target != null) player = target.GetComponent<Player>();
    }

    // a la rejilla de pixeles, igual que el sprite del jugador (PixelSnap), para que no tiemble en pantalla
    float Snap(float value) => Mathf.Round(value * pixelsPerUnit) / pixelsPerUnit;

    void LateUpdate()
    {
        if (shot.HasValue)
        {
            focus = shot.Value;
            ahead = 0f;
        }
        else if (target != null)
        {
            Vector2 wanted = new Vector2(Snap(target.position.x), Snap(target.position.y)) + offset;
            Vector2 diff = wanted - focus;

            // si el jugador aparece lejos (reaparecer en el checkpoint) se centra en el
            if (Mathf.Abs(diff.x) > deadZone.x * 2f + 1f || Mathf.Abs(diff.y) > deadZone.y * 2f) recentering = true;

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

                if (player != null && player.IsGrounded())
                    focus.y = Mathf.MoveTowards(focus.y, wanted.y, groundRecenterSpeed * Time.deltaTime);
            }

            // el adelanto se queda donde estaba cuando te paras, asi la camara no va y viene
            if (player != null && Mathf.Abs(player.Rb.linearVelocity.x) > 1f)
                ahead = Mathf.MoveTowards(ahead, Mathf.Sign(player.Rb.linearVelocity.x) * lookAhead, lookAheadSpeed * Time.deltaTime);
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

        // en los planos de cinematica la camara se mueve suave, sin ajustarse a la rejilla de pixeles
        float x = shot.HasValue ? focus.x : Mathf.Clamp(Snap(focus.x + ahead), limitsX.x, limitsX.y);
        float y = shot.HasValue ? focus.y : Snap(focus.y);
        transform.position = new Vector3(x + shake.x, y + shake.y, transform.position.z);
    }

}
