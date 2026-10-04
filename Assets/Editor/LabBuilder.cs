using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

// Replica > Crear laboratorio
// Monta la escena "Laboratorio": 3 zonas seguidas pintadas con la Tile Palette "Laboratorio",
// con sus trampas, puertas y luces. Luego todo se puede retocar a mano (pintar tiles, mover trampas...),
// pero volver a ejecutar esto borra esos cambios.
//
// Coordenadas: 1 unidad = 1 tile de 32 px. (x, y) es la casilla; y crece hacia arriba.
public static class LabBuilder
{
    const string ScenePath = "Assets/Scenes/Laboratorio.unity";
    const string VolumePath = "Assets/Settings/Laboratorio_Postproceso.asset";
    const string WasteMaterialPath = "Assets/Effects/Materials/Desechos.mat";
    const int Width = 143;
    const int Height = 24;
    // roca de mas alrededor del mapa para que la camara nunca vea el vacio
    const int Margin = 14;

    static readonly Color AmbientColor = new Color(0.58f, 0.63f, 0.72f);
    static readonly Color LampColor = new Color(0.86f, 0.93f, 1f);
    static readonly Color AlarmColor = new Color(1f, 0.22f, 0.16f);
    static readonly Color WasteGlow = new Color(0.62f, 0.86f, 0.25f);
    static readonly Color OpenDoorColor = new Color(0.3f, 0.85f, 1f);
    static readonly Color ExitColor = new Color(1f, 0.94f, 0.82f);

    // alto y ancho en pixeles de lo que se ve de cada objeto de decorado (para su colision)
    static readonly Dictionary<string, Vector2Int> PropSize = new Dictionary<string, Vector2Int>
    {
        { "cajas", new Vector2Int(31, 26) }, { "caja", new Vector2Int(20, 13) }, { "consola", new Vector2Int(29, 21) },
        { "escritorio", new Vector2Int(29, 22) }, { "ordenador", new Vector2Int(24, 25) }, { "tanque", new Vector2Int(28, 32) },
        { "barriles", new Vector2Int(26, 17) }, { "barril_toxico", new Vector2Int(15, 17) },
    };

    static bool[,] solid;
    static List<(Vector3Int cell, TileBase tile)> pipes;
    static LabAssets.Set art;
    static Transform decor, traps, lights;
    static int ground, noRaycast;
    static Material unlit, lines, waste;

