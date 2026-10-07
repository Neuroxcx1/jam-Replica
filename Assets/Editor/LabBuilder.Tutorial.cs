using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Replica > Crear nivel tutorial
// Crea los prefabs de los elementos del nivel en Assets/Prefabs/Nivel y monta con ellos la escena "Nivel Tutorial".
// Los prefabs (y los materiales del agua y el acido) solo se crean si no existen: si retocas uno no se pisa.
// Replica > Regenerar prefabs del nivel los vuelve a crear como salen de aqui.
// Para usarlos en otra escena basta con arrastrarlos. El agua, las plataformas, la cinta y el acido se estiran
// con la herramienta Rect (tecla T) y el collider y la luz se ajustan solos (ResizableArea).
public static partial class LabBuilder
{
    const string TutorialPath = "Assets/Scenes/Nivel Tutorial.unity";
    const string LevelFolder = "Assets/Prefabs/Nivel";
    const string PlayerPrefabPath = "Assets/Prefabs/Nivel/Jugador.prefab";
    const string SignFontPath = "Assets/PixelUISciFiFree/Font/Pixel UI Sci-Fi TMP.asset";
    const string LiquidShaderPath = "Assets/Effects/Shaders/Liquid.shadergraph";
    const string PuddleMaterialPath = "Assets/Effects/Materials/Agua.mat";
    const string StreamMaterialPath = "Assets/Effects/Materials/Agua_Chorro.mat";
    const string AcidMaterialPath = "Assets/Effects/Materials/Acido.mat";
    const int TutorialWidth = 58;
    const int TutorialHeight = 30;
    const float ElevatorHeight = 2.25f;

    static readonly Color WaterColor = new Color(0.45f, 0.75f, 1f);
    static readonly Color WarmColor = new Color(1f, 0.7f, 0.4f);
    static readonly Color WarmLampColor = new Color(1f, 0.84f, 0.64f);
    static readonly Color AcidColor = new Color(0.55f, 1f, 0.3f);

    static GameObject puddlePrefab, streamPrefab, electricPrefab, elevatorPrefab, terminalPrefab, securityDoorPrefab, hoistPrefab, laserPrefab, steamPrefab,
        conveyorPrefab, acidPrefab, zoneDoorPrefab, finalDoorPrefab, signPrefab;
    static bool overwriteLevelAssets;

