using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Cartel de papel con texto que se escribe en el Inspector (titulo y hasta 5 filas). Cada fila lleva un control del
// jugador y un texto corto al lado ("RECUPERAR CLON"): se dibujan las teclas que tenga asignadas ahora mismo, del teclado
// o del mando de Xbox segun lo ultimo que se haya tocado (si coges el mando, todos los carteles pasan a sus botones).
// El papel se ajusta solo al texto.
[ExecuteAlways]
public class Sign : MonoBehaviour
{
    public enum Control { Ninguno, Moverse, Saltar, LanzarClon, Congelar, Recuperar, Reiniciar }

    [Serializable]
    public class Row
    {
        [Tooltip("Sus teclas salen a la izquierda del texto")]
        public Control control;
        [Tooltip("Corto: lo que hace esa tecla (\"RECUPERAR CLON\")")]
        public string text;
    }

    // medidas en pixeles del juego (32 por casilla). Las teclas van a su tamano, sin escalar, para que no se vean borrosas
    const float Pixel = 1f / 32f;
    const int Pad = 6, BandHeight = 14, RowHeight = 20, KeyGap = 2, TextGap = 6;

    [Tooltip("En mayusculas y sin tildes: la letra pixelada no las tiene")]
    [SerializeField] string title = "CONTROLES";
    [SerializeField] Row[] rows = { new Row { control = Control.Saltar, text = "SALTAR" } };

    [Header("Piezas del cartel (no hace falta tocarlas)")]
    [SerializeField] ControlIcons icons;
    [SerializeField] SpriteRenderer paper;
    [SerializeField] SpriteRenderer shadow;
    [SerializeField] SpriteRenderer band;
    [SerializeField] TMP_Text titleText;
    [SerializeField] Transform leftTape;
    [SerializeField] Transform rightTape;
    [SerializeField] TMP_Text[] texts;          // una por fila
    [SerializeField] SpriteRenderer[] keys;     // dos por fila

    static bool gamepad;
    static int checkedFrame = -1;
    string shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetDevice() => gamepad = false;

    void Update()
    {
        if (Application.isPlaying) CheckDevice();

        // solo se vuelve a montar si cambia el texto o las teclas que tocan
        var rowIcons = new List<Sprite>[Mathf.Min(rows.Length, texts.Length)];
        string now = title;
        for (int i = 0; i < rowIcons.Length; i++)
        {
            rowIcons[i] = Icons(rows[i].control);
            now += "|" + rows[i].text + string.Join(",", rowIcons[i].ConvertAll(s => s != null ? s.name : "-"));
        }
        if (now == shown) return;
        shown = now;
        Build(rowIcons);
    }

    // el ultimo dispositivo con el que se ha tocado algo (una vez por fotograma para todos los carteles)
    static void CheckDevice()
    {
        if (checkedFrame == Time.frameCount) return;
        checkedFrame = Time.frameCount;
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) gamepad = false;
        Gamepad pad = Gamepad.current;
        if (pad == null) return;
        foreach (var button in new[] { pad.buttonSouth, pad.buttonNorth, pad.buttonEast, pad.buttonWest, pad.leftShoulder,
                                       pad.rightShoulder, pad.startButton, pad.selectButton })
            if (button.wasPressedThisFrame) gamepad = true;
        if (pad.leftStick.ReadValue().magnitude > 0.5f || pad.dpad.ReadValue().magnitude > 0.5f) gamepad = true;
    }

    List<Sprite> Icons(Control control)
    {
        var found = new List<Sprite>();
        if (control == Control.Ninguno || icons == null) return found;
        foreach (string path in Paths(ActionName(control), gamepad)) found.Add(icons.Find(IconName(path)));
        return found;
    }

    static string ActionName(Control control) => control switch
    {
        Control.Moverse => "Move",
        Control.Saltar => "Jump",
        Control.LanzarClon => "Replicate",
        Control.Congelar => "Freeze",
        Control.Recuperar => "Recall",
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

    void Build(List<Sprite>[] rowIcons)
    {
        int count = rowIcons.Length;
        for (int i = 0; i < texts.Length; i++) texts[i].gameObject.SetActive(i < count);

        // columna de teclas y columna de texto
        int keysWidth = 0;
        for (int i = 0; i < count; i++) keysWidth = Mathf.Max(keysWidth, IconsWidth(rowIcons[i]));
        int textX = Pad + keysWidth + (keysWidth > 0 ? TextGap : 0);
        int textWidth = 0;
        for (int i = 0; i < count; i++) textWidth = Mathf.Max(textWidth, Pixels(texts[i].GetPreferredValues(rows[i].text).x));
        titleText.text = title;
        int width = Mathf.Max(textX + textWidth + Pad, Pixels(titleText.GetPreferredValues(title).x) + 2 * Pad + 16);
        width += width % 2;
        int height = BandHeight + Pad + count * RowHeight;

        // el papel, centrado en el objeto
        float left = -width / 2f * Pixel, top = height / 2f * Pixel;
        paper.size = shadow.size = new Vector2(width, height) * Pixel;
        shadow.transform.localPosition = new Vector3(2f, -2f) * Pixel;
        band.size = new Vector2(width - 2, BandHeight - 1) * Pixel;
        band.transform.localPosition = new Vector3(0f, top - (BandHeight + 1) / 2f * Pixel);
        titleText.transform.localPosition = new Vector3(0f, top - BandHeight / 2f * Pixel);
        leftTape.localPosition = new Vector3(left + 3 * Pixel, top - 4 * Pixel);
        rightTape.localPosition = new Vector3(-left - 3 * Pixel, top - 4 * Pixel);

        for (int i = 0; i < texts.Length; i++)
        {
            bool used = i < count;
            float y = top - (BandHeight + Pad / 2 + RowHeight * i + RowHeight / 2f) * Pixel;
            if (used)
            {
                texts[i].text = rows[i].text;
                texts[i].transform.localPosition = new Vector3(left + textX * Pixel, y);
            }
            float x = left + Pad * Pixel;
            for (int k = 0; k < 2; k++)
            {
                SpriteRenderer key = keys[i * 2 + k];
                Sprite sprite = used && k < rowIcons[i].Count ? rowIcons[i][k] : null;
                key.gameObject.SetActive(sprite != null);
                if (sprite == null) continue;
                key.sprite = sprite;
                key.transform.localScale = Vector3.one;
                float keyWidth = KeyWidth(sprite) * Pixel;
                key.transform.localPosition = new Vector3(x + keyWidth / 2f, y);
                x += keyWidth + KeyGap * Pixel;
            }
        }
    }

    static int Pixels(float units) => Mathf.CeilToInt(units / Pixel);

    static int KeyWidth(Sprite sprite) => Mathf.RoundToInt(sprite.bounds.size.x / Pixel);

    static int IconsWidth(List<Sprite> icons)
    {
        int width = 0;
        foreach (Sprite sprite in icons) if (sprite != null) width += KeyWidth(sprite) + KeyGap;
        return Mathf.Max(0, width - KeyGap);
    }
}