    [MenuItem("Replica/Crear laboratorio")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (System.IO.File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Replica", "Esto borra y regenera " + ScenePath + ". ¿Seguir?", "Sí", "Cancelar"))
            return;

        art = LabAssets.Build();
        var scene = PrototypeBuilder.NewSceneFromTemplate(ScenePath);
        Player player = PrototypeBuilder.CreatePlayerAndPrefabs(new Vector3(4.5f, 6.5f));

        ground = PrototypeBuilder.GroundLayer;
        noRaycast = LayerMask.NameToLayer("Ignore Raycast");
        unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        lines = AssetDatabase.LoadAssetAtPath<Material>("Assets/Effects/Materials/FX_Pixel.mat");
        waste = CreateWasteMaterial();

        decor = new GameObject("Decorado").transform;
        traps = new GameObject("Trampas y puertas").transform;
        lights = new GameObject("Luces").transform;
        solid = new bool[Width, Height];
        pipes = new List<(Vector3Int, TileBase)>();

        // todo es roca y las salas se excavan
        Fill(0, 0, Width - 1, Height - 1);
        Containment();
        Security();
        WasteZone();
        PaintTiles();

        SetupLighting(player);
        PrototypeBuilder.SetupCamera(player.transform);
        Camera.main.backgroundColor = new Color(0.06f, 0.065f, 0.08f);
        SetupPostProcessing();

        EditorSceneManager.SaveScene(scene);
        PrototypeBuilder.AddSceneToBuildSettings(ScenePath, true);
        Selection.activeGameObject = player.gameObject;
        Debug.Log("Laboratorio creado en " + ScenePath);
    }

    // ---------- zonas ----------
    // Cada zona es una sala grande con una salida y varios sistemas que reaccionan a los cuerpos,
    // para que se pueda resolver de varias formas. Las salidas tienen otra ruta o son altas para que
    // un cuerpo mal puesto no las tape (y si pasa, con la Q lo recuperas).

    // Zona 1 - Pabellon de especimenes. Sales de tu corral; la salida esta en la plataforma de observacion
    // (5 de alto, el techo de la sala de control, que esta cerrada). El suelo de los corrales esta electrificado.
    // Para subir: el montacargas (cargar el contrapeso con replicas), una escalera de cuerpos congelados...
    static void Containment()
    {
        Carve(2, 6, 43, 16);           // el pabellon
        ElectricFloor(7, 14, 6);       // suelo de los corrales: 8 de ancho, no se salta
        Fill(33, 6, 43, 10);           // sala de control (hueca, paredes de 1); su techo es la plataforma de observacion
        Carve(34, 6, 42, 9);
        LockedDoor(33, 6);
        Lift(28, 31, 6, 5);            // cabina en x 28-30, contrapeso en x 31-32

        Prop("tanque", 3.5f, 6, false);
        Prop("escritorio", 18f, 6, false);
        Prop("ordenador", 20.5f, 6, false);
        Prop("consola", 35.5f, 6, false);
        Prop("escritorio", 38f, 6, false);
        Prop("ordenador", 40.5f, 6, false);
        Terminal(36.5f, 11);
        Prop("escritorio", 39.5f, 11, false);
        // los controles, pegados en la pared del corral donde empiezas
        AddSprite(NewObject(decor, "Cartel de controles", new Vector3(5.5f, 9.5f), 0), art.controlsSign, -9);

        Lamp(5f, 17, false);
        Lamp(11f, 17, true);
        Lamp(18f, 17, false);
        Lamp(25f, 17, false);
        Lamp(38f, 17, false);
        Lamp(38f, 10, true);
        Alarm(10.5f, 17);

        HorizontalPipe(2, 26, 15);
        VerticalPipe(16, 6, 14);
        VerticalPipe(42, 11, 16);
    }

    // Zona 2 - Control de seguridad. Arriba, la pasarela de observacion con laseres y un tramo de cristal;
    // abajo, el archivo y la sala de vigilancia. La salida esta dentro de la garita, detras de una puerta
    // automatica con una torreta. Formas: tapar laseres con cuerpos, romper el cristal y saltarte uno,
    // replica de señuelo para la torreta, un cuerpo que deja la puerta abierta, saltar por encima de la torreta...
    static void Security()
    {
        ZoneDoor(44, 11, new Vector2(46.5f, 11.5f));
        Carve(45, 11, 72, 15);         // pasarela de observacion (suelo a 11)
        Carve(45, 4, 72, 9);           // archivo, debajo de la pasarela
        Carve(73, 4, 92, 15);          // sala de vigilancia
        Glass(58, 64, 10, 1);          // cristal: aguanta 1 (tu solo); con algo mas se rompe
        Fill(83, 7, 92, 7);            // techo de la garita
        Fill(83, 8, 83, 10);           // caja de la puerta: la hoja se esconde ahi al abrirse

        Laser(52, 11, 16, 0f, 0f);
        Laser(68, 11, 16, 1.6f, 1.1f); // intermitente: se puede cruzar a tiempo
        AutoDoor(83, 4);               // puerta de la garita (3 de alto)
        Turret(87, 4);

        Prop("cajas", 47.5f, 4, true);
        Prop("barriles", 49.5f, 4, false);
        Prop("escritorio", 54f, 4, false);
        Prop("consola", 67f, 4, false);
        Prop("cajas", 70.5f, 4, true);
        Prop("ordenador", 76f, 4, false);
        Terminal(80.5f, 4);

        Lamp(48f, 16, false);
        Lamp(56f, 16, true);
        Lamp(63f, 16, false);
        Lamp(52f, 10, false);
        Lamp(68f, 10, true);
        Lamp(77f, 16, false);
        Lamp(88.5f, 7, false);
        Alarm(85f, 16);

        VerticalPipe(74, 4, 15);
        HorizontalPipe(75, 92, 13);
    }

    // Zona 3 - Planta de residuos. Una cinta lleva la basura a la compactadora y al lado esta el tanque
    // de residuos. La salida (el muelle de carga) esta arriba a la derecha. Formas: montarse en la prensa
    // cuando sube, atascarla con un cuerpo congelado, cruzar el tanque sobre cuerpos flotando,
    // tapar el vapor con un cuerpo, un puente de cuerpos congelados en la pasarela rota...
    static void WasteZone()
    {
        ZoneDoor(93, 4, new Vector2(94.5f, 4.5f));
        Carve(94, 4, 140, 15);         // nave de residuos (suelo a 4, techo a 16)
        Conveyor(97, 108, 3, 2.5f);    // cinta hacia la prensa
        Press(109, 4, 4f, 16);         // compactadora de 3 de ancho: arriba queda a ras de la pasarela
        Fill(112, 8, 120, 8);          // pasarela (suelo a 9)
        Fill(127, 8, 133, 8);          // la pasarela esta rota encima del tanque: hueco de 6 (x 121-126)
        Tank(115, 127, 0, 3);          // tanque hundido de residuos
        Fill(134, 10, 140, 10);        // muelle de carga (suelo a 11), con el almacen debajo

        Steam(101.5f, 16f, false, 12f, 1f, 2f, 0f);    // vapor del techo sobre la cinta
        Steam(105.5f, 16f, false, 12f, 1f, 2f, 1.5f);
        Steam(117.5f, 9f, true, 3f, 1.2f, 1.8f, 0.6f); // tuberia rota en la pasarela
        FinalDoor(141, 11);

        Prop("barril_toxico", 112.5f, 4, false);
        Prop("barriles", 130.5f, 4, false);
        Prop("cajas", 136.5f, 11, true);
        Prop("caja", 138.5f, 11, false);
        Prop("cajas", 135.5f, 4, false);
        Prop("cajas", 137f, 4, false);
        Prop("barril_toxico", 139f, 4, false);
        Lamp(137f, 10, false);

        Lamp(96f, 16, false);
        Lamp(104f, 16, true);
        Lamp(114f, 16, false);
        Lamp(124f, 16, true);
        Lamp(137f, 16, false);
        Alarm(107f, 16);

        HorizontalPipe(94, 140, 14);
        VerticalPipe(132, 4, 13);
        VerticalPipe(129, 4, 13);
    }

    // ---------- mapa ----------

    static void Fill(int x0, int y0, int x1, int y1) => SetSolid(x0, y0, x1, y1, true);
    static void Carve(int x0, int y0, int x1, int y1) => SetSolid(x0, y0, x1, y1, false);

    static void SetSolid(int x0, int y0, int x1, int y1, bool value)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                solid[x, y] = value;
    }

