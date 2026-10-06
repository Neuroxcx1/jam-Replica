using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// Cinematica de la sala del tanque, vista desde la sala de observacion a traves de su ventana. Empieza con el menu
// del principio encima (TitleMenu) y al darle a Iniciar: primer plano del especimen (tu), la camara se aleja y se ven el marco y los cientificos pasando por delante. Tiembla todo, la
// ventana se raja y revienta, se va la luz (menos la verde del tanque) y evacuan; la camara se acerca al tanque,
// que se raja, lo revientas y la camara "atraviesa" la ventana hasta el plano de juego.
// Cualquier tecla se la salta. Solo se ve una vez por partida.
public class IntroCinematic : MonoBehaviour
{
    [SerializeField] Player player;
    // en el tanque esta acurrucado y al salir se despliega: la animacion de hacerse cubo al reves
    [SerializeField] Sprite[] wakeUp;
    [SerializeField] float wakeUpFps = 8f;
    [SerializeField] SpecimenTank tank;
    [SerializeField] ObservationWindow window;
    [SerializeField] Scientist[] scientists;
    [SerializeField] float[] exits;            // por donde sale corriendo cada cientifico
    [SerializeField] ParticleSystem dust;
    [SerializeField] PixelPerfectCamera pixelCamera;
    [SerializeField] GameObject menu;

    [Header("Apagon")]
    [SerializeField] Light2D globalLight;
    [SerializeField] Light2D[] lights;
    [SerializeField] SpriteRenderer[] glows;   // tubos y bombillas que brillan solos
    [SerializeField] Behaviour[] machines;     // parpadeos: sin corriente se paran

    [Header("Planos (zoom: veces mas cerca que al jugar)")]
    [SerializeField] float closeZoom = 4f;
    [SerializeField] float wideZoom = 2f;
    [SerializeField] float tankZoom = 2.6f;    // el tanque entero con el suelo: se ve como sales y caes
    // con el menu la camara va a la izquierda: el tanque queda a la derecha y el menu sobre la pared oscura
    [SerializeField] Vector2 menuOffset = new Vector2(-3f, 0f);
    [SerializeField] float floatHeight = 2.25f;

    [Header("Tiempos")]
    [SerializeField] float calmTime = 3f;
    [SerializeField] float quakeTime = 1.5f;
    [SerializeField] float darkTime = 2.2f;
    [SerializeField] float crackTime = 1f;
    [SerializeField] float powerBackDelay = 1.2f;

    static bool seen;

    Camera cam;
    float baseSize;
    float globalIntensity;
    Vector3 floatPosition;
    Vector2 shot;
    float fade;
    float skipFrom;
    bool floating;
    bool playing;

    // sin recarga de dominio (asi esta el proyecto) la variable sobreviviria entre partidas del editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSeen() => seen = false;

    // al volver al menu principal desde la pausa: otra vez el menu y la cinematica
    public static void ShowMenuAgain() => seen = false;

    void Start()
    {
        cam = pixelCamera.GetComponent<Camera>();
        baseSize = pixelCamera.refResolutionY / (2f * pixelCamera.assetsPPU);
        globalIntensity = globalLight.intensity;
        if (seen)
        {
            menu.SetActive(false);
            Release(true);
            return;
        }

        // el menu: la sala ya se ve detras, con el especimen acurrucado flotando en el tanque, y no se puede mover.
        // Sus estados (y sus animaciones) esperan a que salga
        floating = true;
        player.enabled = false;
        player.Rb.simulated = false;
        player.GetComponentInChildren<StateMachine>().enabled = false;
        player.Animate(new[] { wakeUp[0] }, 1f, true);
        floatPosition = player.transform.position + Vector3.up * floatHeight;

        // el Pixel Perfect Camera solo sabe acercar a saltos: durante la cinematica el zoom va con la camara normal
        pixelCamera.enabled = false;
        SetZoom(wideZoom);
        SetShot((Vector2)floatPosition + menuOffset);
        fade = 1f;
        StartCoroutine(Fade(0f, 1.5f));
    }

