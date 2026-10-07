using UnityEngine;
using UnityEngine.InputSystem;

// Vibracion del mando: Pulse(fuerza de 0 a 1, segundos) y se va apagando sola. Solo si se esta jugando con el mando
// (lo ultimo que se toco). Vibran las sacudidas de la camara, el salto y los menus.
// La mueve GameMenus con Tick() cada fotograma (esta en todas las escenas, tambien en la pausa).
public static class Rumble
{
    static float strength;
    static float time;
    static float timer;
    static bool on;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        strength = time = timer = 0f;
        on = false;
    }

    public static void Pulse(float amount, float seconds)
    {
        if (!ControlIcons.UsingGamepad || amount <= 0f || seconds <= 0f) return;
        // si ya vibra mas fuerte, se queda la que hay
        float left = timer > 0f ? strength * timer / time : 0f;
        if (amount < left) return;
        strength = amount;
        time = timer = seconds;
    }

    // corta lo que este vibrando (al abrir la pausa)
    public static void Stop() => timer = 0f;

    public static void Tick()
    {
        ControlIcons.CheckDevice();
        float now = 0f;
        if (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            now = strength * Mathf.Clamp01(timer / time);
        }
        if (!ControlIcons.UsingGamepad) now = 0f;
        if (now <= 0f && !on) return;
        // a todos los mandos: si hay dos, el "actual" puede cambiar y uno se quedaria vibrando
        foreach (Gamepad pad in Gamepad.all) pad.SetMotorSpeeds(now * 0.7f, now);
        on = now > 0f;
    }

    public static void Off()
    {
        timer = 0f;
        if (on) foreach (Gamepad pad in Gamepad.all) pad.SetMotorSpeeds(0f, 0f);
        on = false;
    }
}