    [MenuItem("Replica/Crear nivel tutorial")]
    static void BuildTutorialMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(TutorialPath) &&
            !EditorUtility.DisplayDialog("Replica", "Esto borra y regenera " + TutorialPath + " (los prefabs que ya existan no se tocan). ¿Seguir?", "Sí", "Cancelar"))
            return;

        art = LabAssets.Build();
        BuildTutorial();
        Debug.Log("Nivel tutorial creado en " + TutorialPath);
    }

    // el agua se recalcula sola al moverla o estirarla; esto lo fuerza en todas
    [MenuItem("Replica/Recalcular agua")]
    static void RecalculateWater()
    {
        LiquidSurface any = Object.FindAnyObjectByType<LiquidSurface>();
        if (any != null) any.RecalculateAll();
        SceneView.RepaintAll();
    }

    // Replica > Nueva escena de nivel: una escena lista para jugar con el jugador, la camara, las luces, el HUD
    // y una sala de ejemplo. Luego se pinta con la paleta "Laboratorio" y se colocan los prefabs de Assets/Prefabs/Nivel
    [MenuItem("Replica/Nueva escena de nivel")]
    static void NewLevelMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string path = EditorUtility.SaveFilePanelInProject("Nueva escena de nivel", "Nivel nuevo", "unity", "Donde se guarda la escena", "Assets/Scenes");
        if (!string.IsNullOrEmpty(path)) NewLevel(path);
    }

    static void NewLevel(string path)
    {
        var scene = PrototypeBuilder.NewSceneFromTemplate(path);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath));
        player.transform.position = new Vector3(3.5f, 2.5f);
        LoadCommon();
        art = LabAssets.Build();

        NewMap(40, 12);
        Carve(2, 2, 37, 9);
        Lamp(8f, 10, false);
        Lamp(20f, 10, false);
        Lamp(32f, 10, false);
        PaintTiles();

        SetupLighting(player.GetComponent<Player>(), false);
        SetupCamera(player.GetComponent<Player>(), 40);
        AddVolume(AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath));
        Hud();

        EditorSceneManager.SaveScene(scene);
        PrototypeBuilder.AddSceneToBuildSettings(path, false);
        Debug.Log("Escena nueva en " + path + ": pinta las salas con la paleta Laboratorio y arrastra los prefabs de " + LevelFolder);
    }

    // las torretas sueltas de la escena abierta (las copiadas del laboratorio) pasan a ser el prefab Torreta,
    // en el mismo sitio y con sus ajustes (hacia donde mira, alcance, tiempos...)
    [MenuItem("Replica/Torretas de la escena a prefab")]
    static void TurretsToPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{LevelFolder}/Torreta.prefab");
        if (prefab == null)
        {
            Debug.LogError("Falta el prefab " + LevelFolder + "/Torreta.prefab");
            return;
        }
        int count = 0;
        foreach (Turret old in Object.FindObjectsByType<Turret>(FindObjectsInactive.Include))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(old)) continue;
            Transform t = old.transform;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, t.gameObject.scene);
            go.transform.SetParent(t.parent, false);
            go.transform.SetSiblingIndex(t.GetSiblingIndex());
            go.transform.SetPositionAndRotation(t.position, t.rotation);
            go.transform.localScale = t.localScale;
            go.name = old.name;

            var from = new SerializedObject(old);
            var to = new SerializedObject(go.GetComponent<Turret>());
            foreach (string field in new[] { "facingRight", "range", "maxAngle", "chargeTime", "lockTime", "cooldown", "sightMask", "targetMask", "sightColor", "shakePixels" })
                to.CopyFromSerializedProperty(from.FindProperty(field));
            to.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(go, "Torretas a prefab");
            Undo.DestroyObjectImmediate(old.gameObject);
            count++;
        }
        Debug.Log(count + " torretas pasadas al prefab Torreta");
    }

    [MenuItem("Replica/Regenerar prefabs del nivel")]
    static void RebuildLevelPrefabsMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("Replica", "Esto vuelve a crear los prefabs de " + LevelFolder + " (pisa lo que hayas retocado en ellos) y regenera " + TutorialPath + ". ¿Seguir?", "Sí", "Cancelar"))
            return;

        overwriteLevelAssets = true;
        art = LabAssets.Build();
        BuildTutorial();
        overwriteLevelAssets = false;
    }

    static void BuildTutorial()
    {
        var scene = PrototypeBuilder.NewSceneFromTemplate(TutorialPath);
        Player player = PrototypeBuilder.CreatePlayerAndPrefabs(new Vector3(3.5f, 5.5f));
        LoadCommon();
        CreateLevelPrefabs();

        NewMap(TutorialWidth, TutorialHeight);
        FloodedRoom();
        ElevatorShaft();
        SecurityRoom();
        LastRoom();
        PaintTiles();

        SetupLighting(player);
        // el jugador (con su luz) tambien como prefab: lo usa Replica > Nueva escena de nivel
        PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject, PlayerPrefabPath, InteractionMode.AutomatedAction);
        SetupCamera(player, TutorialWidth);
        AddVolume(AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath));
        Hud();
        AddAftershock(player, 2f, 18f, 11.8f);

        EditorSceneManager.SaveScene(scene);
        PrototypeBuilder.AddSceneToBuildSettings(TutorialPath, false);
        // la partida empieza en la intro, que lleva aqui
        if (File.Exists(IntroPath)) PrototypeBuilder.AddSceneToBuildSettings(IntroPath, true);
    }

    // ---------- salas del tutorial ----------

    // Sala 1 - La fuga. Vienes de la sala del tanque. Una tuberia rota ha encharcado el suelo y el agua tiene corriente:
    // se cruza sobre un cuerpo (los cuerpos no conducen) o congelandote encima del agua.
    static void FloodedRoom()
    {
        Carve(2, 5, 17, 11);
        LockedDoor(2, 5);
        Cartel(6f, 9f, "CONTROLES",
            (Sign.Control.Saltar, "SALTAR"),
            (Sign.Control.Congelar, "CLON CONGELADO"),
            (Sign.Control.Recuperar, "RECUPERAR CLON"));

        Stretch(puddlePrefab, 6.25f, 5f, 14.25f, 5.625f);  // el charco (superficie a 5.5)
        Stretch(streamPrefab, 10f, 5.5f, 11f, 7.75f);      // lo que cae de la tuberia
        BrokenPipe(10, 8, 11);

        TutorialDoor(18, 5);
        Prop("escritorio", 15.5f, 5, false);
        Lamp(5f, 12, false);
        Lamp(13.5f, 12, true);
    }

    // Hueco de los ascensores, en dos pisos separados por un forjado. Abajo se cae al fondo y se sube congelandote
    // (cada copia es un escalon) y por los ascensores, que bajan un poco al pisarlos, hasta la sala de seguridad.
    // De ella se sale al piso de arriba, donde el agua que cae de la pared encharca el techo de un ascensor,
    // se derrama por su borde y acaba en un charco en el suelo. La luz de esta zona es algo mas calida.
    static void ElevatorShaft()
    {
        Carve(19, 0, 27, 21);
        Fill(19, 13, 27, 13);                              // forjado: techo del piso de abajo y suelo del de arriba (a 14)
        Fill(19, 4, 20, 4);                                // rellano a la salida de la puerta (suelo a 5)
        FloorStrip(electricPrefab, 21f, 28f, 0f);          // el fondo tiene corriente a ratos

        Elevator(19f, 21.5f, 9f);
        Elevator(25.5f, 28f, 10.3f);                       // pisado queda a ras de la puerta de la sala de seguridad
        Elevator(25f, 28f, 20.3f);                         // pisado queda a ras de la puerta de la ultima sala

        GameObject flooded = Elevator(19f, 22f, 16.6f);
        Attach(Stretch(puddlePrefab, 19f, 16.6f, 22f, 16.975f), flooded);
        Attach(Stretch(streamPrefab, 22f, 14.25f, 22.75f, 16.85f), flooded);
        Stretch(streamPrefab, 19f, 16.85f, 20.25f, 19.75f);
        Stretch(puddlePrefab, 19f, 14f, 23.5f, 14.375f);
        BrokenPipe(19, 20, 21);

        TutorialDoor(28, 10);
        TutorialDoor(28, 14);
        TutorialDoor(28, 20);

        Lamp(22.5f, 13, false, WarmLampColor);
        Lamp(21.5f, 22, false, WarmLampColor);
        Lamp(25.5f, 22, false, WarmLampColor);
        WarmFill(23f, 7.5f);
        WarmFill(23f, 18f);
    }

    // relleno calido suave para un piso del hueco (con sombras: no se sale a las otras salas)
    static void WarmFill(float x, float y)
    {
        Light2D fill = AddLight(lights, "Luz calida del hueco", new Vector3(x, y), new Color(1f, 0.62f, 0.32f), 0.45f, 8f);
        fill.falloffIntensity = 0.3f;
        fill.shadowsEnabled = true;
        fill.shadowIntensity = 1f;
    }

    // Sala de seguridad. La salida esta en la repisa de arriba, detras de una puerta de seguridad que solo se abre
    // mientras haya algo en el computador del fondo: hay que dejarle un clon. Por medio, dos chorros de vapor del techo.
    static void SecurityRoom()
    {
        Carve(29, 10, 40, 15);
        Fill(29, 13, 31, 13);                              // repisa (suelo a 14)

        GameObject door = Place(securityDoorPrefab, 31.5f, 14f);
        GameObject terminal = Place(terminalPrefab, 40.5f, 10f);
        SetRef(terminal.GetComponent<SecurityTerminal>(), "door", door.GetComponent<SecurityDoor>());
        Cartel(38.5f, 13.25f, "SEGURIDAD", (Sign.Control.LanzarClon, "CLON AL COMPUTADOR"));

        CeilingSteam(33.5f, 16f, 0f);
        CeilingSteam(35.5f, 16f, 1.5f);

        Prop("consola", 30.5f, 10, false);
        Lamp(35f, 16, false);
        Lamp(39f, 16, true);
        HorizontalPipe(32, 40, 15);
    }

    // Ultima sala. El montacargas sube al bloque de los laseres si cargas el contrapeso con dos replicas; los laseres
    // del techo se apagan a ratos; la cinta lleva al tanque de acido, que se cruza sobre cuerpos flotando.
    static void LastRoom()
    {
        Carve(29, 20, 54, 27);
        Fill(34, 20, 38, 23);                              // bloque de los laseres (arriba a 24)
        Fill(47, 20, 47, 23);                              // pared del tanque
        Fill(54, 20, 54, 24);                              // repisa de la salida (arriba a 25)

        // montacargas: la cabina (x 30-31) sube 4 desde un foso de 1 y el contrapeso (x 32-33, contra el bloque: las
        // replicas que lanzas hacia alli se quedan encima) baja 2 en un foso de 3. En x 29 queda suelo para esperar
        Carve(30, 19, 31, 19);
        Carve(32, 17, 33, 19);
        Place(hoistPrefab, 32f, 20f);
        Cartel(31.5f, 26.25f, "MONTACARGAS", (Sign.Control.LanzarClon, "CLONES AL CONTRAPESO"));

        CeilingLaser(35.5f, 28f, 1.2f, 1.4f, 0f);
        CeilingLaser(38f, 28f, 1.2f, 1.4f, 1.3f);
        Stretch(conveyorPrefab, 39f, 23f, 47f, 24f);
        CeilingSteam(43f, 28f, 0.5f);

        // tanque: el acido llega a 23, uno por debajo del borde, asi lo que cae se hunde dentro y flota alli
        TankBack(48f, 54f, 20f, 24f);
        Stretch(acidPrefab, 48f, 20f, 54f, 23.125f);
        TankWall(47.5f, 20f, 24f);
        TankWall(54.5f, 20f, 24f);

        Carve(55, 25, 55, 26);
        Place(finalDoorPrefab, 55.5f, 25f);

        Prop("cajas", 41.5f, 20, false);
        Prop("barril_toxico", 45f, 20, false);
        Lamp(30f, 28, false);
        Lamp(41f, 28, false);
        Lamp(46f, 28, true);
        Lamp(51.5f, 28, false);
        HorizontalPipe(39, 53, 27);
    }

    // ---------- colocar los prefabs ----------

    static GameObject Place(GameObject prefab, float x, float y, float angle = 0f, Transform parent = null)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent != null ? parent : traps);
        go.transform.SetPositionAndRotation(new Vector3(x, y), Quaternion.Euler(0f, 0f, angle));
        Keep(go.transform);
        return go;
    }

    // un prefab con ResizableArea estirado para ocupar de (x0, y0) a (x1, y1)
    static GameObject Stretch(GameObject prefab, float x0, float y0, float x1, float y1)
    {
        GameObject go = Place(prefab, (x0 + x1) / 2f, (y0 + y1) / 2f);
        go.GetComponent<SpriteRenderer>().size = new Vector2(x1 - x0, y1 - y0);
        go.GetComponent<ResizableArea>().Sync();
        foreach (Component c in go.GetComponentsInChildren<Component>()) Keep(c);
        return go;
    }

    // los cambios hechos por codigo en un prefab de la escena hay que apuntarlos o se pierden al guardar
    static void Keep(Object changed) => PrefabUtility.RecordPrefabInstancePropertyModifications(changed);

    // lo que va encima de un ascensor baja con el
    static void Attach(GameObject child, GameObject platform)
    {
        child.transform.SetParent(platform.transform, true);
        Keep(child.transform);
    }

    static GameObject Elevator(float x0, float x1, float top) => Stretch(elevatorPrefab, x0, top - ElevatorHeight, x1, top);

    // puerta de zona en la pared x (hueco de 2 desde y). left = se cruza hacia la izquierda (Scale X a -1)
    // puerta de zona en la pared x (hueco de 2 desde y): se cierra en el sentido en que la cruces
    static void TutorialDoor(int x, int y)
    {
        Carve(x, y, x, y + 1);
        Place(zoneDoorPrefab, x + 0.5f, y);
    }

    // un prefab que solo se estira a lo ancho (suelo electrico) de x0 a x1 sobre el suelo y
    static GameObject FloorStrip(GameObject prefab, float x0, float x1, float y)
    {
        GameObject go = Place(prefab, (x0 + x1) / 2f, y);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.size = new Vector2(x1 - x0, sr.size.y);
        go.GetComponent<ResizableArea>().Sync();
        foreach (Component c in go.GetComponentsInChildren<Component>()) Keep(c);
        return go;
    }

    // laser del techo hacia abajo que se enciende y apaga
    static void CeilingLaser(float x, float ceilingY, float onTime, float offTime, float delay)
    {
        var laser = Place(laserPrefab, x, ceilingY - 0.1f, 180f).GetComponent<SecurityLaser>();
        var so = new SerializedObject(laser);
        so.FindProperty("onTime").floatValue = onTime;
        so.FindProperty("offTime").floatValue = offTime;
        so.FindProperty("startDelay").floatValue = delay;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CeilingSteam(float x, float ceilingY, float delay)
    {
        var vent = Place(steamPrefab, x, ceilingY - 0.35f, 180f).GetComponent<SteamVent>();
        var so = new SerializedObject(vent);
        so.FindProperty("startDelay").floatValue = delay;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // tuberia que baja del techo hasta la casilla y0 y acaba rota (de ahi sale el agua)
    static void BrokenPipe(int x, int y0, int y1)
    {
        VerticalPipe(x, y0, y1);
        AddSprite(NewObject(decor, "Tuberia rota", new Vector3(x + 0.5f, y0), 0), art.nozzle, 3);
    }

    // fondo del tanque (se ve por encima del liquido, hasta el borde)
    static void TankBack(float x0, float x1, float y0, float y1)
    {
        SpriteRenderer sr = AddSprite(NewObject(decor, "Fondo del tanque", new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f), 0), art.tankBack, -15);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(x1 - x0, y1 - y0);
    }

    // chapa del tanque por encima de la roca de sus paredes
    static void TankWall(float x, float y0, float y1)
    {
        SpriteRenderer sr = AddSprite(NewObject(decor, "Pared del tanque", new Vector3(x, (y0 + y1) / 2f), 0), art.tankWall, 1);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(1f, y1 - y0);
    }

    // ---------- prefabs de los elementos ----------

    static void CreateLevelPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(LevelFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Nivel");
        puddlePrefab = LevelPrefab("Charco electrificado", CreatePuddle);
        streamPrefab = LevelPrefab("Chorro electrificado", CreateStream);
        electricPrefab = LevelPrefab("Suelo electrico", CreateElectricFloor);
        elevatorPrefab = LevelPrefab("Ascensor", CreateElevator);
        terminalPrefab = LevelPrefab("Computador de seguridad", CreateSecurityTerminal);
        securityDoorPrefab = LevelPrefab("Puerta de seguridad", CreateSecurityDoor);
        hoistPrefab = LevelPrefab("Montacargas", CreateHoist);
        laserPrefab = LevelPrefab("Laser", CreateLaser);
        steamPrefab = LevelPrefab("Vapor", CreateSteam);
        conveyorPrefab = LevelPrefab("Cinta transportadora", CreateConveyor);
        acidPrefab = LevelPrefab("Tanque de acido", CreateAcid);
        zoneDoorPrefab = LevelPrefab("Puerta de zona", CreateZoneDoor);
        finalDoorPrefab = LevelPrefab("Puerta final", CreateFinalDoor);
        LevelPrefab("Muestra de mutageno", CreateSample);
        signPrefab = LevelPrefab("Cartel", CreateSignPrefab);
        LevelPrefab("Torreta", CreateTurret);
    }

    static GameObject LevelPrefab(string name, System.Func<GameObject> create)
    {
        if (!AssetDatabase.IsValidFolder(LevelFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Nivel");
        string path = $"{LevelFolder}/{name}.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null && !overwriteLevelAssets) return prefab;

        GameObject go = create();
        prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // charco de agua con corriente: mata al jugador y a las replicas; los cuerpos no conducen y se puede pisar encima
    static GameObject CreatePuddle()
    {
        GameObject go = Liquid("Charco electrificado", PuddleMaterial(), new Vector2(3f, 0.5f), 0.125f, false);
        go.AddComponent<Hazard>();
        ElectricGlow(go);
        return go;
    }

    // agua con corriente que cae (de una tuberia rota, por el borde de una plataforma...); al final suelta gotas
    static GameObject CreateStream()
    {
        GameObject go = Liquid("Chorro electrificado", StreamMaterial(), new Vector2(1f, 3f), 0f, true);
        go.AddComponent<Hazard>();
        ElectricGlow(go);
        SetRef(go.GetComponent<ResizableArea>(), "bottom", Drops(go.transform).transform);
        return go;
    }

    // suelo electrico: con corriente (a pulsos) mata al que lo pisa; los cuerpos no conducen. Se estira a lo ancho
    static GameObject CreateElectricFloor()
    {
        const float width = 4f;
        var go = NewObject(null, "Suelo electrico", Vector3.zero, noRaycast);
        SpriteRenderer plate = AddSprite(go, art.electricPlate, 1);
        plate.drawMode = SpriteDrawMode.Tiled;
        plate.size = new Vector2(width, 6f / 32f);

        SpriteRenderer arcs = AddSprite(NewObject(go.transform, "Chispas", Vector3.zero, 0), art.electricArcs[0], 5);
        arcs.drawMode = SpriteDrawMode.Tiled;
        arcs.size = new Vector2(width, 0.5f);
        arcs.sharedMaterial = unlit;
        Animate(arcs.gameObject, art.electricArcs, 14f);

        // franja fina pegada al suelo: subido a un cuerpo ya no la tocas
        var zone = go.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.size = new Vector2(width, 0.3f);
        zone.offset = new Vector2(0f, 0.15f);
        go.AddComponent<Hazard>();
        Light2D glow = AddLight(go.transform, "Brillo", new Vector3(0f, 0.4f), new Color(0.45f, 0.8f, 1f), 0.9f, width / 2f + 1.5f);

        var floor = go.AddComponent<ElectricFloor>();
        SetRef(floor, "arcs", arcs);
        SetRef(floor, "glow", glow);
        var area = go.AddComponent<ResizableArea>();
        SetRef(area, "sprite", plate);
        SetRef(area, "area", zone);
        SetArray(area, "alsoResize", new[] { arcs });
        SetRef(area, "glow", glow);
        var so = new SerializedObject(area);
        so.FindProperty("widthOnly").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // un sprite que se estira con el shader de liquidos y un trigger que llega justo hasta la superficie.
    // falls: es un chorro que cae (se junta solo con los charcos que toca: salpica, se derrama...)
    static GameObject Liquid(string name, Material material, Vector2 size, float below, bool falls)
    {
        var go = NewObject(null, name, Vector3.zero, noRaycast);
        SpriteRenderer sr = AddSprite(go, PrototypeBuilder.Square, 4);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.sharedMaterial = material;
        var surface = go.AddComponent<LiquidSurface>();
        var so = new SerializedObject(surface);
        so.FindProperty("below").floatValue = below;
        so.FindProperty("falls").boolValue = falls;
        so.ApplyModifiedPropertiesWithoutUndo();

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;
        col.offset = new Vector2(0f, -below);
        var area = go.AddComponent<ResizableArea>();
        SetRef(area, "sprite", sr);
        SetRef(area, "area", col);
        return go;
    }

    // brillo azul que parpadea a ratos, como la corriente
    static void ElectricGlow(GameObject go)
    {
        Light2D glow = AddLight(go.transform, "Brillo", Vector3.zero, WaterColor, 0.8f, 3f);
        var flicker = go.AddComponent<LightFlicker>();
        SetRef(flicker, "lamp", glow);
        var so = new SerializedObject(flicker);
        so.FindProperty("waitRange").vector2Value = new Vector2(0.3f, 1.5f);
        so.ApplyModifiedPropertiesWithoutUndo();
        SetRef(go.GetComponent<ResizableArea>(), "glow", glow);
    }

    // gotas que se separan al final del chorro (o salpican donde cae)
    static ParticleSystem Drops(Transform parent)
    {
        var ps = NewObject(parent, "Gotas", parent.position, 0).AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(1f / 32f, 2f / 32f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.65f, 0.88f, 1f, 0.9f), new Color(0.3f, 0.6f, 0.95f, 0.8f));
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 25f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.7f, 0.05f, 0f);
        shape.randomDirectionAmount = 1f;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = lines;
        renderer.sortingOrder = 5;
        return ps;
    }

    static Material PuddleMaterial() => LiquidMaterial(PuddleMaterialPath, m =>
    {
        WaterColors(m);
        m.SetFloat("_OpacityTop", 0.55f);
        m.SetFloat("_OpacityDeep", 0.85f);
        m.SetFloat("_Depth", 0.6f);
        m.SetFloat("_Taper", 48f);
        m.SetFloat("_Electric", 0.35f);
        m.SetFloat("_ArcDepth", 5f);
    });

    static Material StreamMaterial() => LiquidMaterial(StreamMaterialPath, m =>
    {
        WaterColors(m);
        m.SetFloat("_OpacityTop", 0.7f);
        m.SetFloat("_OpacityDeep", 0.7f);
        m.SetFloat("_Depth", 4f);
        m.SetFloat("_Electric", 0.5f);
        m.SetFloat("_Fall", 1f);
        m.SetFloat("_FallSpeed", 6f);
    });

    static void WaterColors(Material m)
    {
        m.SetColor("_ColorSurface", new Color(0.35f, 0.65f, 0.95f));
        m.SetColor("_ColorDeep", new Color(0.08f, 0.2f, 0.5f));
        m.SetColor("_ColorFoam", new Color(0.85f, 0.95f, 1f));
        m.SetColor("_ColorSpark", new Color(0.85f, 1f, 1f));
    }

    // material con el shader de liquidos: se crea una vez (los retoques se respetan) salvo al regenerar
    static Material LiquidMaterial(string path, System.Action<Material> setup)
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(LiquidShaderPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null && !overwriteLevelAssets) return material;
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        setup(material);
        EditorUtility.SetDirty(material);
        return material;
    }

    // ascensor por fuera: una cabina cerrada que se pisa por el techo y baja un poco con peso, colgada de dos cables.
    // Se estira con la herramienta Rect (la chapa se estira por el medio y las puertas se quedan abajo en el centro)
    static GameObject CreateElevator()
    {
        var go = NewObject(null, "Ascensor", Vector3.zero, ground);
        SpriteRenderer sr = AddSprite(go, art.elevatorCabin, 1);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(2.5f, ElevatorHeight);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = sr.size;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var doors = NewObject(go.transform, "Puertas", new Vector3(0f, -ElevatorHeight / 2f), 0);
        AddSprite(doors, art.elevatorDoors, 2);
        // la luz calida de dentro sale por las ventanitas
        AddLight(doors.transform, "Luz de dentro", new Vector3(0f, -ElevatorHeight / 2f + 0.9f), WarmColor, 0.9f, 2.5f);

        var elevator = go.AddComponent<SinkingPlatform>();
        SetArray(elevator, "cables", new[] { ElevatorCable(go.transform), ElevatorCable(go.transform) });
        var area = go.AddComponent<ResizableArea>();
        SetRef(area, "sprite", sr);
        SetRef(area, "area", col);
        SetRef(area, "bottom", doors.transform);
        return go;
    }

    // cable de acero mas claro que la pared, para que se vea que la cabina cuelga
    static LineRenderer ElevatorCable(Transform parent)
    {
        LineRenderer cable = Cable(parent, "Cable");
        cable.startColor = cable.endColor = new Color(0.42f, 0.46f, 0.53f);
        cable.widthMultiplier = 2f / 32f;
        return cable;
    }

    // el computador rojo de siempre con un marco blanco y la placa donde hay que dejar el clon
    // al abrirse la pantalla pasa a verde, el marco y la placa se encienden y un cable lleva la senal a la puerta
    static GameObject CreateSecurityTerminal()
    {
        var go = NewObject(null, "Computador de seguridad", Vector3.zero, 0);
        AddSprite(go, art.terminal[0], -8);
        Animate(go, art.terminal, 8f);
        SpriteRenderer frame = AddSprite(NewObject(go.transform, "Marco", new Vector3(0f, -6f / 32f), 0), art.securityFrame, -7);
        frame.sharedMaterial = unlit;
        SpriteRenderer pad = AddSprite(NewObject(go.transform, "Placa", Vector3.zero, 0), art.securityPad, -6);
        Light2D screen = AddLight(go.transform, "Pantalla", new Vector3(0.2f, 0.6f), AlarmColor, 0.8f, 1.8f);
        // por la pared y el techo hasta la puerta: se coloca al empezar, cuando ya se sabe donde esta la puerta
        LineRenderer cable = Line(go.transform, "Cable a la puerta", 2f / 32f);
        cable.sortingOrder = -9;
        cable.enabled = true;

        var terminal = go.AddComponent<SecurityTerminal>();
        SetRef(terminal, "screenLight", screen);
        SetRef(terminal, "screen", go.GetComponent<SpriteLoop>());
        SetArray(terminal, "lockedFrames", art.terminal);
        SetArray(terminal, "openFrames", art.terminalOpen);
        SetArray(terminal, "marks", new[] { frame, pad });
        SetRef(terminal, "cable", cable);
        return go;
    }

    // puerta de 2 de alto; la hoja sube y se esconde en el techo (va por detras de los tiles)
    static GameObject CreateSecurityDoor()
    {
        var go = NewObject(null, "Puerta de seguridad", Vector3.zero, 0);
        var panel = NewObject(go.transform, "Hoja", Vector3.zero, 0);
        AddSprite(panel, art.securityDoor, -1);
        var blocker = NewObject(go.transform, "Bloqueo", Vector3.zero, ground).AddComponent<BoxCollider2D>();
        blocker.size = new Vector2(0.8f, 2f);
        blocker.offset = new Vector2(0f, 1f);
        Light2D lamp = AddLight(go.transform, "Piloto", new Vector3(0f, 1.8f), AlarmColor, 0.9f, 2.5f);

        var door = go.AddComponent<SecurityDoor>();
        SetRef(door, "panel", panel.transform);
        SetRef(door, "blocker", blocker);
        SetRef(door, "lamp", lamp);
        return go;
    }

    // montacargas de obra: plataforma con barandas a la izquierda y contrapeso a la derecha, colgados de una polea en lo
    // alto del mastil. El prefab va a ras del suelo; en la escena hacen falta los fosos (plataforma 1 de hondo, contrapeso 3)
    static GameObject CreateHoist()
    {
        const float pulleyHeight = 7.5f;
        var go = NewObject(null, "Montacargas", Vector3.zero, 0);

        // el mastil va por detras de todo (tambien de los carteles de la pared)
        SpriteRenderer mast = AddSprite(NewObject(go.transform, "Mastil", new Vector3(0f, (pulleyHeight - 3f) / 2f), 0), art.piston, -12);
        mast.drawMode = SpriteDrawMode.Tiled;
        mast.size = new Vector2(12f / 32f, pulleyHeight + 3f);
        var pulley = NewObject(go.transform, "Polea", new Vector3(0f, pulleyHeight), 0);
        AddSprite(pulley, art.pulley, 1);

        // por detras del terreno (orden -1): lo que baja por debajo del foso no se ve
        var weight = NewObject(go.transform, "Contrapeso", new Vector3(1f, 0f), ground);
        AddSprite(weight, art.liftWeight, -1);
        var weightCol = weight.AddComponent<BoxCollider2D>();
        weightCol.size = new Vector2(2f, 0.25f);
        weightCol.offset = new Vector2(0f, -0.125f);

        var car = NewObject(go.transform, "Cabina", new Vector3(-1f, 0f), ground);
        AddSprite(NewObject(car.transform, "Jaula", new Vector3(-1f, -10f / 32f), 0), art.liftCage, -1);
        var carCol = car.AddComponent<BoxCollider2D>();
        carCol.size = new Vector2(2f, 10f / 32f);
        carCol.offset = new Vector2(0f, -5f / 32f);
        var hook = NewObject(car.transform, "Gancho", new Vector3(-1f, 37f / 32f), 0);

        var lift = go.AddComponent<CounterweightLift>();
        SetRef(lift, "car", Kinematic(car));
        SetRef(lift, "counterweight", Kinematic(weight));
        SetRef(lift, "pulley", pulley.transform);
        SetRef(lift, "carHook", hook.transform);
        // los cables por detras de los carteles de la pared, para que no crucen su texto
        LineRenderer carCable = Cable(go.transform, "Cable cabina");
        LineRenderer weightCable = Cable(go.transform, "Cable contrapeso");
        carCable.sortingOrder = weightCable.sortingOrder = -11;
        SetRef(lift, "carCable", carCable);
        SetRef(lift, "counterweightCable", weightCable);
        var so = new SerializedObject(lift);
        so.FindProperty("carTravel").floatValue = 4f;
        so.FindProperty("counterweightTravel").floatValue = 2f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    static Rigidbody2D Kinematic(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        return rb;
    }

    // dispara hacia arriba desde el emisor; girado 180 grados dispara desde el techo hacia abajo.
    // El receptor se coloca solo donde el rayo choca con la pared
    static GameObject CreateLaser()
    {
        var go = NewObject(null, "Laser", Vector3.zero, 0);
        var emitter = NewObject(go.transform, "Emisor", new Vector3(0f, -0.1f), 0);
        AddSprite(emitter, art.laserBase[0], 3);
        Animate(emitter, art.laserBase, 12f);
        var receiver = NewObject(go.transform, "Receptor", new Vector3(0f, 4f), 0);
        AddSprite(receiver, art.laserTop[0], 3);
        Animate(receiver, art.laserTop, 12f);

        var beam = NewObject(go.transform, "Rayo", new Vector3(0f, 2f), 0);
        SpriteRenderer beamSr = AddSprite(beam, art.laserBeam[0], 5);
        beamSr.sharedMaterial = unlit;
        beamSr.drawMode = SpriteDrawMode.Tiled;
        beamSr.size = new Vector2(6f / 32f, 4f);
        Animate(beam, art.laserBeam, 16f);
        Transform impact = AddLight(go.transform, "Impacto", new Vector3(0f, 4f), AlarmColor, 1.4f, 1.2f).transform;
        Light2D glow = AddLight(go.transform, "Luz del rayo", new Vector3(0f, 2f), AlarmColor, 0.6f, 3f);

        var mount = new SerializedObject(go.AddComponent<SurfaceMount>());
        mount.FindProperty("offset").floatValue = 0.1f;
        mount.ApplyModifiedPropertiesWithoutUndo();
        var laser = go.AddComponent<SecurityLaser>();
        SetRef(laser, "beam", beamSr);
        SetRef(laser, "impact", impact);
        SetRef(laser, "beamLight", glow);
        SetRef(laser, "receiver", receiver.transform);
        var so = new SerializedObject(laser);
        so.FindProperty("blockers").intValue = (1 << ground) | 1;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // tuberia de vapor que sale hacia arriba; girada 180 grados sale del techo hacia abajo.
    // El chorro llega solo hasta la primera pared que tenga delante
    static GameObject CreateSteam()
    {
        var go = NewObject(null, "Vapor", Vector3.zero, 0);
        var nozzle = NewObject(go.transform, "Boca", new Vector3(0f, -0.35f), 0);
        nozzle.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        AddSprite(nozzle, art.nozzle, 3);

        go.AddComponent<SurfaceMount>();
        var vent = go.AddComponent<SteamVent>();
        SetRef(vent, "steam", SteamParticles(go.transform));
        var so = new SerializedObject(vent);
        so.FindProperty("length").floatValue = 30f;
        so.FindProperty("onTime").floatValue = 1f;
        so.FindProperty("offTime").floatValue = 2f;
        so.FindProperty("blockers").intValue = 1 << ground;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // con velocidad negativa va hacia la izquierda
    static GameObject CreateConveyor()
    {
        const float speed = 2.5f;
        var go = NewObject(null, "Cinta transportadora", Vector3.zero, ground);
        SpriteRenderer sr = AddSprite(go, art.belt[0], 1);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(4f, 1f);
        Animate(go, art.belt, 16f * speed);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = sr.size;

        var conveyor = go.AddComponent<Conveyor>();
        var so = new SerializedObject(conveyor);
        so.FindProperty("speed").floatValue = speed;
        so.ApplyModifiedPropertiesWithoutUndo();

        var area = go.AddComponent<ResizableArea>();
        SetRef(area, "sprite", sr);
        SetRef(area, "area", col);
        return go;
    }

    // acido: deshace al jugador y a las replicas y los cuerpos flotan encima como balsas. Va dentro de un tanque,
    // con la superficie por debajo del borde: asi lo que cae se hunde dentro y flota alli, no en el borde
    static GameObject CreateAcid()
    {
        GameObject go = Liquid("Tanque de acido", AcidMaterial(), new Vector2(4f, 3f), 0.125f, false);
        go.AddComponent<WastePool>();
        Light2D glow = AddLight(go.transform, "Brillo", Vector3.zero, AcidColor, 0.9f, 3.5f);
        glow.falloffIntensity = 0.45f;
        SetRef(go.GetComponent<ResizableArea>(), "glow", glow);
        return go;
    }

    static Material AcidMaterial() => LiquidMaterial(AcidMaterialPath, m =>
    {
        m.SetColor("_ColorSurface", new Color(0.55f, 0.95f, 0.25f));
        m.SetColor("_ColorDeep", new Color(0.1f, 0.32f, 0.08f));
        m.SetColor("_ColorFoam", new Color(0.85f, 1f, 0.6f));
        m.SetFloat("_OpacityTop", 0.85f);
        m.SetFloat("_OpacityDeep", 0.95f);
        m.SetFloat("_Depth", 2.5f);
        m.SetFloat("_WaveSpeed", 1.2f);
        m.SetFloat("_BubbleAmount", 0.5f);
    });

    // puerta de zona: se cierra detras de ti en el sentido en que la cruces y justo pasada la puerta queda el
    // checkpoint (y se recargan las replicas). Va en un hueco de 2 de alto en una pared
    static GameObject CreateZoneDoor()
    {
        var go = NewObject(null, "Puerta de zona", Vector3.zero, noRaycast);
        Sprite open = art.doorTurningOff[art.doorTurningOff.Length - 1];
        AddSprite(go, open, 1);
        var loop = go.AddComponent<SpriteLoop>();
        SetArray(loop, "frames", new[] { open });

        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(3f, 2f);
        trigger.offset = new Vector2(0f, 1f);
        var blocker = NewObject(go.transform, "Bloqueo", Vector3.zero, ground).AddComponent<BoxCollider2D>();
        blocker.size = new Vector2(1f, 2f);
        blocker.offset = new Vector2(0f, 1f);
        Light2D doorLight = AddLight(go.transform, "Luz", new Vector3(0f, 1.8f), OpenDoorColor, 0.9f, 3f);

        var door = go.AddComponent<ZoneDoor>();
        SetRef(door, "blocker", blocker);
        SetRef(door, "barrier", loop);
        SetRef(door, "doorLight", doorLight);
        SetArray(door, "closingFrames", Enumerable.Reverse(art.doorTurningOff).ToArray());
        SetArray(door, "closedFrames", art.doorClosed);
        return go;
    }

    // salida del nivel: el panel de victoria es el del HUD (hay que enlazarlo en el inspector)
    static GameObject CreateFinalDoor()
    {
        var go = NewObject(null, "Puerta final", Vector3.zero, noRaycast);
        AddSprite(go, art.doorTurningOff[art.doorTurningOff.Length - 1], 1);
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(0.6f, 2f);
        trigger.offset = new Vector2(0f, 1f);
        go.AddComponent<FinalDoor>();

        Light2D exit = AddLight(go.transform, "Luz de fuera", new Vector3(0f, 1.5f), ExitColor, 1.6f, 7f);
        exit.volumetricEnabled = true;
        exit.volumeIntensity = 0.12f;
        return go;
    }

    static GameObject CreateSample()
    {
        var go = NewObject(null, "Muestra de mutageno", Vector3.zero, noRaycast);
        var trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.45f;
        var visual = NewObject(go.transform, "Vial", Vector3.zero, 0);
        AddSprite(visual, art.replicaSample, 5).sharedMaterial = unlit;
        AddLight(go.transform, "Brillo", Vector3.zero, new Color(0.35f, 1f, 0.55f), 0.8f, 2.5f);

        var pickup = go.AddComponent<ReplicaPickup>();
        SetRef(pickup, "visual", visual.transform);
        SetRef(pickup, "collectEffect", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Effects/Prefabs/Absorb.prefab"));
        return go;
    }

    // un cartel en (x, y) con su titulo y sus filas: el control cuyas teclas salen y el texto
    static void Cartel(float x, float y, string title, params (Sign.Control control, string text)[] rows)
    {
        if (signPrefab == null) signPrefab = LevelPrefab("Cartel", CreateSignPrefab);
        var so = new SerializedObject(Place(signPrefab, x, y, 0f, decor).GetComponent<Sign>());
        so.FindProperty("title").stringValue = title;
        SerializedProperty list = so.FindProperty("rows");
        list.arraySize = rows.Length;
        for (int i = 0; i < rows.Length; i++)
        {
            list.GetArrayElementAtIndex(i).FindPropertyRelative("control").enumValueIndex = (int)rows[i].control;
            list.GetArrayElementAtIndex(i).FindPropertyRelative("text").stringValue = rows[i].text;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // cartel de papel editable (componente Sign): titulo, hasta 5 filas de texto y dos teclas por fila.
    // El tamaño y la colocacion de cada pieza los pone el componente segun el texto
    static GameObject CreateSignPrefab()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SignFontPath);
        var go = NewObject(null, "Cartel", Vector3.zero, 0);
        SpriteRenderer shadow = SignPaper(go.transform, "Sombra", -10);
        shadow.color = new Color(0f, 0f, 0f, 0.35f);
        SpriteRenderer paper = SignPaper(go.transform, "Papel", -9);
        SpriteRenderer band = AddSprite(NewObject(go.transform, "Franja del titulo", Vector3.zero, 0), PrototypeBuilder.Square, -8);
        band.drawMode = SpriteDrawMode.Sliced;
        band.color = new Color(0.62f, 0.17f, 0.19f);
        TextMeshPro title = SignText(go.transform, "Titulo", font, new Color(0.84f, 0.82f, 0.77f), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f));
        SpriteRenderer leftTape = AddSprite(NewObject(go.transform, "Cinta izquierda", Vector3.zero, 0), art.signTape, -6);
        SpriteRenderer rightTape = AddSprite(NewObject(go.transform, "Cinta derecha", Vector3.zero, 0), art.signTape, -6);
        rightTape.flipX = true;

        var texts = new TMP_Text[5];
        var keys = new SpriteRenderer[10];
        for (int i = 0; i < texts.Length; i++)
        {
            var row = NewObject(go.transform, "Fila " + (i + 1), Vector3.zero, 0);
            texts[i] = SignText(row.transform, "Texto", font, new Color(0.16f, 0.17f, 0.22f), TextAlignmentOptions.Left, new Vector2(0f, 0.5f));
            for (int k = 0; k < 2; k++) keys[i * 2 + k] = AddSprite(NewObject(row.transform, "Tecla " + (k + 1), Vector3.zero, 0), null, -7);
        }

        var sign = go.AddComponent<Sign>();
        SetRef(sign, "icons", art.controlIcons);
        SetRef(sign, "paper", paper);
        SetRef(sign, "shadow", shadow);
        SetRef(sign, "band", band);
        SetRef(sign, "titleText", title);
        SetRef(sign, "leftTape", leftTape.transform);
        SetRef(sign, "rightTape", rightTape.transform);
        SetArray(sign, "texts", texts);
        SetArray(sign, "keys", keys);
        return go;
    }

    static SpriteRenderer SignPaper(Transform parent, string name, int order)
    {
        SpriteRenderer sr = AddSprite(NewObject(parent, name, Vector3.zero, 0), art.signPaper, order);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(4f, 2f);
        return sr;
    }

    // texto con la letra pixelada: a tamaño 2.5 cada pixel de la letra es un pixel del juego
    static TextMeshPro SignText(Transform parent, string name, TMP_FontAsset font, Color color, TextAlignmentOptions alignment, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshPro>();
        text.font = font;
        text.fontSize = 2.5f;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.pivot = pivot;
        text.rectTransform.sizeDelta = new Vector2(12f, 1f);
        text.sortingOrder = -7;
        return text;
    }
}
