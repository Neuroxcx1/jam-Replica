using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Replica > Crear menus de pausa y opciones: el prefab Resources/Menus (GameMenus), con el mismo estilo que el menu
// del principio. Replica > Botones de las replicas en el HUD: las teclas de Shift, Ctrl y Q debajo del contador.
// Replica > Poner sonidos de la intro: crea los sonidos combinados (Sounds/Combinados) y los pone en la escena Intro.
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
        var ui = go.AddComponent<AudioSource>();
        ui.outputAudioMixerGroup = MixerGroup("Efectos");
        ui.playOnAwake = false;

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

        // al pulsar suena Select; en las que vuelven atras, Back
        foreach (Selectable option in go.GetComponentsInChildren<Selectable>(true))
            SetBool(option.gameObject.AddComponent<MenuSound>(), "back", option == resume || option == back);

        var menus = go.AddComponent<GameMenus>();
        SetRef(menus, "mixer", AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath));
        SetRef(menus, "musicSource", music);
        SetRef(menus, "uiSource", ui);
        SetRef(menus, "moveSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Interface/Holder.ogg"));
        SetRef(menus, "selectSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Interface/Select.ogg"));
        SetRef(menus, "backSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Interface/Back.ogg"));
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

    static void SetBool(Object target, string field, bool value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
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

    // debajo del recuadro del contador de replicas, pegadas a el: las teclas (o botones) de lanzar, congelar y recuperar.
    // Solo los iconos; cambian solos al coger el mando
    [MenuItem("Replica/Botones de las replicas en el HUD")]
    static void AddReplicaHints()
    {
        var icons = AssetDatabase.LoadAssetAtPath<ControlIcons>(ControlIconsAssetPath);
        GameObject hud = PrefabUtility.LoadPrefabContents(HudPath);
        Transform panel = hud.GetComponentsInChildren<Transform>(true).First(t => t.name == "Window" && t.parent.name == "ReplicaContador");
        foreach (Transform t in hud.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Controles de replicas").ToArray())
            Object.DestroyImmediate(t.gameObject);

        var row = new GameObject("Controles de replicas", typeof(RectTransform));
        row.transform.SetParent(panel, false);
        row.AddComponent<LayoutElement>().ignoreLayout = true;
        var rect = (RectTransform)row.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0f, -4f);
        rect.sizeDelta = new Vector2(150f, 16f);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        foreach (var (control, name) in new[] { (Sign.Control.LanzarClon, "Lanzar"), (Sign.Control.Congelar, "Congelar"), (Sign.Control.Recuperar, "Recuperar") })
        {
            Image icon = UiBox(row.transform, "Tecla " + name, Color.white);
            icon.raycastTarget = false;
            var hint = icon.gameObject.AddComponent<ControlHint>();
            SetRef(hint, "icons", icons);
            SetRef(hint, "icon", icon);
            var so = new SerializedObject(hint);
            so.FindProperty("control").enumValueIndex = (int)control;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
        PrefabUtility.UnloadPrefabContents(hud);
        Debug.Log("Botones de las replicas en " + HudPath);
    }

    // ---------- sonidos de la intro ----------

    const string MixesFolder = "Assets/Sounds/Combinados";

    static AudioClip Clip(string name) => AssetDatabase.FindAssets(name + " t:AudioClip", new[] { "Assets/Sounds" })
        .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(c => c.name == name);

    // una capa: sonido, retraso, volumen, tono, desde donde empieza, cuanto suena (0 = entero) y lo que tarda en apagarse
    static (string, float, float, float, float, float, float) L(string clip, float delay, float volume, float pitch = 1f, float start = 0f, float length = 0f, float fade = 0.2f)
        => (clip, delay, volume, pitch, start, length, fade);

    // crea (o rehace) un sonido combinado en Sounds/Combinados
    static SoundMix Mix(string name, float volume, params (string clip, float delay, float volume, float pitch, float start, float length, float fade)[] layers)
    {
        if (!AssetDatabase.IsValidFolder(MixesFolder)) AssetDatabase.CreateFolder("Assets/Sounds", "Combinados");
        string path = $"{MixesFolder}/{name}.asset";
        var mix = AssetDatabase.LoadAssetAtPath<SoundMix>(path);
        if (mix == null)
        {
            mix = ScriptableObject.CreateInstance<SoundMix>();
            AssetDatabase.CreateAsset(mix, path);
        }
        var so = new SerializedObject(mix);
        so.FindProperty("output").objectReferenceValue = MixerGroup("Efectos");
        so.FindProperty("volume").floatValue = volume;
        SerializedProperty list = so.FindProperty("layers");
        list.arraySize = layers.Length;
        for (int i = 0; i < layers.Length; i++)
        {
            SerializedProperty layer = list.GetArrayElementAtIndex(i);
            layer.FindPropertyRelative("clip").objectReferenceValue = Clip(layers[i].clip);
            layer.FindPropertyRelative("delay").floatValue = layers[i].delay;
            layer.FindPropertyRelative("volume").floatValue = layers[i].volume;
            layer.FindPropertyRelative("pitch").floatValue = layers[i].pitch;
            layer.FindPropertyRelative("start").floatValue = layers[i].start;
            layer.FindPropertyRelative("length").floatValue = layers[i].length;
            layer.FindPropertyRelative("fadeOut").floatValue = layers[i].fade;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mix);
        return mix;
    }

    static AudioSource Loop(Transform parent, string name, string clip, float volume, bool playNow)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var source = go.AddComponent<AudioSource>();
        source.clip = Clip(clip);
        source.outputAudioMixerGroup = MixerGroup("Efectos");
        source.loop = true;
        source.volume = volume;
        source.playOnAwake = playNow;
        return source;
    }

    // en la escena Intro abierta: el ambiente y las voces del menu, el temblor, la ventana, el tanque, los gritos
    // y los sonidos de las opciones del menu del principio
    [MenuItem("Replica/Poner sonidos de la intro")]
    static void AddIntroSounds()
    {
        var intro = Object.FindAnyObjectByType<IntroCinematic>(FindObjectsInactive.Include);
        if (intro == null)
        {
            Debug.LogError("Abre la escena Intro");
            return;
        }

        // el vidrio: los tres sonidos encima unos de otros, con trocitos que caen despues
        SoundMix windowCrack = Mix("Ventana rajada", 0.8f, L("Vidrio 3", 0f, 0.5f, 1.35f, 0f, 0.3f, 0.1f), L("Vidrio 2", 0.05f, 0.3f, 1.6f));
        SoundMix windowShatter = Mix("Vidrio fuerte", 0.9f,
            L("Vidrio_1", 0f, 1f, 1f, 0.25f), L("Vidrio 3", 0.02f, 0.9f, 0.95f), L("Vidrio 2", 0.06f, 0.8f, 1.1f),
            L("Vidrio 3", 0.18f, 0.45f, 1.3f), L("Vidrio 2", 0.32f, 0.35f, 1.5f));
        // el tanque con los mismos, pero mas grave, en otro orden y con el liquido saliendo: no suena igual que la ventana
        SoundMix tankCrack = Mix("Tanque rajado", 0.8f, L("Vidrio 2", 0f, 0.45f, 0.8f), L("Vidrio 3", 0.04f, 0.25f, 0.7f, 0f, 0.3f, 0.15f));
        SoundMix tankBreak = Mix("Tanque roto", 0.9f,
            L("Vidrio 3", 0f, 1f, 0.72f), L("Vidrio_1", 0.04f, 0.9f, 0.8f, 0.25f), L("Vidrio 2", 0.12f, 0.7f, 0.85f),
            L("Vidrio 2", 0.35f, 0.3f, 1f), L("Sonido de tanque de agua", 0f, 0.7f, 0.8f, 0f, 1.4f, 0.9f));
        // los dos temblores juntos: el segundo entra un poco despues y mas grave; suenan lo que dura el temblor y se apagan
        SoundMix quake = Mix("Temblor", 1f, L("Temblor 1", 0f, 0.3f, 1f, 0f, 3.4f, 1.2f), L("Tembloe 2", 0.4f, 0.35f, 0.9f, 5f, 3.2f, 1.5f));
        // los gritos son largos: solo el primer segundo y se apagan
        SoundMix[] screams =
        {
            Mix("Grito 1", 1f, L("Gritos 1", 0f, 0.35f, 1f, 0f, 1f, 0.35f)),
            Mix("Grito 2", 1f, L("Gritos 2", 0f, 0.35f, 1.05f, 0.09f, 1f, 0.35f)),
            Mix("Grito 3", 1f, L("Gritos 3", 0f, 0.55f, 0.95f, 0.26f, 1.2f, 0.4f)),
        };
        AssetDatabase.SaveAssets();

        // la sala: ambiente y voces desde el menu; el zumbido de emergencia cuando vuelve la corriente
        Transform old = intro.transform.Find("Sonido de la sala");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var room = new GameObject("Sonido de la sala").transform;
        room.SetParent(intro.transform, false);
        SetRef(intro, "ambience", Loop(room, "Ambiente", "Sonido ambiente 2", 0.25f, true));
        SetArray(intro, "voices", new[] { Loop(room, "Voces", "Voces", 1f, true), Loop(room, "Voces 2", "Voces 2", 1f, true) });
        SetRef(intro, "emergencyAmbience", Loop(room, "Corriente de emergencia", "Sonido ambiente 1", 0.7f, false));
        SetRef(intro, "quakeSound", quake);

        var window = Object.FindAnyObjectByType<ObservationWindow>(FindObjectsInactive.Include);
        SetRef(window, "crackSound", windowCrack);
        SetRef(window, "shatterSound", windowShatter);
        var tank = Object.FindAnyObjectByType<SpecimenTank>(FindObjectsInactive.Include);
        SetRef(tank, "crackSound", tankCrack);
        SetRef(tank, "breakSound", tankBreak);

        // un grito por cientifico (los tres primeros que huyen; mas a la vez sonaria raro)
        var scientists = new SerializedObject(intro).FindProperty("scientists");
        for (int i = 0; i < scientists.arraySize; i++)
        {
            var scientist = (Scientist)scientists.GetArrayElementAtIndex(i).objectReferenceValue;
            if (scientist != null) SetRef(scientist, "scream", i < screams.Length ? screams[i] : null);
        }

        var title = Object.FindAnyObjectByType<TitleMenu>(FindObjectsInactive.Include);
        foreach (Selectable option in title.GetComponentsInChildren<Selectable>(true))
            if (option.GetComponent<MenuSound>() == null) option.gameObject.AddComponent<MenuSound>();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(intro.gameObject.scene);
        Debug.Log("Sonidos de la intro puestos");
    }
}
