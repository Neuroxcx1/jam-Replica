using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Replica > Crear escena prototipo
// Monta sprites, prefabs y un nivel de prueba para no tener que hacerlo a mano.
public static class PrototypeBuilder
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string TemplateScenePath = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";

    static readonly Vector2 CharacterSize = new Vector2(0.8f, 1f);

    static readonly Color GroundColor = new Color(0.28f, 0.27f, 0.36f);
    static readonly Color BoxColor = new Color(0.42f, 0.38f, 0.55f);
    static readonly Color PlayerColor = new Color(0.35f, 0.85f, 1f);
    // replicas y cuerpos en gris neutro: el color lo pone el material (Shader Graph ReplicaSprite)
    static readonly Color CloneColor = new Color(0.5f, 0.5f, 0.5f);
    static readonly Color BodyColor = new Color(0.5f, 0.5f, 0.5f);
    static readonly Color EyeColor = new Color(0.08f, 0.08f, 0.12f);
    static readonly Color SpikeColor = new Color(0.95f, 0.3f, 0.35f);
    static readonly Color FlagColor = new Color(1f, 0.85f, 0.3f);
    static readonly Color GoalColor = new Color(1f, 0.75f, 0.15f);

    static Sprite square;
    static Sprite spike;
    static int groundLayer;
    static PhysicsMaterial2D noFriction;

    [MenuItem("Replica/Crear escena prototipo")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Replica", "Esto borra y regenera " + ScenePath + ". ¿Seguir?", "Sí", "Cancelar"))
            return;

        var scene = NewSceneFromTemplate(ScenePath);
        Player player = CreatePlayerAndPrefabs(new Vector3(0f, 0.5f));
        BuildLevel();
        SetupCamera(player.transform);

        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings(ScenePath, false);
        Selection.activeGameObject = player.gameObject;
        Debug.Log("Escena prototipo creada en " + ScenePath);
    }

    // ---------- lo comparten la escena de prueba y el laboratorio (LabBuilder) ----------

    static EffectsBuilder.EffectSet effects;
    static Clone clonePrefab;
    static GameObject bodyPrefab;

    public static int GroundLayer => groundLayer;
    public static Sprite Square => square;

    public static UnityEngine.SceneManagement.Scene NewSceneFromTemplate(string path)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(TemplateScenePath, path);
        return EditorSceneManager.OpenScene(path);
    }

    // sprites base, efectos, prefabs de replica y cuerpo, y el jugador
    public static Player CreatePlayerAndPrefabs(Vector3 position)
    {
        CreateFolder("Assets/Sprites");
        CreateFolder("Assets/Prefabs");
        CreateFolder("Assets/Physics");

        square = CreateSprite("Assets/Sprites/Square.png", false);
        spike = CreateSprite("Assets/Sprites/Spike.png", true);
        noFriction = CreateNoFrictionMaterial();
        groundLayer = AddLayer("Ground");

        effects = EffectsBuilder.Build();
        bodyPrefab = CreateBodyPrefab(effects);
        clonePrefab = CreateClonePrefab(bodyPrefab, effects);
        return CreatePlayer(position, clonePrefab, bodyPrefab, effects);
    }

    // otro jugador con los prefabs que ya se generaron (para una segunda escena, sin volver a crearlos)
    public static Player CreateAnotherPlayer(Vector3 position) => CreatePlayer(position, clonePrefab, bodyPrefab, effects);

    static void BuildLevel()
    {
        Transform level = new GameObject("Level").transform;

        Block(level, "Left Wall", -4, -6, -2, 15);
        Block(level, "Right Wall", 76, -6, 78, 15);
        Block(level, "Floor 1", -2, -6, 28, 0);

        // 0: zona de practica, cajas y plataformas para probar salto, coyote y buffer
        Transform practice = new GameObject("Practice").transform;
        practice.SetParent(level);
        Block(practice, "Box 1", 4, 0, 5, 1, BoxColor);
        Block(practice, "Box 2", 7, 0, 8.5f, 2, BoxColor);
        Block(practice, "Platform 1", 10.5f, 3, 13.5f, 3.5f, BoxColor);
        Block(practice, "Platform 2", 15.5f, 4.5f, 18, 5, BoxColor);
        Spikes(practice, 20, 23, 0);

        // 1: hueco normal
        CreateCheckpoint(level, 25);
        Block(level, "Pit 1", 28, -6, 30, -3);
        Spikes(level, 28, 30, -3);

        // 2: foso con techo bajo, no se puede saltar. Rellenarlo con replicas
        Block(level, "Floor 2", 30, -6, 40, 0);
        CreateCheckpoint(level, 33);
        Block(level, "Ceiling", 37, 2, 49, 8);
        Block(level, "Pit 2", 40, -6, 45, -1.5f);
        Spikes(level, 40, 45, -1.5f);

        // 3: pared alta. Hay que apilar cuerpos para subir
        Block(level, "Floor 3", 45, -6, 60, 0);
        CreateCheckpoint(level, 52);
        Block(level, "Cliff", 60, -6, 76, 4);
        CreateGoal(level, 73, 4);
    }

    static Player CreatePlayer(Vector3 position, Clone clonePrefab, GameObject bodyPrefab, EffectsBuilder.EffectSet fx)
    {
        var go = new GameObject("Player");
        go.transform.position = position;
        AddCharacterPhysics(go);
        Transform visual = CreateVisual(go.transform, PlayerColor, 3);
        Transform groundCheck = Child(go.transform, "GroundCheck", new Vector3(0f, -0.5f)).transform;
        var walkDust = (GameObject)PrefabUtility.InstantiatePrefab(fx.walkDust.gameObject, groundCheck);

        var machine = Child(go.transform, "StateMachine", Vector3.zero).AddComponent<StateMachine>();
        State idle = AddState<IdleState>(machine.transform, "Idle");
        SetRef(AddState<RunState>(machine.transform, "Run"), "walkDust", walkDust.GetComponent<ParticleSystem>());
        SetRef(AddState<JumpState>(machine.transform, "Jump"), "jumpDust", fx.jumpDust);
        SetRef(AddState<FallState>(machine.transform, "Fall"), "landDust", fx.landDust);
        DeadState dead = AddState<DeadState>(machine.transform, "Dead");
        SetRef(dead, "soulOrb", fx.soulOrb);
        SetRef(dead, "respawnEffect", fx.respawn);
        SetRef(machine, "initialState", idle);

        var player = go.AddComponent<Player>();
        SetRef(player, "groundCheck", groundCheck);
        SetRef(player, "visual", visual);
        SetRef(player, "clonePrefab", clonePrefab);
        SetRef(player, "bodyPrefab", bodyPrefab);
        SetRef(player, "splitEffect", fx.split);
        SetRef(player, "deathEffect", fx.death);
        SetRef(player, "freezeEffect", fx.freeze);
        SetRef(player, "recallEffect", fx.respawn);
        SetRef(player, "recallGhost", fx.recallGhost);

        var so = new SerializedObject(player);
        so.FindProperty("groundLayer").intValue = 1 << groundLayer;
        so.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    static Clone CreateClonePrefab(GameObject bodyPrefab, EffectsBuilder.EffectSet fx)
    {
        var go = new GameObject("Clone");
        AddCharacterPhysics(go);
        Transform visual = CreateVisual(go.transform, CloneColor, 2);
        foreach (SpriteRenderer sr in visual.GetComponentsInChildren<SpriteRenderer>()) sr.sharedMaterial = fx.cloneMaterial;

        var clone = go.AddComponent<Clone>();
        SetRef(clone, "bodyPrefab", bodyPrefab);
        SetRef(clone, "visual", visual);
        SetRef(clone, "solidifyEffect", fx.solidify);
        SetRef(clone, "impactEffect", fx.cloneImpact);
        var speedLines = (GameObject)PrefabUtility.InstantiatePrefab(fx.speedLines, go.transform);
        SetRef(clone, "speedLines", speedLines.GetComponent<ParticleSystem>());
        go.AddComponent<SpriteFlash>();
        var afterimage = go.AddComponent<Afterimage>();
        SetRef(afterimage, "source", visual.GetComponent<SpriteRenderer>());
        SetRef(afterimage, "material", fx.afterimageMaterial);

        return SavePrefab(go, "Assets/Prefabs/Clone.prefab").GetComponent<Clone>();
    }

    static GameObject CreateBodyPrefab(EffectsBuilder.EffectSet fx)
    {
        var go = new GameObject("Body");
        go.layer = groundLayer;
        // el sprite va en un hijo para pegarlo a la rejilla de pixeles sin tocar la fisica
        GameObject visual = Child(go.transform, "Visual", Vector3.zero);
        AddSprite(visual, CharacterSize, BodyColor, 1).sharedMaterial = fx.bodyMaterial;
        visual.AddComponent<PixelSnap>();
        go.AddComponent<BoxCollider2D>().size = CharacterSize;

        // solo cae en vertical, no se puede empujar
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.mass = 5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        go.AddComponent<Body>();

        return SavePrefab(go, "Assets/Prefabs/Body.prefab");
    }

    static void AddCharacterPhysics(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = CharacterSize - new Vector2(0.04f, 0.04f);
        col.edgeRadius = 0.02f;
        col.sharedMaterial = noFriction;
    }

    static Transform CreateVisual(Transform parent, Color color, int order)
    {
        GameObject visual = Child(parent, "Visual", Vector3.zero);
        AddSprite(visual, CharacterSize, color, order);
        visual.AddComponent<PixelSnap>();
        AddSprite(Child(visual.transform, "Eye", new Vector3(0.2f, 0.25f)), new Vector2(0.15f, 0.15f), EyeColor, order + 1);
        return visual.transform;
    }

    static void Block(Transform parent, string name, float x0, float y0, float x1, float y1)
    {
        Block(parent, name, x0, y0, x1, y1, GroundColor);
    }

    static void Block(Transform parent, string name, float x0, float y0, float x1, float y1, Color color)
    {
        var size = new Vector2(x1 - x0, y1 - y0);
        GameObject go = Child(parent, name, new Vector3(x0 + size.x / 2, y0 + size.y / 2));
        go.layer = groundLayer;
        AddSprite(go, size, color, 0);
        go.AddComponent<BoxCollider2D>().size = size;
    }

    static void Spikes(Transform parent, float x0, float x1, float y)
    {
        var size = new Vector2(x1 - x0, 0.5f);
        GameObject go = Child(parent, "Spikes", new Vector3(x0 + size.x / 2, y + size.y / 2));

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spike;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.color = SpikeColor;

        go.AddComponent<BoxCollider2D>().size = size;
        go.AddComponent<Hazard>();
    }

    static void CreateCheckpoint(Transform parent, float x)
    {
        GameObject go = Child(parent, "Checkpoint", new Vector3(x, 0.5f));
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1f, 2f);
        trigger.offset = new Vector2(0f, 0.5f);

        AddSprite(Child(go.transform, "Pole", new Vector3(-0.25f, 0.5f)), new Vector2(0.1f, 2f), Color.gray, 0);
        SpriteRenderer flag = AddSprite(Child(go.transform, "Flag", new Vector3(0.05f, 1.25f)), new Vector2(0.5f, 0.4f), FlagColor, 0);

        SetRef(go.AddComponent<Checkpoint>(), "flag", flag);
    }

    static void CreateGoal(Transform parent, float x, float groundY)
    {
        GameObject go = Child(parent, "Goal", new Vector3(x, groundY + 1f));
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1f, 2f);

        AddSprite(go, new Vector2(1f, 2f), GoalColor, 0);
        go.AddComponent<Goal>();
    }

    public static void SetupCamera(Transform target)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No se encontró la Main Camera");
            return;
        }

        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.11f, 0.16f);
        cam.transform.position = new Vector3(target.position.x, target.position.y + 1.5f, -10f);

        SetRef(cam.gameObject.AddComponent<CameraFollow>(), "target", target);

        // pixel art de 32 px por unidad: se renderiza a 960x540 (30x17 tiles de mapa) y se escala,
        // asi todo (efectos incluidos) sale en pixeles. En 1080p escala justo x2
        var pixelPerfect = cam.gameObject.AddComponent<PixelPerfectCamera>();
        pixelPerfect.assetsPPU = 32;
        pixelPerfect.refResolutionX = 960;
        pixelPerfect.refResolutionY = 540;
        pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
    }

    static SpriteRenderer AddSprite(GameObject go, Vector2 size, Color color, int order)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    static GameObject Child(Transform parent, string name, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go;
    }

    static T AddState<T>(Transform machine, string name) where T : State
    {
        return Child(machine, name, Vector3.zero).AddComponent<T>();
    }

    static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject SavePrefab(GameObject go, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Sprite CreateSprite(string path, bool triangle)
    {
        const int size = 32;
        var tex = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool filled = !triangle || Mathf.Abs(x + 0.5f - size / 2f) <= (size - y - 0.5f) / 2f;
                tex.SetPixel(x, y, filled ? Color.white : Color.clear);
            }
        }
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = triangle ? size * 2 : size;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;

        // FullRect hace falta para usar el modo Tiled en los pinchos
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static PhysicsMaterial2D CreateNoFrictionMaterial()
    {
        const string path = "Assets/Physics/NoFriction.physicsMaterial2D";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (material != null) return material;

        material = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static int AddLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing != -1) return existing;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(layer.stringValue)) continue;

            layer.stringValue = name;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            return i;
        }

        Debug.LogError("No quedan capas libres para " + name);
        return 0;
    }

    static void CreateFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(path));
    }

    // first = true la pone la primera (la que se abre al jugar la build)
    public static void AddSceneToBuildSettings(string path, bool first)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == path);
        if (first) scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        else scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
