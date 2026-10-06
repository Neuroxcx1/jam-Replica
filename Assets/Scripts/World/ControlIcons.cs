using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Los iconos de las teclas y de los botones del mando de Xbox (hechos con los tiles pixel de Assets/Buttons/Tiles), por su
// nombre: "keyboard_space", "xbox_a"... Lo usan los carteles y el HUD. Lo genera Replica > Crear nivel tutorial.
// Tambien sabe con que se esta jugando: si tocas el mando salen sus botones, si tocas el teclado sus teclas.
public class ControlIcons : ScriptableObject
{
    [SerializeField] Sprite[] sprites;

    Dictionary<string, Sprite> byName;

    public static bool UsingGamepad { get; private set; }
    static int checkedFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetDevice() => UsingGamepad = false;

    public Sprite Find(string name)
    {
        if (byName == null)
        {
            byName = new Dictionary<string, Sprite>();
            foreach (Sprite sprite in sprites)
                if (sprite != null) byName[sprite.name] = sprite;
        }
        return byName.TryGetValue(name, out Sprite found) ? found : null;
    }

    // las teclas (o botones) que tiene ahora ese control: de Moverse, la de izquierda y la de derecha
    public List<Sprite> For(Sign.Control control)
    {
        var found = new List<Sprite>();
        if (control == Sign.Control.Ninguno) return found;
        foreach (string path in Paths(ActionName(control), UsingGamepad)) found.Add(Find(IconName(path)));
        return found;
    }

    // el ultimo dispositivo con el que se ha tocado algo (una vez por fotograma aunque lo pregunten muchos)
    public static void CheckDevice()
    {
        if (checkedFrame == Time.frameCount) return;
        checkedFrame = Time.frameCount;
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) UsingGamepad = false;
        Gamepad pad = Gamepad.current;
        if (pad == null) return;
        foreach (var button in new[] { pad.buttonSouth, pad.buttonNorth, pad.buttonEast, pad.buttonWest, pad.leftShoulder,
                                       pad.rightShoulder, pad.startButton, pad.selectButton })
            if (button.wasPressedThisFrame) UsingGamepad = true;
        if (pad.leftStick.ReadValue().magnitude > 0.5f || pad.dpad.ReadValue().magnitude > 0.5f) UsingGamepad = true;
    }

    static string ActionName(Sign.Control control) => control switch
    {
        Sign.Control.Moverse => "Move",
        Sign.Control.Saltar => "Jump",
        Sign.Control.LanzarClon => "Replicate",
        Sign.Control.Congelar => "Freeze",
        Sign.Control.Recuperar => "Recall",
        _ => "Restart",
    };

    // las teclas asignadas ahora mismo a la accion. De Moverse, la de izquierda y la de derecha (el juego es de lado)
    static List<string> Paths(string actionName, bool gamepad)
    {
        var paths = new List<string>();
        InputAction action = InputSystem.actions != null ? InputSystem.actions.FindAction(actionName) : null;
        if (action == null) return paths;
        string device = gamepad ? "<Gamepad>" : "<Keyboard>";
        string left = null, right = null;
        foreach (InputBinding binding in action.bindings)
        {
            string path = binding.effectivePath;
            if (string.IsNullOrEmpty(path) || !path.StartsWith(device)) continue;
            if (!binding.isPartOfComposite)
            {
                paths.Add(path);
                return paths;
            }
            if (binding.name == "left" && left == null) left = path;
            if (binding.name == "right" && right == null) right = path;
        }
        if (left != null) paths.Add(left);
        if (right != null) paths.Add(right);
        return paths;
    }

    // de "<Keyboard>/space" a "keyboard_space", de "<Gamepad>/buttonSouth" a "xbox_a"
    static string IconName(string path)
    {
        string control = path.Substring(path.IndexOf('/') + 1);
        if (path.StartsWith("<Gamepad>"))
            return "xbox_" + control switch
            {
                "buttonSouth" => "a",
                "buttonEast" => "b",
                "buttonWest" => "x",
                "buttonNorth" => "y",
                "leftShoulder" => "lb",
                "rightShoulder" => "rb",
                "leftTrigger" => "lt",
                "rightTrigger" => "rt",
                "select" => "view",
                "start" => "menu",
                "leftStick" => "stick_l",
                "rightStick" => "stick_r",
                _ => control.ToLower(),
            };
        return "keyboard_" + control.ToLower() switch
        {
            "leftarrow" => "arrow_left",
            "rightarrow" => "arrow_right",
            "uparrow" => "arrow_up",
            "downarrow" => "arrow_down",
            "leftshift" or "rightshift" => "shift",
            "leftctrl" or "rightctrl" => "ctrl",
            "leftalt" or "rightalt" => "alt",
            var key => key,
        };
    }

#if UNITY_EDITOR
    public void Set(Sprite[] all)
    {
        sprites = all;
        byName = null;
    }
#endif
}