    static void HorizontalPipe(int x0, int x1, int y)
    {
        for (int x = x0; x <= x1; x++) pipes.Add((new Vector3Int(x, y, 0), art.tiles[57]));
    }

    static void VerticalPipe(int x, int y0, int y1)
    {
        for (int y = y0; y <= y1; y++) pipes.Add((new Vector3Int(x, y, 0), art.tiles[66]));
    }

    static void PaintTiles()
    {
        var grid = new GameObject("Mapa").AddComponent<Grid>();
        Tilemap background = NewTilemap(grid, "Fondo", -20);
        Tilemap terrain = NewTilemap(grid, "Terreno", 0);
        terrain.gameObject.layer = ground;

        var rock = new List<Vector3Int>();
        var room = new List<Vector3Int>();
        for (int x = -Margin; x < Width + Margin; x++)
            for (int y = -Margin; y < Height + Margin; y++)
            {
                bool inside = x >= 0 && y >= 0 && x < Width && y < Height;
                (inside && !solid[x, y] ? room : rock).Add(new Vector3Int(x, y, 0));
            }
        terrain.SetTiles(rock.ToArray(), Enumerable.Repeat<TileBase>(art.terrain, rock.Count).ToArray());
        background.SetTiles(room.ToArray(), Enumerable.Repeat<TileBase>(art.background, room.Count).ToArray());
        foreach (var (cell, tile) in pipes) background.SetTile(cell, tile);

        // un solo contorno para todo el terreno: sin bordes fantasma entre tiles
        terrain.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        terrain.gameObject.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
        terrain.gameObject.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;

        // las paredes cortan la luz de las lamparas
        var shadows = terrain.gameObject.AddComponent<ShadowCaster2D>();
        shadows.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow;
    }

    static Tilemap NewTilemap(Grid grid, string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(grid.transform, false);
        var tilemap = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>().sortingOrder = order;
        return tilemap;
    }

    // ---------- puertas ----------

    static void ZoneDoor(int x, int y, Vector2 spawn)
    {
        Carve(x, y, x, y + 1);
        var go = NewObject(traps, "Puerta de zona", new Vector3(x + 0.5f, y), noRaycast);
        Sprite open = art.doorTurningOff[art.doorTurningOff.Length - 1];
        AddSprite(go, open, 1);
        var loop = go.AddComponent<SpriteLoop>();
        SetSprites(loop, "frames", new[] { open });

        // el trigger esta en la casilla de despues: la puerta se cierra cuando ya has pasado
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1f, 2f);
        trigger.offset = new Vector2(1.5f, 1f);

        var blocker = NewObject(go.transform, "Bloqueo", go.transform.position, ground).AddComponent<BoxCollider2D>();
        blocker.size = new Vector2(1f, 2f);
        blocker.offset = new Vector2(0f, 1f);

        Transform spawnPoint = NewObject(go.transform, "Reaparicion", spawn, 0).transform;
        Light2D doorLight = AddLight(go.transform, "Luz", new Vector3(x + 0.5f, y + 1.8f), OpenDoorColor, 0.9f, 3f);

