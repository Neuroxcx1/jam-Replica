using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Replica > Crear menus de pausa y opciones: el prefab Resources/Menus (GameMenus), con el mismo estilo que el menu
// del principio. Replica > Botones de las replicas en el HUD: los recordatorios de Shift, Ctrl y Q debajo del contador.
public static partial class LabBuilder
{
    const string MenusPath = "Assets/Resources/Menus.prefab";
    const string MixerPath = "Assets/Sounds/Mezclador.mixer";
    const string HudPath = "Assets/Prefabs/HUD.prefab";
    const string MenuFontPath = "Assets/PixelUISciFiFree/Font/Pixel UI Sci-Fi TMP.asset";
    const string ControlIconsAssetPath = "Assets/Settings/Iconos de controles.asset";

    static readonly Color MenuGreen = new Color(0.45f, 1f, 0.6f);

    static AudioMixerGroup MixerGroup(string name)
    {
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        return mixer != null ? mixer.FindMatchingGroups(name).FirstOrDefault(g => g.name == name) : null;
    }

    [MenuItem("Replica/Crear menus de pausa y opciones")]
    static void BuildMenus()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MenuFontPath);
        var go = new GameObject("Menus", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        var music = go.AddComponent<AudioSource>();
        music.outputAudioMixerGroup = MixerGroup("Musica");
        music.loop = true;
        music.playOnAwake = false;

        Image background = UiBox(go.transform, "Fondo", new Color(0f, 0f, 0f, 0.7f));
        Stretch(background.rectTransform);

        // pausa, a la izquierda como el menu del principio
        Transform pause = UiPanel(go.transform, "Pausa");
        MenuText(pause, "Titulo", "PAUSA", 96, new Vector2(160f, 160f), Color.white, font);
        Button resume = MenuButton(pause, "CONTINUAR", new Vector2(160f, -16f), font);
        Button options = MenuButton(pause, "OPCIONES", new Vector2(160f, -88f), font);
        Button toMenu = MenuButton(pause, "MENU PRINCIPAL", new Vector2(160f, -160f), font);
        ((RectTransform)toMenu.transform).sizeDelta = new Vector2(700f, 64f);
        MenuText(pause, "Ayuda", "ESC / START     CONTINUAR", 24, new Vector2(164f, -400f), new Color(1f, 1f, 1f, 0.4f), font);

        // opciones: una barra por grupo del mezclador
        Transform optionsPanel = UiPanel(go.transform, "Opciones");
        MenuText(optionsPanel, "Titulo", "OPCIONES", 96, new Vector2(160f, 160f), Color.white, font);
        var (master, masterValue) = VolumeRow(optionsPanel, "GENERAL", 16f, font);
        var (musicSlider, musicValue) = VolumeRow(optionsPanel, "MUSICA", -64f, font);
        var (effects, effectsValue) = VolumeRow(optionsPanel, "EFECTOS", -144f, font);
        Button back = MenuButton(optionsPanel, "VOLVER", new Vector2(160f, -240f), font);
        MenuText(optionsPanel, "Ayuda", "IZQUIERDA / DERECHA  VOLUMEN     ESC / B  VOLVER", 24, new Vector2(164f, -400f), new Color(1f, 1f, 1f, 0.4f), font);

        var menus = go.AddComponent<GameMenus>();
        SetRef(menus, "mixer", AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath));
        SetRef(menus, "musicSource", music);
        SetRef(menus, "background", background.gameObject);
        SetRef(menus, "pausePanel", pause.gameObject);
        SetRef(menus, "resumeButton", resume);
        SetRef(menus, "optionsButton", options);
        SetRef(menus, "menuButton", toMenu);
        SetRef(menus, "optionsPanel", optionsPanel.gameObject);
        SetRef(menus, "master", master);
        SetRef(menus, "music", musicSlider);
        SetRef(menus, "effects", effects);
        SetRef(menus, "masterValue", masterValue);
        SetRef(menus, "musicValue", musicValue);
        SetRef(menus, "effectsValue", effectsValue);
        SetRef(menus, "backButton", back);

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        PrefabUtility.SaveAsPrefabAsset(go, MenusPath);
        Object.DestroyImmediate(go);
        Debug.Log("Menus de pausa y opciones en " + MenusPath);
    }

    // nombre, barra y porcentaje; la barra se elige con arriba/abajo y se mueve con izquierda/derecha
    static (Slider, TMP_Text) VolumeRow(Transform parent, string name, float y, TMP_FontAsset font)
    {
        MenuText(parent, name, name, 48, new Vector2(160f, y), new Color(0.55f, 0.6f, 0.65f), font);
        var go = new GameObject("Barra " + name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(600f, y);
        rect.sizeDelta = new Vector2(480f, 16f);

        Stretch(UiBox(go.transform, "Fondo", new Color(0.12f, 0.14f, 0.17f)).rectTransform);
        Transform fillArea = UiPanel(go.transform, "Relleno");
        Image fill = UiBox(fillArea, "Barra", new Color(0.3f, 0.75f, 0.45f));
        Stretch(fill.rectTransform);
        Transform handleArea = UiPanel(go.transform, "Recorrido");
        Image handle = UiBox(handleArea, "Tirador", Color.white);
        handle.rectTransform.sizeDelta = new Vector2(16f, 24f);

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.value = 1f;
        ColorBlock colors = slider.colors;
        colors.normalColor = new Color(0.55f, 0.6f, 0.65f);
        colors.highlightedColor = colors.selectedColor = MenuGreen;
        colors.pressedColor = Color.white;
        slider.colors = colors;

        // con esta letra los numeros salen muy juntos: un poco mas pequeños y separados
        TextMeshProUGUI value = MenuText(parent, "Valor " + name, "100%", 40, new Vector2(1120f, y), Color.white, font);
        value.characterSpacing = 12f;
        return (slider, value);
    }

    static Image UiBox(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static Transform UiPanel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);
        return go.transform;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    // ---------- HUD ----------

    // debajo del contador de replicas: que tecla (o boton) lanza, congela y recupera. Cambian solas al coger el mando
    [MenuItem("Replica/Botones de las replicas en el HUD")]
    static void AddReplicaHints()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MenuFontPath);
        var icons = AssetDatabase.LoadAssetAtPath<ControlIcons>(ControlIconsAssetPath);
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPath);
        Transform counter = hud.GetComponentsInChildren<Transform>(true).First(t => t.name == "ReplicaContador");
        Transform old = counter.parent.Find("Controles de replicas");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var group = new GameObject("Controles de replicas", typeof(RectTransform));
        group.transform.SetParent(counter.parent, false);
        var rect = (RectTransform)group.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = HintsPosition;
        rect.sizeDelta = new Vector2(200f, 60f);

        var rows = new[] { (Sign.Control.LanzarClon, "LANZAR"), (Sign.Control.Congelar, "CONGELAR"), (Sign.Control.Recuperar, "RECUPERAR") };
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new GameObject("Control " + rows[i].Item2, typeof(RectTransform));
            row.transform.SetParent(group.transform, false);
            var rowRect = (RectTransform)row.transform;
            rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -i * HintRowHeight);
            rowRect.sizeDelta = new Vector2(200f, HintRowHeight);

            Image icon = UiBox(row.transform, "Icono", Color.white);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(0f, 0.5f);
            icon.preserveAspect = true;

            var text = new GameObject("Texto", typeof(RectTransform));
            text.transform.SetParent(row.transform, false);
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0f, 0.5f);
            textRect.anchoredPosition = new Vector2(HintLabelX, 0f);
            textRect.sizeDelta = new Vector2(150f, HintRowHeight);
            var label = text.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = HintFontSize;
            label.text = rows[i].Item2;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(1f, 1f, 1f, 0.85f);

            var hint = row.AddComponent<ControlHint>();
            SetRef(hint, "icons", icons);
            SetRef(hint, "icon", icon);
            var so = new SerializedObject(hint);
            so.FindProperty("control").enumValueIndex = (int)rows[i].Item1;
            so.FindProperty("pixelSize").floatValue = HintPixelSize;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
        PrefabUtility.UnloadPrefabContents(hud);
        Debug.Log("Botones de las replicas en " + HudPath);
    }

    // medidas en el canvas del HUD (1045 x 554): debajo del recuadro del contador, alineados con su borde.
    // Los textos en columna, despues de la tecla mas ancha (Shift y Ctrl, 32 px)
    static readonly Vector2 HintsPosition = new Vector2(78f, -84f);
    const float HintLabelX = 38f;
    const float HintRowHeight = 18f;
    const float HintPixelSize = 1f;
    const float HintFontSize = 10f;
}