    // lo llama el boton Iniciar del menu
    public void Begin()
    {
        if (playing) return;
        seen = true;
        StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        // el mismo Enter de Iniciar no se tiene que saltar la cinematica
        playing = true;
        skipFrom = Time.time + 0.5f;
        Vector2 tankShot = floatPosition;

        // la camara se coloca y se acerca despacio al especimen
        yield return Frame(tankShot, closeZoom, 2.5f);
        yield return new WaitForSeconds(1f);

        // la camara se aleja: el tanque entero, el marco de la ventana y los cientificos pasando por delante;
        // mientras todo esta tranquilo va y vuelve un poco hacia un lado
        yield return Frame(tankShot, wideZoom, 3f);
        yield return Frame(tankShot + Vector2.right * 0.5f, wideZoom, calmTime / 2f);
        yield return Frame(tankShot, wideZoom, calmTime / 2f);

        // temblor: cae polvo, las luces parpadean y la camara se acerca un poco con el susto; la ventana se raja...
        CameraFollow.Shake(3f, quakeTime + darkTime);
        dust.Play();
        StartCoroutine(Frame(shot, wideZoom * 1.15f, quakeTime));
        yield return Flicker(quakeTime / 3f);
        window.Crack();
        yield return Flicker(quakeTime * 2f / 3f);

        // ...y revienta. Apagon: solo queda la luz verde del tanque y los cientificos evacuan
        window.Shatter();
        SetPower(false);
        for (int i = 0; i < scientists.Length; i++) scientists[i].Flee(exits[i]);
        yield return new WaitForSeconds(darkTime);

        // la camara se acerca despacio al tanque, que se raja
        yield return Frame(tankShot, tankZoom, 1.2f);
        tank.Crack();
        yield return new WaitForSeconds(crackTime);

        // revienta y el especimen sale de un salto
        dust.Stop();
        Release(false);
        yield return new WaitForSeconds(1f);

        // la camara atraviesa la ventana hasta el plano de juego
        yield return PassThroughWindow(1.2f);
        ShowGameplay();

        // vuelve la corriente de emergencia
        yield return new WaitForSeconds(powerBackDelay);
        for (int i = 0; i < 4; i++)
        {
            SetPower(i % 2 == 1);
            yield return new WaitForSeconds(0.08f);
        }
        SetPower(true);
    }

    void Update()
    {
        if (floating) player.transform.position = floatPosition + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.08f;
        if (!playing || Time.time < skipFrom) return;

        bool skip = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                    (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (!skip) return;
        StopAllCoroutines();
        Release(true);
    }

    // revienta el tanque y te devuelve el control. quiet: sin efectos ni salto, ya con el plano de juego
    void Release(bool quiet)
    {
        playing = false;
        floating = false;
        tank.Break(quiet);
        if (quiet)
        {
            foreach (Scientist scientist in scientists) scientist.gameObject.SetActive(false);
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            SetPower(true);
            ShowGameplay();
            fade = 0f;
        }

        player.Rb.simulated = true;
        player.enabled = true;
        if (quiet) Stand();
        else
        {
            // sale del tanque de un salto mientras se despliega
            player.Rb.linearVelocity = new Vector2(2.5f, 7f);
            StartCoroutine(WakeUp());
        }
    }

    IEnumerator WakeUp()
    {
        player.Animate(wakeUp, wakeUpFps, false);
        yield return new WaitForSeconds(wakeUp.Length / wakeUpFps);
        Stand();
    }

    // vuelven sus estados: el que toque (de pie, cayendo...) pone su animacion
    void Stand()
    {
        var states = player.GetComponentInChildren<StateMachine>();
        states.enabled = true;
        states.ChangeState("idle");
    }

    // el marco y la pared crecen cada vez mas rapido hasta salirse de la vista (parece que la camara pasa por la
    // ventana) mientras el plano se abre hasta donde la camara va a seguir al jugador
    IEnumerator PassThroughWindow(float time)
    {
        Vector2 fromShot = shot;
        float fromZoom = baseSize / cam.orthographicSize;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time, eased = Mathf.SmoothStep(0f, 1f, k);
            SetShot(Vector2.Lerp(fromShot, CameraFollow.FollowPoint(), eased));
            SetZoom(Mathf.Lerp(fromZoom, 1f, eased));
            window.transform.localScale = Vector3.one * (1f + 5f * k * k);
            yield return null;
        }
    }

    // la camara normal de juego: sin zoom, otra vez pixel perfect y siguiendo al jugador
    void ShowGameplay()
    {
        window.gameObject.SetActive(false);
        SetZoom(1f);
        pixelCamera.enabled = true;
        CameraFollow.SetShot(null);
    }

    IEnumerator Flicker(float time)
    {
        for (float t = 0f; t < time; t += 0.12f)
        {
            SetPower(Random.value < 0.6f);
            yield return new WaitForSeconds(0.12f);
        }
    }

    // lleva la camara a otro punto y otro zoom a la vez, sin cortes
    IEnumerator Frame(Vector2 target, float zoom, float time)
    {
        Vector2 fromShot = shot;
        float fromZoom = baseSize / cam.orthographicSize;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / time);
            SetShot(Vector2.Lerp(fromShot, target, k));
            SetZoom(Mathf.Lerp(fromZoom, zoom, k));
            yield return null;
        }
        SetShot(target);
        SetZoom(zoom);
    }

    void SetShot(Vector2 point)
    {
        shot = point;
        CameraFollow.SetShot(point);
    }

    void SetZoom(float zoom) => cam.orthographicSize = baseSize / zoom;

    IEnumerator Fade(float target, float time)
    {
        float from = fade;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            fade = Mathf.Lerp(from, target, t / time);
            yield return null;
        }
        fade = target;
    }

    void SetPower(bool on)
    {
        globalLight.intensity = on ? globalIntensity : globalIntensity * 0.2f;
        foreach (Light2D light in lights) light.enabled = on;
        foreach (SpriteRenderer glow in glows) glow.enabled = on;
        foreach (Behaviour machine in machines) machine.enabled = on;
    }

    void OnGUI()
    {
        if (fade <= 0f) return;
        GUI.color = new Color(0f, 0f, 0f, fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
    }
}