        var door = go.AddComponent<ZoneDoor>();
        SetRef(door, "blocker", blocker);
        SetRef(door, "spawnPoint", spawnPoint);
        SetRef(door, "barrier", loop);
        SetRef(door, "doorLight", doorLight);
        SetSprites(door, "closingFrames", Enumerable.Reverse(art.doorTurningOff).ToArray());
        SetSprites(door, "closedFrames", art.doorClosed);
    }

    static void FinalDoor(int x, int y)
    {
        Carve(x, y, x, y + 1);
        var go = NewObject(traps, "Puerta final", new Vector3(x + 0.5f, y), noRaycast);
        AddSprite(go, art.doorTurningOff[art.doorTurningOff.Length - 1], 1);
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(0.6f, 2f);
        trigger.offset = new Vector2(0f, 1f);
        go.AddComponent<FinalDoor>();

        // luz de fuera: la salida tiene que verse desde abajo del conducto
        Light2D exit = AddLight(go.transform, "Luz de fuera", new Vector3(x + 0.5f, y + 1.5f), ExitColor, 1.6f, 7f);
        exit.volumetricEnabled = true;
        exit.volumeIntensity = 0.12f;
    }

    // puerta de seguridad cerrada (solo decorado, la pared sigue ahi): explica por que no se puede pasar
    static void LockedDoor(int x, int y)
    {
        var go = NewObject(decor, "Puerta bloqueada", new Vector3(x + 0.5f, y), 0);
        AddSprite(go, art.autoDoor, 1);
        AddLight(go.transform, "Piloto", new Vector3(x + 0.5f, y + 2.6f), AlarmColor, 0.7f, 2f);
    }

    // ---------- trampas ----------

    // laser de suelo a techo: emisor abajo, receptor arriba y el rayo en medio
    static void Laser(int x, int floorY, int ceilingY, float onTime, float offTime)
    {
        var go = NewObject(traps, "Laser", new Vector3(x + 0.5f, floorY + 0.1f), 0);
        var emitter = NewObject(go.transform, "Emisor", new Vector3(x + 0.5f, floorY), 0);
        AddSprite(emitter, art.laserBase[0], 3);
        Animate(emitter, art.laserBase, 12f);

        var receiver = NewObject(go.transform, "Receptor", new Vector3(x + 0.5f, ceilingY), 0);
        AddSprite(receiver, art.laserTop[0], 3);
        Animate(receiver, art.laserTop, 12f);

        var beam = NewObject(go.transform, "Rayo", go.transform.position, 0);
        SpriteRenderer beamSr = AddSprite(beam, art.laserBeam[0], 5);
        beamSr.sharedMaterial = unlit;
        beamSr.drawMode = SpriteDrawMode.Tiled;
        beamSr.size = new Vector2(6f / 32f, ceilingY - floorY);
        Animate(beam, art.laserBeam, 16f);

        Transform impact = AddLight(go.transform, "Impacto", go.transform.position, AlarmColor, 1.4f, 1.2f).transform;
        Light2D glow = AddLight(go.transform, "Luz del rayo", go.transform.position, AlarmColor, 0.6f, 3f);

        var laser = go.AddComponent<SecurityLaser>();
        SetRef(laser, "beam", beamSr);
        SetRef(laser, "impact", impact);
        SetRef(laser, "beamLight", glow);
        var so = new SerializedObject(laser);
        so.FindProperty("blockers").intValue = (1 << ground) | 1;
        so.FindProperty("onTime").floatValue = onTime;
        so.FindProperty("offTime").floatValue = offTime;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Turret(int x, int floorY)
    {
        var go = NewObject(traps, "Torreta", new Vector3(x + 0.5f, floorY), ground);
        SpriteRenderer body = AddSprite(go, art.turret[0], 2);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.95f, 0.75f);
        col.offset = new Vector2(0f, 0.375f);

        Transform muzzle = NewObject(go.transform, "Canon", new Vector3(x - 0.1f, floorY + 0.5f), 0).transform;
        LineRenderer sight = Line(go.transform, "Mira", 1f / 32f);
        LineRenderer bolt = Line(go.transform, "Rayo", 2f / 32f);
        bolt.startColor = bolt.endColor = new Color(0.85f, 0.95f, 1f);
        Light2D flash = AddLight(muzzle, "Fogonazo", muzzle.position, new Color(0.7f, 0.85f, 1f), 2.2f, 4f);
        AddLight(go.transform, "Piloto", new Vector3(x + 0.5f, floorY + 0.6f), AlarmColor, 0.5f, 1.5f);

        var turret = go.AddComponent<Turret>();
        SetRef(turret, "muzzle", muzzle);
        SetRef(turret, "body", body);
        SetRef(turret, "idleSprite", art.turret[0]);
        SetSprites(turret, "chargeFrames", art.turret.Skip(12).Take(6).ToArray());
        SetRef(turret, "sight", sight);
        SetRef(turret, "bolt", bolt);
        SetRef(turret, "flash", flash);
        var so = new SerializedObject(turret);
        so.FindProperty("sightMask").intValue = (1 << ground) | 1;
        so.FindProperty("targetMask").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // suelo electrificado de x0 a x1 encima del suelo que esta a la altura floorY
    static void ElectricFloor(int x0, int x1, int floorY)
    {
        float width = x1 - x0 + 1;
        var go = NewObject(traps, "Suelo electrificado", new Vector3(x0 + width / 2f, floorY), noRaycast);
        SpriteRenderer plate = AddSprite(go, art.electricPlate, 1);
        plate.drawMode = SpriteDrawMode.Tiled;
        plate.size = new Vector2(width, 6f / 32f);

        var sparks = NewObject(go.transform, "Chispas", go.transform.position, 0);
        SpriteRenderer arcs = AddSprite(sparks, art.electricArcs[0], 5);
        arcs.drawMode = SpriteDrawMode.Tiled;
        arcs.size = new Vector2(width, 0.5f);
        arcs.sharedMaterial = unlit;
        Animate(sparks, art.electricArcs, 14f);

        // franja fina pegada al suelo: subido a un cuerpo ya no la tocas
        var zone = go.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.size = new Vector2(width - 0.1f, 0.3f);
        zone.offset = new Vector2(0f, 0.15f);
        go.AddComponent<Hazard>();

        Light2D glow = AddLight(go.transform, "Brillo", go.transform.position + Vector3.up * 0.4f, new Color(0.45f, 0.8f, 1f), 0.9f, width / 2f + 1.5f);
        var floor = go.AddComponent<ElectricFloor>();
        SetRef(floor, "arcs", arcs);
        SetRef(floor, "glow", glow);
    }

    // montacargas de contrapeso: cabina de 3 en carX y contrapeso de 2 en weightX, los dos a ras del suelo.
    // La cabina sube rise casillas y el contrapeso baja 2
    static void Lift(int carX, int weightX, int floorY, int rise)
    {
        Carve(carX, floorY - 1, carX + 2, floorY - 1);
        Carve(weightX, floorY - 3, weightX + 1, floorY - 1);

        var go = NewObject(traps, "Montacargas", new Vector3(weightX, floorY), 0);
        Rigidbody2D car = Platform(go.transform, "Cabina", new Vector2(carX + 1.5f, floorY - 0.25f), 3f);
        Rigidbody2D weight = Platform(go.transform, "Contrapeso", new Vector2(weightX + 1f, floorY - 0.25f), 2f);
        var pulley = NewObject(go.transform, "Polea", new Vector3(weightX, FirstSolidAbove(weightX, floorY) - 0.5f), 0);
        AddSprite(pulley, art.pulley, 1);

        var lift = go.AddComponent<CounterweightLift>();
        SetRef(lift, "car", car);
        SetRef(lift, "counterweight", weight);
        SetRef(lift, "pulley", pulley.transform);
        SetRef(lift, "carCable", Cable(go.transform, "Cable cabina"));
        SetRef(lift, "counterweightCable", Cable(go.transform, "Cable contrapeso"));
        var so = new SerializedObject(lift);
        so.FindProperty("carTravel").floatValue = rise;
        so.FindProperty("counterweightTravel").floatValue = 2f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Rigidbody2D Platform(Transform parent, string name, Vector2 position, float width)
    {
        var go = NewObject(parent, name, position, ground);
        SpriteRenderer sr = AddSprite(go, art.platform, 1);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(width, 0.5f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(width, 0.5f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        return rb;
    }

    static LineRenderer Cable(Transform parent, string name)
    {
        LineRenderer line = Line(parent, name, 1f / 32f);
        line.startColor = line.endColor = new Color(0.1f, 0.11f, 0.14f);
        line.sortingOrder = 0;
        line.enabled = true;
        return line;
    }

    // puerta automatica de 3 de alto en la casilla x: la hoja sube y se mete en el techo
    static void AutoDoor(int x, int y)
    {
        var go = NewObject(traps, "Puerta automatica", new Vector3(x + 0.5f, y), 0);
        var panel = NewObject(go.transform, "Hoja", go.transform.position, 0);
        AddSprite(panel, art.autoDoor, -1);   // detras del techo: al abrirse queda tapada
        var blocker = NewObject(go.transform, "Bloqueo", go.transform.position, ground).AddComponent<BoxCollider2D>();
        blocker.size = new Vector2(0.6f, 3f);
        blocker.offset = new Vector2(0f, 1.5f);

        var door = go.AddComponent<AutoDoor>();
        SetRef(door, "panel", panel.transform);
        SetRef(door, "blocker", blocker);
        AddLight(go.transform, "Piloto", new Vector3(x + 0.5f, y + 2.6f), OpenDoorColor, 0.6f, 2f);
    }

    // suelo de cristal de x0 a x1 en la fila y (queda a ras del suelo de esa fila)
    static void Glass(int x0, int x1, int y, int maxLoad)
    {
        Carve(x0, y, x1, y);
        float width = x1 - x0 + 1;
        var go = NewObject(traps, "Suelo de cristal", new Vector3(x0 + width / 2f, y + 1f), ground);
        SpriteRenderer sr = AddSprite(go, art.glass, 2);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(width, 0.25f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, 0.25f);
        col.offset = new Vector2(0f, -0.125f);

        var glass = go.AddComponent<FragileGlass>();
        SetRef(glass, "glass", sr);
        SetRef(glass, "cracked", art.glassCracked);
        SetRef(glass, "shards", Shards(go.transform, width));
        var so = new SerializedObject(glass);
        so.FindProperty("maxLoad").intValue = maxLoad;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static ParticleSystem Shards(Transform parent, float width)
    {
        var ps = NewObject(parent, "Esquirlas", parent.position, 0).AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(2f / 32f, 4f / 32f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.97f, 1f), new Color(0.5f, 0.8f, 1f));
        main.gravityModifier = 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(width * 8)) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width, 0.1f, 0f);
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = lines;
        renderer.sortingOrder = 6;
        return ps;
    }

    // cinta transportadora en la fila y (por arriba queda a ras del suelo)
    static void Conveyor(int x0, int x1, int y, float speed)
    {
        Carve(x0, y, x1, y);
        float width = x1 - x0 + 1;
        var go = NewObject(traps, "Cinta transportadora", new Vector3(x0 + width / 2f, y + 0.5f), ground);
        SpriteRenderer sr = AddSprite(go, art.belt[0], 1);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(width, 1f);
        // los listones avanzan 2 px por fotograma: 16 fotogramas por segundo = 1 unidad por segundo
        Animate(go, speed > 0f ? art.belt : Enumerable.Reverse(art.belt).ToArray(), 16f * Mathf.Abs(speed));
        go.AddComponent<BoxCollider2D>().size = new Vector2(width, 1f);

        var conveyor = go.AddComponent<Conveyor>();
        var so = new SerializedObject(conveyor);
        so.FindProperty("speed").floatValue = speed;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // compactadora de 3 de ancho que empieza en la casilla x: baja travel casillas hasta el suelo floorY
    static void Press(int x, int floorY, float travel, int ceilingY)
    {
        var go = NewObject(traps, "Compactadora", new Vector3(x + 1.5f, floorY + travel), 0);
        var head = NewObject(go.transform, "Cabeza", new Vector3(x + 1.5f, floorY + travel + 0.5f), ground);
        AddSprite(head, art.pressHead, 2);
        head.AddComponent<BoxCollider2D>().size = new Vector2(3f, 1f);
        var rb = head.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        SpriteRenderer piston = AddSprite(NewObject(go.transform, "Piston", head.transform.position, 0), art.piston, 1);
        piston.drawMode = SpriteDrawMode.Tiled;
        piston.size = new Vector2(12f / 32f, 1f);

        var press = go.AddComponent<HydraulicPress>();
        SetRef(press, "head", rb);
        SetRef(press, "piston", piston);
        SetRef(press, "compactEffect", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Effects/Prefabs/Death.prefab"));
        var so = new SerializedObject(press);
        so.FindProperty("travel").floatValue = travel;
        so.FindProperty("ceilingY").floatValue = ceilingY;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // tuberia de vapor en (x, y): up = sale hacia arriba desde un suelo; si no, hacia abajo desde un techo
    static void Steam(float x, float y, bool up, float length, float onTime, float offTime, float delay)
    {
        // el chorro empieza un poco fuera de la boca, asi el rayo no choca con la pared donde esta clavada
        var go = NewObject(traps, "Vapor", new Vector3(x, y + (up ? 0.35f : -0.35f)), 0);
        go.transform.rotation = Quaternion.Euler(0f, 0f, up ? 0f : 180f);
        var nozzle = NewObject(go.transform, "Boca", new Vector3(x, y), 0);
        nozzle.transform.rotation = Quaternion.Euler(0f, 0f, up ? 180f : 0f);
        AddSprite(nozzle, art.nozzle, 3);

        var vent = go.AddComponent<SteamVent>();
        SetRef(vent, "steam", SteamParticles(go.transform));
        var so = new SerializedObject(vent);
        so.FindProperty("length").floatValue = length;
        so.FindProperty("onTime").floatValue = onTime;
        so.FindProperty("offTime").floatValue = offTime;
        so.FindProperty("startDelay").floatValue = delay;
        so.FindProperty("blockers").intValue = 1 << ground;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static ParticleSystem SteamParticles(Transform parent)
    {
        var ps = NewObject(parent, "Chorro", parent.position, 0).AddComponent<ParticleSystem>();
        ps.transform.rotation = parent.rotation;
        var main = ps.main;
        main.loop = true;
        main.startSpeed = 8f;
        main.startLifetime = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.startColor = new Color(0.92f, 0.95f, 0.97f, 0.8f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        var emission = ps.emission;
        emission.rateOverTime = 45f;
        emission.enabled = false;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 6f;
        shape.radius = 0.12f;
        shape.rotation = new Vector3(-90f, 0f, 0f);   // el cono sale por Z; asi sale por arriba (transform.up)
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 2.4f));
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;
        var sheet = ps.textureSheetAnimation;
        sheet.enabled = true;
        sheet.numTilesX = 4;
        sheet.numTilesY = 1;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
        sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 0.49f);
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Effects/Materials/FX_Puff.mat");
        renderer.sortingOrder = 5;
        return ps;
    }

    // tanque hundido de residuos de x0 a x1: pozo desde y0, desechos hasta surfaceY y paredes de chapa
    static void Tank(int x0, int x1, int y0, int surfaceY)
    {
        Carve(x0, y0, x1, surfaceY);
        WastePool(x0, y0, x1, surfaceY - 1);

        float width = x1 - x0 + 1, depth = surfaceY - y0 + 1;
        var go = NewObject(decor, "Tanque", new Vector3(x0 + width / 2f, y0 + depth / 2f), 0);
        SpriteRenderer back = AddSprite(go, art.tankBack, -15);
        back.drawMode = SpriteDrawMode.Tiled;
        back.size = new Vector2(width, depth);

        // las paredes tapan los bordes de piedra del pozo y sobresalen 1 por encima del suelo (bordes solidos)
        foreach (float wallX in new[] { x0 - 0.5f, x1 + 1.5f })
        {
            var wall = NewObject(go.transform, "Pared", new Vector3(wallX, y0 + depth / 2f), 0);
            SpriteRenderer sr = AddSprite(wall, art.tankWall, 1);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(1f, depth);

            var rim = NewObject(go.transform, "Borde", new Vector3(wallX, surfaceY + 1.5f), ground);
            AddSprite(rim, art.tankRim, 1);
            rim.AddComponent<BoxCollider2D>().size = Vector2.one;
        }
    }

    static float FirstSolidAbove(int x, int y)
    {
        while (y < Height && !solid[x, y]) y++;
        return y;
    }

    // altura del suelo que hay debajo de la casilla (x, y)
    static float FloorBelow(int x, int y)
    {
        while (y > 0 && !solid[x, y - 1]) y--;
        return y;
    }

    static void WastePool(int x0, int y0, int x1, int y1)
    {
        float width = x1 - x0 + 1, depth = y1 - y0 + 1;
        // el sprite sobresale un poco por arriba para que se vean las olas
        var go = NewObject(traps, "Desechos", new Vector3(x0 + width / 2f, y0 + (depth + 0.25f) / 2f), noRaycast);
        SpriteRenderer sr = AddSprite(go, PrototypeBuilder.Square, 4);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(width, depth + 0.25f);
        sr.sharedMaterial = waste;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width, depth);
        col.offset = new Vector2(0f, -0.125f);
        go.AddComponent<WastePool>();

        Light2D glow = AddLight(go.transform, "Brillo", new Vector3(x0 + width / 2f, y1 + 1.5f), WasteGlow, 0.9f, width / 2f + 3f);
        glow.falloffIntensity = 0.45f;
    }

    static Material CreateWasteMaterial()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Effects/Shaders/Waste.shadergraph");
        var material = AssetDatabase.LoadAssetAtPath<Material>(WasteMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, WasteMaterialPath);
        }
        material.shader = shader;
        material.SetColor("_ColorDeep", new Color(0.1f, 0.13f, 0.07f));
        material.SetColor("_ColorMid", new Color(0.3f, 0.42f, 0.12f));
        material.SetColor("_ColorSurface", new Color(0.6f, 0.78f, 0.22f));
        material.SetColor("_ColorFoam", new Color(0.86f, 0.95f, 0.58f));
        material.SetFloat("_WaveAmount", 2f);
        material.SetFloat("_WaveSpeed", 1.4f);
        material.SetFloat("_BubbleAmount", 0.45f);
        material.SetFloat("_Opacity", 0.96f);
        material.SetFloat("_PixelsPerUnit", 32f);
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------- decorado y luces ----------

    // los solidos (cajas, consolas...) se pueden pisar y tapan disparos; el resto es fondo
    static void Prop(string name, float x, float y, bool isSolid)
    {
        var go = NewObject(decor, name, new Vector3(x, y), isSolid ? ground : 0);
        AddSprite(go, art.props[name], isSolid ? 0 : -8);
        if (!isSolid) return;

        Vector2 size = (Vector2)PropSize[name] / 32f;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.offset = new Vector2(0f, size.y / 2f);
    }

    static void Terminal(float x, float y)
    {
        var go = NewObject(decor, "Terminal", new Vector3(x, y), 0);
        AddSprite(go, art.terminal[0], -8);
        Animate(go, art.terminal, 8f);
        AddLight(go.transform, "Pantalla", new Vector3(x + 0.2f, y + 0.6f), AlarmColor, 0.5f, 1.5f);
    }

    // fluorescente de techo: carcasa, tubo que brilla y un cono de luz hacia abajo
    static void Lamp(float x, float ceilingY, bool flicker)
    {
        var go = NewObject(lights, "Fluorescente", new Vector3(x, ceilingY), 0);
        AddSprite(go, art.lamp, -5);
        var tube = NewObject(go.transform, "Tubo", new Vector3(x, ceilingY - 3f / 32f), 0);
        SpriteRenderer tubeSr = AddSprite(tube, art.lampTube, -4);
        tubeSr.sharedMaterial = unlit;
        tubeSr.color = LampColor;

        // el cono llega hasta el suelo que tenga debajo
        float reach = ceilingY - FloorBelow(Mathf.FloorToInt(x), (int)ceilingY - 1);
        Light2D light = AddLight(go.transform, "Luz", new Vector3(x, ceilingY - 0.2f), LampColor, 1.1f, reach + 1.5f);
        light.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
        light.pointLightInnerAngle = 50f;
        light.pointLightOuterAngle = 120f;
        light.pointLightInnerRadius = 0.5f;
        light.falloffIntensity = 0.55f;
        light.shadowsEnabled = true;
        light.shadowIntensity = 0.85f;
        light.volumetricEnabled = true;
        light.volumeIntensity = 0.06f;

        if (!flicker) return;
        var flick = go.AddComponent<LightFlicker>();
        SetRef(flick, "lamp", light);
        SetRef(flick, "tube", tubeSr);
    }

    // luz roja de emergencia que respira
    static void Alarm(float x, float ceilingY)
    {
        var go = NewObject(lights, "Alarma", new Vector3(x, ceilingY), 0);
        AddSprite(go, art.alarm, -5);
        var bulb = NewObject(go.transform, "Bombilla", new Vector3(x, ceilingY - 2f / 32f), 0);
        SpriteRenderer bulbSr = AddSprite(bulb, art.alarmBulb, -4);
        bulbSr.sharedMaterial = unlit;
        bulbSr.color = AlarmColor;

        Light2D light = AddLight(go.transform, "Luz", new Vector3(x, ceilingY - 0.3f), AlarmColor, 1f, 4.5f);
        light.shadowsEnabled = true;
        light.shadowIntensity = 0.7f;

        var pulse = go.AddComponent<LightFlicker>();
        SetRef(pulse, "lamp", light);
        SetRef(pulse, "tube", bulbSr);
        var so = new SerializedObject(pulse);
        so.FindProperty("pulseSpeed").floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetupLighting(Player player)
    {
        // ambiente bajo y frio: las salas solo se ven bien donde hay lamparas
        foreach (Light2D light in Object.FindObjectsByType<Light2D>())
        {
            if (light.lightType != Light2D.LightType.Global) continue;
            light.intensity = 0.3f;
            light.color = AmbientColor;
        }

        // un poco de luz alrededor del jugador para que siempre se lea
        AddLight(player.transform, "Luz propia", player.transform.position, new Color(1f, 0.93f, 0.86f), 0.35f, 2.5f);
    }

    static void SetupPostProcessing()
    {
        Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        AssetDatabase.DeleteAsset(VolumePath);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, VolumePath);

        var bloom = AddOverride<Bloom>(profile);
        bloom.intensity.Override(0.7f);
        bloom.threshold.Override(0.85f);
        bloom.scatter.Override(0.55f);
        var vignette = AddOverride<Vignette>(profile);
        vignette.intensity.Override(0.3f);
        vignette.smoothness.Override(0.45f);
        AddOverride<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);
        var color = AddOverride<ColorAdjustments>(profile);
        color.saturation.Override(-12f);
        color.contrast.Override(12f);
        AssetDatabase.SaveAssets();

        var volume = new GameObject("Postproceso").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }

    static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T component = profile.Add<T>();
        component.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    // ---------- ayudas ----------

    static GameObject NewObject(Transform parent, string name, Vector3 position, int layer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.layer = layer;
        return go;
    }

    static SpriteRenderer AddSprite(GameObject go, Sprite sprite, int order)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    static void Animate(GameObject go, Sprite[] frames, float fps)
    {
        var loop = go.AddComponent<SpriteLoop>();
        SetSprites(loop, "frames", frames);
        var so = new SerializedObject(loop);
        so.FindProperty("fps").floatValue = fps;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static LineRenderer Line(Transform parent, string name, float width)
    {
        var line = NewObject(parent, name, parent.position, 0).AddComponent<LineRenderer>();
        line.sharedMaterial = lines;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.widthMultiplier = width;
        line.sortingOrder = 6;
        line.enabled = false;
        return line;
    }

    static Light2D AddLight(Transform parent, string name, Vector3 position, Color color, float intensity, float radius)
    {
        var light = NewObject(parent, name, position, 0).AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightInnerRadius = 0f;
        light.pointLightOuterRadius = radius;
        light.falloffIntensity = 0.6f;
        light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
        return light;
    }

    static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetSprites(Object target, string field, Sprite[] sprites)
    {
        var so = new SerializedObject(target);
        SerializedProperty list = so.FindProperty(field);
        list.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
