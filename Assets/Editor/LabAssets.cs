using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

// Prepara el tileset Sci-Fi Labs (Foozle) para Unity: corta las hojas de sprites,
// crea un Tile por cada tile, los Rule Tiles "Terreno" y "Fondo" y la Tile Palette "Laboratorio".
// Lo usa LabBuilder, pero tambien se puede lanzar solo desde Replica > Preparar tiles del laboratorio.
public static class LabAssets
{
    const string Source = "Assets/Sprites/Map/Foozle_2DT0001_Science_Fiction_Labs_Tileset";
    const string Generated = "Assets/Sprites/Map/Generado";
    // el personaje tal cual lo dibujaron (115 px por fotograma) y la version reducida que usa el juego
    const string CharacterSource = "Assets/Sprites/Personaje/Bicho_original.png";
    const string CharacterSheet = "Assets/Sprites/Personaje/Bicho.png";
    // los botones pixel de Kenney (tiles de 16 px): de ahi salen las teclas y los botones del mando de los carteles
    const string ButtonTiles = "Assets/Buttons/Tiles";
    const string ControlIconsPath = "Assets/Settings/Iconos de controles.asset";
    const string TilesFolder = "Assets/Tiles";
    const int PPU = 32;

    public class Set
    {
        public RuleTile terrain;
        public RuleTile background;
        public Tile[] tiles = new Tile[100];      // por numero del tileset: tiles[57] = tile057
        public Sprite[] doorClosed;               // barrera encendida, en bucle
        public Sprite[] doorTurningOff;           // barrera apagandose (al reves se enciende)
        public Sprite[] terminal;
        public Sprite[] turret;
        public Sprite[] saw;
        public Sprite[] blades;
        public Sprite[] laserBase;
        public Sprite[] laserBeam;
        public Sprite[] laserTop;
        public Dictionary<string, Sprite> props = new Dictionary<string, Sprite>();
        public Sprite lamp;
        public Sprite lampTube;
        public Sprite alarm;
        public Sprite alarmBulb;

        // maquinas dibujadas a mano con la paleta del tileset
        public Sprite platform;
        public Sprite pulley;
        public Sprite glass;
        public Sprite glassCracked;
        public Sprite[] belt;
        public Sprite pressHead;
        public Sprite piston;
        public Sprite nozzle;
        public Sprite electricPlate;
        public Sprite[] electricArcs;
        public Sprite autoDoor;
        public Sprite tankWall;
        public Sprite tankRim;
        public Sprite tankBack;

        public Sprite controlsSign;               // el cartel del laboratorio (imagen)
        public Sprite signPaper;                  // los carteles editables: papel (9-slice) y cinta
        public Sprite signTape;
        public ControlIcons controlIcons;         // iconos de teclas y del mando para los carteles
        public Sprite replicaSample;              // muestra de mutageno: una replica mas

        // elementos del nivel (prefabs)
        public Sprite elevatorCabin;              // ascensor por fuera: se estira por el medio (9-slice)
        public Sprite elevatorDoors;              // sus puertas, con las ventanitas
        public Sprite[] terminalOpen;             // el computador de seguridad en verde (abierto)
        public Sprite securityDoor;
        public Sprite securityPad;                // placa delante del computador de seguridad
        public Sprite securityFrame;              // marco blanco que distingue al computador
        public Sprite liftCage;                   // plataforma del montacargas, con barandas
        public Sprite liftWeight;                 // su contrapeso

        // cinematica del principio
        public Sprite specimenTank;
        public Sprite specimenLiquid;
        public Sprite[] specimenGlass;            // entero, rajado y roto
        public Sprite[][] scientists;             // cada cientifico: quieto y 4 pasos
        public Sprite[][] foregroundScientists;   // los que pasan por delante de la camara: quieto y 2 pasos
        public Sprite windowFrame;                // ventana de la sala de observacion: marco, pared y cristal
        public Sprite windowWall;
        public Sprite[] windowGlass;              // entero, rajado y roto
    }

    // colores sacados del tileset
    static readonly Color Dark = Hex("282C38"), Dark2 = Hex("333B51"), Mid2 = Hex("4C5C76"),
        Mid3 = Hex("60748A"), Light = Hex("76899D"), Light2 = Hex("89A7B6"),
        Yellow = Hex("BEA830"), Black = Hex("111111");

    [MenuItem("Replica/Preparar tiles del laboratorio")]
    public static Set Build()
    {
        CreateFolder(Generated);
        CreateFolder(TilesFolder);
        CreateFolder(TilesFolder + "/Laboratorio");

        var set = new Set();
        Dictionary<int, Sprite> tileSprites = BuildTileAtlas();
        foreach (var pair in tileSprites) set.tiles[pair.Key] = CreateTile(pair.Value, pair.Key);
        set.terrain = CreateTerrainTile(tileSprites);
        set.background = CreateBackgroundTile(tileSprites);
        CreatePalette(set);

        set.doorClosed = SliceStrip(Source + "/Barrier_Control_Panel/barrier_idle.png", 32, 64, new Vector2(0.5f, 0f));
        set.doorTurningOff = SliceStrip(Source + "/Barrier_Control_Panel/barrier_deactivate.png", 32, 64, new Vector2(0.5f, 0f));
        set.terminal = SliceStrip(Source + "/Barrier_Control_Panel/control_panel_idle.png", 32, 32, new Vector2(0.5f, 0f));
        // la torreta ocupa la mitad derecha de cada fotograma (a la izquierda va el rayo del dibujo original)
        set.turret = SliceStrip(Source + "/Traps/electric_turret.png", 64, 32, new Vector2(0.75f, 0f));
        // la sierra va pegada a la pared por su lado plano (el derecho)
        set.saw = SliceStrip(Source + "/Traps/saw_idle.png", 32, 32, new Vector2(1f, 0.5f));
        set.blades = SliceStrip(Source + "/Traps/wall_blades.png", 32, 32, new Vector2(0.5f, 0.5f));
        SliceLaser(set);
        SliceProps(set);
        CreateLampSprites(set);
        CreateMachineSprites(set);
        CreateSigns(set);
        CreateReplicaSample(set);
        CreateLevelElements(set);
        CreateIntroSprites(set);

        AssetDatabase.SaveAssets();
        return set;
    }

    // ---------- tiles ----------

    // junta los 100 PNG sueltos del tileset en una textura de 10x10 tiles: el tile N va en la columna N % 10, fila N / 10
    static Dictionary<int, Sprite> BuildTileAtlas()
    {
        string path = Generated + "/Laboratorio_Tiles.png";
        var atlas = new Texture2D(320, 320, TextureFormat.RGBA32, false);
        atlas.SetPixels32(new Color32[320 * 320]);
        var rects = new List<SpriteRect>();

        for (int i = 0; i < 100; i++)
        {
            var tile = new Texture2D(2, 2);
            tile.LoadImage(File.ReadAllBytes($"{Source}/Tileset/Individual_PNGs/Level_Tileset/tile{i:D3}.png"));
            Color[] pixels = tile.GetPixels();
            Object.DestroyImmediate(tile);
            if (pixels.All(p => p.a == 0f)) continue;

            var rect = new Rect(i % 10 * 32, (9 - i / 10) * 32, 32, 32);
            atlas.SetPixels((int)rect.x, (int)rect.y, 32, 32, pixels);
            rects.Add(NewRect($"lab_{i:D3}", rect, new Vector2(0.5f, 0.5f)));
        }

        // el tileset no trae rincones (donde una pared se junta con el suelo o el techo):
        // se hacen con la franja del suelo/techo sin su borde, asi sigue por debajo de la pared sin cortarse.
        // Van en los huecos vacios 8, 9, 18 y 19 del atlas
        AddCorner(atlas, rects, 1, 8, true);
        AddCorner(atlas, rects, 2, 9, true);
        AddCorner(atlas, rects, 31, 18, false);
        AddCorner(atlas, rects, 32, 19, false);

        File.WriteAllBytes(path, atlas.EncodeToPNG());
        Object.DestroyImmediate(atlas);
        return Slice(path, rects).ToDictionary(s => int.Parse(s.name.Substring(4)), s => s);
    }

    // copia el tile source en target tapando las 7 filas del borde (arriba en el suelo, abajo en el techo) con las 7 de al lado
    static void AddCorner(Texture2D atlas, List<SpriteRect> rects, int source, int target, bool borderOnTop)
    {
        Color[] pixels = atlas.GetPixels(source % 10 * 32, (9 - source / 10) * 32, 32, 32);
        for (int i = 0; i < 7; i++)
        {
            int row = borderOnTop ? 31 - i : i;
            int inner = borderOnTop ? row - 7 : row + 7;
            for (int x = 0; x < 32; x++) pixels[row * 32 + x] = pixels[inner * 32 + x];
        }

        var rect = new Rect(target % 10 * 32, (9 - target / 10) * 32, 32, 32);
        atlas.SetPixels((int)rect.x, (int)rect.y, 32, 32, pixels);
        rects.Add(NewRect($"lab_{target:D3}", rect, new Vector2(0.5f, 0.5f)));
    }

    static Tile CreateTile(Sprite sprite, int number)
    {
        var tile = LoadOrCreate<Tile>($"{TilesFolder}/Laboratorio/{sprite.name}.asset");
        tile.sprite = sprite;
        tile.colliderType = TileCollider(number);
        EditorUtility.SetDirty(tile);
        return tile;
    }

    // piedra solida, rampas con su forma, y paneles de fondo y maquinas sin colision
    static Tile.ColliderType TileCollider(int number)
    {
        int column = number % 10, row = number / 10;
        if (number == 8 || number == 9 || number == 18 || number == 19) return Tile.ColliderType.Grid;
        if ((row <= 2 && column <= 7) || (row == 3 && column <= 5) || number == 44 || number == 45) return Tile.ColliderType.Grid;
        if (row >= 4 && row <= 7 && column <= 3) return Tile.ColliderType.Sprite;
        return Tile.ColliderType.None;
    }

    // Rule Tile del terreno: elige el borde de piedra segun los vecinos.
    // Patron de 3x3 (fila de arriba primero): '#' = mas terreno, '.' = vacio, '-' = da igual
    static RuleTile CreateTerrainTile(Dictionary<int, Sprite> s)
    {
        var rule = LoadOrCreate<RuleTile>(TilesFolder + "/Terreno.asset");
        rule.m_DefaultSprite = s[11];
        rule.m_DefaultColliderType = Tile.ColliderType.Grid;
        rule.m_TilingRules = new List<RuleTile.TilingRule>
        {
            Rule("-.-|.?.|-.-", s[15]),                     // bloque suelto
            Rule("-.-|.?#|-.-", s[5]),                      // plataforma de una fila
            Rule("-.-|#?#|-.-", s[6]),
            Rule("-.-|#?.|-.-", s[7]),
            Rule("-.-|.?.|-#-", s[4]),                      // columna de un tile de ancho
            Rule("-#-|.?.|-#-", s[14]),
            Rule("-#-|.?.|-.-", s[24]),
            Rule("-.-|.?#|-#-", s[0]),                      // esquinas y bordes
            Rule("-.-|#?.|-#-", s[3]),
            Rule("-.-|#?#|-#-", s[1], s[2]),
            Rule("-#-|.?#|-.-", s[30]),
            Rule("-#-|#?.|-.-", s[33]),
            Rule("-#-|#?#|-.-", s[31], s[32]),
            Rule("-#-|.?#|-#-", s[10], s[20]),
            Rule("-#-|#?.|-#-", s[13], s[23]),
            Rule("-#.|#?#|-#-", s[8], s[9]),                // rincon con el suelo: la franja del suelo sigue bajo la pared
            Rule(".#-|#?#|-#-", s[8], s[9]),
            Rule("-#-|#?#|-#.", s[18], s[19]),              // rincon con el techo
            Rule("-#-|#?#|.#-", s[18], s[19]),
            Rule("---|-?-|---", s[11], s[11], s[11], s[12], s[21], s[22], s[22], s[16], s[26], s[27]),  // roca de dentro
        };
        EditorUtility.SetDirty(rule);
        return rule;
    }

    // paneles de pared mezclados al azar (siempre igual en la misma casilla)
    static RuleTile CreateBackgroundTile(Dictionary<int, Sprite> s)
    {
        var rule = LoadOrCreate<RuleTile>(TilesFolder + "/Fondo.asset");
        rule.m_DefaultSprite = s[38];
        rule.m_DefaultColliderType = Tile.ColliderType.None;
        RuleTile.TilingRule panels = Rule("---|-?-|---", s[38], s[38], s[38], s[38], s[38], s[67], s[67], s[37], s[46]);
        panels.m_ColliderType = Tile.ColliderType.None;
        rule.m_TilingRules = new List<RuleTile.TilingRule> { panels };
        EditorUtility.SetDirty(rule);
        return rule;
    }

    static RuleTile.TilingRule Rule(string pattern, params Sprite[] sprites)
    {
        var rule = new RuleTile.TilingRule
        {
            m_Sprites = sprites,
            m_Output = sprites.Length > 1 ? RuleTile.TilingRuleOutput.OutputSprite.Random : RuleTile.TilingRuleOutput.OutputSprite.Single,
            m_ColliderType = Tile.ColliderType.Grid,
            // con la escala alta del ruido las variantes al azar no salen en grupos
            m_PerlinScale = 0.9f,
            m_Neighbors = new List<int>(),
            m_NeighborPositions = new List<Vector3Int>(),
        };

        string cells = pattern.Replace("|", "");
        for (int i = 0; i < 9; i++)
        {
            if (cells[i] != '#' && cells[i] != '.') continue;
            rule.m_NeighborPositions.Add(new Vector3Int(i % 3 - 1, 1 - i / 3, 0));
            rule.m_Neighbors.Add(cells[i] == '#' ? RuleTile.TilingRuleOutput.Neighbor.This : RuleTile.TilingRuleOutput.Neighbor.NotThis);
        }
        return rule;
    }

    // Tile Palette con los dos Rule Tiles arriba a la izquierda y el tileset entero colocado como en la hoja original
    static void CreatePalette(Set set)
    {
        string path = TilesFolder + "/Laboratorio.prefab";
        AssetDatabase.DeleteAsset(path);
        GridPaletteUtility.CreateNewPalette(TilesFolder, "Laboratorio", GridLayout.CellLayout.Rectangle,
            GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ);

        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        Tilemap tilemap = contents.GetComponentInChildren<Tilemap>();
        tilemap.SetTile(new Vector3Int(-2, 0, 0), set.terrain);
        tilemap.SetTile(new Vector3Int(-2, -1, 0), set.background);
        for (int i = 0; i < 100; i++)
            if (set.tiles[i] != null) tilemap.SetTile(new Vector3Int(i % 10, -(i / 10), 0), set.tiles[i]);

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    // ---------- sprites animados y decorado ----------

    static Sprite[] SliceStrip(string path, int width, int height, Vector2 pivot)
    {
        Vector2Int size = TextureSize(path);
        string name = Path.GetFileNameWithoutExtension(path);
        var rects = new List<SpriteRect>();
        for (int i = 0; i < size.x / width; i++)
            rects.Add(NewRect($"{name}_{i:D2}", new Rect(i * width, 0, width, height), pivot));
        return Slice(path, rects).OrderBy(s => s.name).ToArray();
    }

    // cada fotograma del laser de 1 tile se corta en 3: emisor de abajo, tramo de rayo (se repite) y receptor de arriba
    static void SliceLaser(Set set)
    {
        string path = Source + "/Traps/laser_idle.png";
        var rects = new List<SpriteRect>();
        for (int i = 0; i < 8; i++)
        {
            rects.Add(NewRect($"laser_base_{i}", new Rect(i * 32 + 8, 0, 14, 9), new Vector2(0.5f, 0f)));
            rects.Add(NewRect($"laser_beam_{i}", new Rect(i * 32 + 12, 10, 6, 12), new Vector2(0.5f, 0.5f)));
            rects.Add(NewRect($"laser_top_{i}", new Rect(i * 32 + 8, 23, 14, 9), new Vector2(0.5f, 1f)));
        }
        Sprite[] sprites = Slice(path, rects);
        set.laserBase = sprites.Where(s => s.name.StartsWith("laser_base")).OrderBy(s => s.name).ToArray();
        set.laserBeam = sprites.Where(s => s.name.StartsWith("laser_beam")).OrderBy(s => s.name).ToArray();
        set.laserTop = sprites.Where(s => s.name.StartsWith("laser_top")).OrderBy(s => s.name).ToArray();
    }

    static void SliceProps(Set set)
    {
        string[,] names =
        {
            { "barril_toxico", "barriles", "" },
            { "escritorio", "ordenador", "tanque" },
            { "caja", "cajas", "consola" },
        };
        var rects = new List<SpriteRect>();
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                if (names[row, column] != "")
                    rects.Add(NewRect(names[row, column], new Rect(column * 32, (2 - row) * 32, 32, 32), new Vector2(0.5f, 0f)));

        foreach (Sprite sprite in Slice(Source + "/Decor/full decor tiles.png", rects)) set.props[sprite.name] = sprite;
    }

    // fluorescente y luz de emergencia dibujados a mano: la carcasa recibe luz, el tubo brilla solo
    static void CreateLampSprites(Set set)
    {
        var dark = new Color(0.16f, 0.17f, 0.21f);
        var edge = new Color(0.33f, 0.36f, 0.42f);

        var lamp = Canvas(24, 4);
        Paint(lamp, 0, 1, 24, 3, dark);
        Paint(lamp, 1, 3, 22, 1, edge);
        Paint(lamp, 0, 0, 2, 1, dark);
        Paint(lamp, 22, 0, 2, 1, dark);
        set.lamp = SaveSprite(lamp, "Fluorescente", new Vector2(0.5f, 1f));

        var tube = Canvas(20, 2);
        Paint(tube, 0, 0, 20, 2, Color.white);
        set.lampTube = SaveSprite(tube, "Fluorescente_Tubo", new Vector2(0.5f, 1f));

        var alarm = Canvas(10, 4);
        Paint(alarm, 0, 2, 10, 2, dark);
        Paint(alarm, 1, 3, 8, 1, edge);
        set.alarm = SaveSprite(alarm, "Alarma", new Vector2(0.5f, 1f));

        var bulb = Canvas(6, 3);
        Paint(bulb, 0, 0, 6, 3, Color.white);
        Paint(bulb, 0, 0, 1, 1, Color.clear);
        Paint(bulb, 5, 0, 1, 1, Color.clear);
        set.alarmBulb = SaveSprite(bulb, "Alarma_Luz", new Vector2(0.5f, 1f));
    }

    static void CreateMachineSprites(Set set)
    {
        // plataforma de montacargas: borde claro, franja de peligro y chapa
        var platform = Canvas(32, 16);
        Paint(platform, 0, 15, 32, 1, Light2);
        Paint(platform, 0, 14, 32, 1, Light);
        Stripes(platform, 0, 11, 32, 3);
        Paint(platform, 0, 4, 32, 7, Mid3);
        Paint(platform, 0, 2, 32, 2, Mid2);
        Paint(platform, 0, 0, 32, 2, Dark);
        for (int x = 3; x < 32; x += 8) Paint(platform, x, 7, 2, 1, Light);
        set.platform = SaveSprite(platform, "Plataforma", new Vector2(0.5f, 0.5f));

        var pulley = Canvas(16, 16);
        Circle(pulley, 8f, 8f, 7.5f, Dark2);
        Circle(pulley, 8f, 8f, 5.5f, Mid3);
        Circle(pulley, 8f, 8f, 2f, Light2);
        set.pulley = SaveSprite(pulley, "Polea", new Vector2(0.5f, 0.5f));

        // cristal de observacion (8 px de grueso) y el mismo rajado
        var glassColor = new Color(0.62f, 0.85f, 1f, 0.35f);
        var glare = new Color(0.85f, 0.95f, 1f, 0.7f);
        var glass = Canvas(32, 8);
        Paint(glass, 0, 7, 32, 1, Light2);
        Paint(glass, 0, 1, 32, 6, glassColor);
        Paint(glass, 0, 0, 32, 1, Mid2);
        for (int x = 0; x < 32; x++)
            for (int y = 1; y < 7; y++)
                if ((x + y * 2) % 16 < 2) glass.SetPixel(x, y, glare);
        var cracked = Canvas(32, 8);
        cracked.SetPixels(glass.GetPixels());
        set.glass = SaveSprite(glass, "Cristal", new Vector2(0.5f, 1f));

        var crack = new Color(1f, 1f, 1f, 0.95f);
        int[] crackY = { 3, 4, 4, 5, 3, 2, 2, 3, 4, 6, 5, 4, 3, 3, 2, 1, 2, 3, 4, 4, 5, 6, 5, 4, 3, 2, 3, 4, 5, 4, 3, 3 };
        for (int x = 0; x < 32; x++) cracked.SetPixel(x, crackY[x], crack);
        cracked.SetPixel(9, 5, crack);
        cracked.SetPixel(21, 2, crack);
        set.glassCracked = SaveSprite(cracked, "Cristal_Rajado", new Vector2(0.5f, 1f));

        // cinta transportadora: 4 fotogramas con los listones avanzando
        var belt = Canvas(128, 32);
        for (int f = 0; f < 4; f++)
        {
            int ox = f * 32;
            Paint(belt, ox, 0, 32, 26, Mid2);
            Stripes(belt, ox, 0, 32, 3);
            Paint(belt, ox, 26, 32, 6, Dark);
            for (int x = 0; x < 32; x++)
                if ((x - f * 2 + 64) % 8 < 2) Paint(belt, ox + x, 27, 1, 4, Light);
            Paint(belt, ox, 31, 32, 1, Light2);
            Circle(belt, ox + 8f, 14f, 6f, Dark2);
            Circle(belt, ox + 24f, 14f, 6f, Dark2);
            Circle(belt, ox + 8f, 14f, 2f, Light);
            Circle(belt, ox + 24f, 14f, 2f, Light);
        }
        set.belt = SaveStrip(belt, "Cinta", 32, 32, new Vector2(0.5f, 0.5f));

        // cabeza de la prensa: bloque de 3 tiles con remaches y franja de peligro abajo
        var head = Canvas(96, 32);
        Paint(head, 0, 0, 96, 32, Dark);
        Paint(head, 1, 1, 94, 30, Mid3);
        Paint(head, 1, 28, 94, 2, Light);
        Paint(head, 1, 30, 94, 1, Light2);
        Stripes(head, 1, 1, 94, 6);
        for (int x = 6; x < 96; x += 14)
        {
            Paint(head, x, 22, 2, 2, Light2);
            Paint(head, x, 11, 2, 2, Light2);
        }
        set.pressHead = SaveSprite(head, "Prensa", new Vector2(0.5f, 0.5f));

        var piston = Canvas(12, 16);
        Paint(piston, 0, 0, 12, 16, Dark);
        Paint(piston, 1, 0, 10, 16, Light);
        Paint(piston, 3, 0, 3, 16, Light2);
        Paint(piston, 9, 0, 2, 16, Mid3);
        set.piston = SaveSprite(piston, "Piston", new Vector2(0.5f, 0.5f));

        // boca de la tuberia de vapor (mira hacia abajo)
        var nozzle = Canvas(16, 10);
        Paint(nozzle, 2, 0, 12, 10, Dark);
        Paint(nozzle, 3, 2, 10, 8, Mid3);
        Paint(nozzle, 4, 2, 2, 8, Light);
        Paint(nozzle, 1, 0, 14, 2, Mid2);
        Paint(nozzle, 5, 0, 6, 1, Black);
        set.nozzle = SaveSprite(nozzle, "Tuberia_Vapor", new Vector2(0.5f, 1f));

        // suelo electrificado: rejilla con luces y 4 fotogramas de chispazos
        var plate = Canvas(32, 6);
        Paint(plate, 0, 0, 32, 6, Dark);
        Paint(plate, 0, 5, 32, 1, Light);
        for (int x = 2; x < 32; x += 4) Paint(plate, x, 1, 2, 3, Dark2);
        for (int x = 4; x < 32; x += 8) plate.SetPixel(x, 2, new Color(0.45f, 0.9f, 1f));
        set.electricPlate = SaveSprite(plate, "Suelo_Electrico", new Vector2(0.5f, 0f));

        var arcs = Canvas(128, 16);
        var rng = new System.Random(3);
        for (int f = 0; f < 4; f++)
            for (int arc = 0; arc < 2; arc++)
            {
                int y = rng.Next(1, 8);
                for (int x = rng.Next(0, 6); x < 32; x++)
                {
                    y = Mathf.Clamp(y + rng.Next(-1, 2), 0, 12);
                    arcs.SetPixel(f * 32 + x, y, arc == 0 ? Color.white : new Color(0.55f, 0.9f, 1f));
                }
            }
        set.electricArcs = SaveStrip(arcs, "Chispas", 32, 16, new Vector2(0.5f, 0f));

        // hoja de la puerta automatica (3 tiles de alto) con ventanilla y franja abajo
        var door = Canvas(32, 96);
        Paint(door, 0, 0, 32, 96, Dark);
        Paint(door, 2, 0, 28, 96, Mid2);
        Paint(door, 15, 0, 2, 96, Dark2);
        Paint(door, 3, 0, 1, 96, Light);
        Stripes(door, 2, 0, 28, 6);
        Paint(door, 7, 58, 18, 22, Dark);
        Paint(door, 8, 59, 16, 20, new Color(0.25f, 0.35f, 0.45f));
        for (int i = 0; i < 6; i++) door.SetPixel(10 + i, 64 + i, new Color(0.6f, 0.75f, 0.85f));
        Paint(door, 2, 94, 28, 2, Light2);
        set.autoDoor = SaveSprite(door, "Puerta_Automatica", new Vector2(0.5f, 0f));

        // tanque de residuos: pared de chapa con remaches, borde de arriba y fondo oscuro
        var wall = Canvas(32, 32);
        Paint(wall, 0, 0, 32, 32, Mid3);
        Paint(wall, 0, 0, 2, 32, Dark);
        Paint(wall, 30, 0, 2, 32, Dark);
        Paint(wall, 3, 0, 1, 32, Light);
        Paint(wall, 0, 15, 32, 1, Mid2);
        for (int x = 7; x < 30; x += 9)
        {
            wall.SetPixel(x, 4, Light2);
            wall.SetPixel(x, 27, Light2);
        }
        set.tankWall = SaveSprite(wall, "Tanque_Pared", new Vector2(0.5f, 0.5f));

        var rim = Canvas(32, 32);
        Paint(rim, 0, 0, 32, 32, Mid3);
        Paint(rim, 0, 0, 2, 32, Dark);
        Paint(rim, 30, 0, 2, 32, Dark);
        Paint(rim, 0, 30, 32, 2, Light2);
        Paint(rim, 0, 28, 32, 2, Light);
        Stripes(rim, 2, 22, 28, 5);
        set.tankRim = SaveSprite(rim, "Tanque_Borde", new Vector2(0.5f, 0.5f));

        var back = Canvas(32, 32);
        Paint(back, 0, 0, 32, 32, Dark2);
        Paint(back, 0, 0, 1, 32, Dark);
        Paint(back, 0, 16, 32, 1, Dark);
        for (int x = 4; x < 32; x += 12) back.SetPixel(x, 8, Mid2);
        set.tankBack = SaveSprite(back, "Tanque_Fondo", new Vector2(0.5f, 0.5f));
    }

    // carteles de papel pegados con cinta en la pared: cabecera roja y una fila por control (las teclas dibujadas como teclas)
    static void CreateSigns(Set set)
    {
        set.controlsSign = PaperSign("Cartel_Controles", "CONTROLES", new[]
        {
            ("A D", "MOVERSE"),
            ("ESPACIO W", "SALTAR"),
            ("SHIFT", "LANZAR CLON"),
            ("CTRL", "CLON CONGELADO"),
            ("Q", "RECUPERAR CLON"),
            ("K", "REINICIAR"),
        });

        // los carteles editables (componente Sign): el papel se estira por el medio y la cinta va en las esquinas
        var paper = Canvas(8, 8);
        Paint(paper, 0, 0, 8, 8, Hex("A9A595"));
        Paint(paper, 1, 1, 6, 6, Hex("D6D2C4"));
        set.signPaper = SaveSprite(paper, "Cartel_Papel", new Vector2(0.5f, 0.5f), new Vector4(2f, 2f, 2f, 2f));
        var tape = Canvas(18, 18);
        Tape(tape, 9, 9, 1);
        set.signTape = SaveSprite(tape, "Cartel_Cinta", new Vector2(0.5f, 0.5f));
        set.controlIcons = CreateControlIcons();
    }

    // las teclas y los botones del mando de Xbox de los carteles, por nombre. Cada una son uno o varios tiles seguidos
    // (tile_0235.png...): se juntan en una hoja, Teclas.png, para que las anchas como el espacio sean un solo sprite
    static ControlIcons CreateControlIcons()
    {
        var keys = new List<(string name, int[] tiles)>
        {
            ("keyboard_space", new[] { 235, 236, 237 }),
            ("keyboard_shift", new[] { 255, 256 }),
            ("keyboard_ctrl", new[] { 221, 222 }),
            ("keyboard_alt", new[] { 187, 188 }),
            ("keyboard_tab", new[] { 189, 190 }),
            ("keyboard_escape", new[] { 17 }),
            ("keyboard_arrow_up", new[] { 166 }),
            ("keyboard_arrow_right", new[] { 167 }),
            ("keyboard_arrow_down", new[] { 168 }),
            ("keyboard_arrow_left", new[] { 169 }),
            ("xbox_a", new[] { 4 }),
            ("xbox_b", new[] { 5 }),
            ("xbox_x", new[] { 6 }),
            ("xbox_y", new[] { 7 }),
            ("xbox_lt", new[] { 551 }),
            ("xbox_rt", new[] { 552 }),
            ("xbox_lb", new[] { 553 }),
            ("xbox_rb", new[] { 554 }),
            ("xbox_view", new[] { 616 }),
            ("xbox_menu", new[] { 617 }),
            ("xbox_stick_l", new[] { 217 }),
            ("xbox_stick_r", new[] { 285 }),
        };
        // las letras, fila a fila del teclado
        foreach (var (letters, first) in new[] { ("qwertyuiop", 85), ("asdfghjkl", 120), ("zxcvbnm", 155) })
            for (int i = 0; i < letters.Length; i++) keys.Add(("keyboard_" + letters[i], new[] { first + i }));

        // los tiles sueltos tambien sin suavizado ni compresion, para que no se vean borrosos si se usan en otro sitio
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ButtonTiles }))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (importer.filterMode == FilterMode.Point && importer.spritePixelsPerUnit == PPU &&
                    importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
                importer.filterMode = FilterMode.Point;
                importer.spritePixelsPerUnit = PPU;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }

        // una tecla por fila, con 2 pixeles libres entre filas
        var sheet = Canvas(48, keys.Count * 18);
        var rects = new List<SpriteRect>();
        for (int k = 0; k < keys.Count; k++)
        {
            int[] tiles = keys[k].tiles;
            for (int t = 0; t < tiles.Length; t++)
            {
                var tile = new Texture2D(2, 2);
                tile.LoadImage(File.ReadAllBytes($"{ButtonTiles}/tile_{tiles[t]:D4}.png"));
                sheet.SetPixels32(t * 16, k * 18, 16, 16, tile.GetPixels32());
                Object.DestroyImmediate(tile);
            }
            rects.Add(NewRect(keys[k].name, new Rect(0, k * 18, tiles.Length * 16, 16), new Vector2(0.5f, 0.5f)));
        }
        string path = $"{Generated}/Teclas.png";
        File.WriteAllBytes(path, sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);

        var icons = LoadOrCreate<ControlIcons>(ControlIconsPath);
        icons.Set(Slice(path, rects));
        EditorUtility.SetDirty(icons);
        return icons;
    }

    static Sprite PaperSign(string name, string title, (string keys, string action)[] rows)
    {
        Color paper = Hex("D6D2C4");
        int keysWidth = rows.Max(r => r.keys.Length == 0 ? 0 : r.keys.Split(' ').Sum(k => PixelText.Width(k) + 6) - 2);
        int textX = keysWidth > 0 ? 12 + keysWidth : 6;
        int actionsWidth = rows.Max(r => PixelText.Width(r.action));

        // el papel; alrededor queda sitio para su sombra en la pared y para la cinta.
        // Medidas pares: asi el cartel cae justo en la rejilla de pixeles
        const int left = 4, bottom = 6;
        int width = Mathf.Max(textX + 6 + actionsWidth, PixelText.Width(title) + 16);
        width += width % 2;
        int height = 20 + rows.Length * 14;
        var sign = Canvas(width + 8, height + 10);
        Paint(sign, left + 2, bottom - 2, width, height, new Color(0f, 0f, 0f, 0.35f));
        Paint(sign, left, bottom, width, height, Hex("A9A595"));
        Paint(sign, left + 1, bottom + 1, width - 2, height - 2, paper);
        Paint(sign, left + 1, bottom + height - 14, width - 2, 13, Hex("9E2B30"));
        PixelText.Draw(sign, title, left + (width - PixelText.Width(title)) / 2, bottom + height - 11, paper);

        int y = bottom + height - 28;
        foreach (var (keys, action) in rows)
        {
            int x = left + 6;
            if (keys.Length > 0)
                foreach (string key in keys.Split(' ')) x = Keycap(sign, key, x, y) + 2;
            PixelText.Draw(sign, action, left + textX, y + 2, Dark);
            y -= 14;
        }

        Tape(sign, left + 3, bottom + height - 4, 1);
        Tape(sign, left + width - 4, bottom + height - 4, -1);
        return SaveSprite(sign, name, new Vector2(0.5f, 0.5f));
    }

    // muestra de mutageno: un vial con tapa y base de metal y el liquido verde del tanque, con un brillo y burbujas
    static void CreateReplicaSample(Set set)
    {
        var vial = Canvas(14, 22);
        Paint(vial, 2, 0, 10, 3, Dark);
        Paint(vial, 3, 1, 8, 1, Mid3);
        Paint(vial, 2, 3, 10, 15, new Color(0.75f, 0.95f, 1f, 0.8f));
        for (int y = 4; y < 17; y++)
            Paint(vial, 3, y, 8, 1, Color.Lerp(new Color(0.15f, 0.6f, 0.3f), new Color(0.4f, 1f, 0.6f), (y - 4) / 12f));
        Paint(vial, 4, 6, 1, 9, new Color(0.9f, 1f, 0.95f));
        vial.SetPixel(8, 9, new Color(0.85f, 1f, 0.9f));
        vial.SetPixel(7, 13, new Color(0.85f, 1f, 0.9f));
        Paint(vial, 1, 18, 12, 4, Dark);
        Paint(vial, 2, 19, 10, 2, Mid3);
        Paint(vial, 2, 20, 10, 1, Light2);
        set.replicaSample = SaveSprite(vial, "Muestra_Mutageno", new Vector2(0.5f, 0.5f));
    }

    // ---------- personaje ----------

    // animaciones del bicho, por el nombre de sus etiquetas en el proyecto de Pixelorama
    public class Character
    {
        public Sprite[] run;          // RUN 1-8
        public Sprite[] jump;         // JUMP 9-10: se agacha y sale
        public Sprite[] fall;         // JUMP 11-12: arriba encogido y cayendo
        public Sprite[] idle;         // IDLE 13-17
        public Sprite[] cube;         // CUBO 18-22: se encoge en un cubo (lo que queda al morir una replica)
        public Sprite[] frozenCube;   // lo mismo pero acaba en CUBO congelado (23): la copia de Ctrl
    }

    // Reduce los fotogramas de 115 px a 46 (0.4) promediando, para que el bicho vaya a los mismos 32 px por casilla
    // que el resto del juego y mida lo mismo que su collider. El pivote va en los pies (o en la base del cubo)
    public static Character CreateCharacter()
    {
        const int sourceSize = 115, size = 46, frames = 24, firstCube = 17;
        CreateFolder("Assets/Sprites/Personaje");
        var original = new Texture2D(2, 2);
        original.LoadImage(File.ReadAllBytes(CharacterSource));
        Texture2D sheet = Shrink(original, (float)size / sourceSize);
        Object.DestroyImmediate(original);
        File.WriteAllBytes(CharacterSheet, sheet.EncodeToPNG());

        float cubeBase = LowestRow(sheet, 21 * size, size);
        var rects = new List<SpriteRect>();
        for (int i = 0; i < frames; i++)
        {
            float feet = i < firstCube ? LowestRow(sheet, i * size, size) : cubeBase;
            rects.Add(NewRect($"Bicho_{i:D2}", new Rect(i * size, 0, size, size), new Vector2(0.5f, feet / size)));
        }
        Object.DestroyImmediate(sheet);
        Sprite[] all = Slice(CharacterSheet, rects).OrderBy(sprite => sprite.name).ToArray();

        return new Character
        {
            run = all[0..8],
            jump = all[8..10],
            fall = all[10..12],
            idle = all[12..17],
            cube = all[17..22],
            frozenCube = new[] { all[17], all[18], all[19], all[20], all[22] },
        };
    }

    // cada pixel nuevo es la media de los que tapa del original (los transparentes no oscurecen el borde)
    static Texture2D Shrink(Texture2D source, float scale)
    {
        int width = Mathf.RoundToInt(source.width * scale), height = Mathf.RoundToInt(source.height * scale);
        var result = Canvas(width, height);
        Color[] pixels = source.GetPixels();
        float step = 1f / scale;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float x0 = x * step, x1 = x0 + step, y0 = y * step, y1 = y0 + step;
                Vector4 sum = Vector4.zero;
                for (int sy = (int)y0; sy < Mathf.CeilToInt(y1); sy++)
                    for (int sx = (int)x0; sx < Mathf.CeilToInt(x1); sx++)
                    {
                        float cover = (Mathf.Min(sx + 1, x1) - Mathf.Max(sx, x0)) * (Mathf.Min(sy + 1, y1) - Mathf.Max(sy, y0));
                        Color c = pixels[sy * source.width + sx];
                        sum += new Vector4(c.r * c.a, c.g * c.a, c.b * c.a, c.a) * cover;
                    }
                // casi transparente o casi opaco se redondea: bordes limpios, pero los brillos verdes se quedan suaves
                float alpha = sum.w / (step * step);
                if (alpha < 0.15f) continue;
                if (alpha > 0.85f) alpha = 1f;
                result.SetPixel(x, y, new Color(sum.x / sum.w, sum.y / sum.w, sum.z / sum.w, alpha));
            }
        return result;
    }

    // primera fila (desde abajo) del fotograma con algo bien opaco: donde estan los pies
    static float LowestRow(Texture2D sheet, int left, int size)
    {
        for (int y = 0; y < size; y++)
            for (int x = left; x < left + size; x++)
                if (sheet.GetPixel(x, y).a >= 0.5f) return y;
        return 0f;
    }

    // ---------- elementos del nivel (prefabs) ----------

    static void CreateLevelElements(Set set)
    {
        // ascensor visto por fuera: una caja de chapa con marco, remaches y franja abajo. Los 10 px de cada borde
        // no se estiran (9-slice), asi vale para cualquier tamaño
        var cabin = Canvas(48, 48);
        Paint(cabin, 0, 0, 48, 48, Dark);
        Paint(cabin, 1, 1, 46, 46, Dark2);
        Paint(cabin, 4, 6, 40, 36, Mid2);
        Paint(cabin, 1, 44, 46, 3, Light);
        Paint(cabin, 1, 47, 46, 1, Light2);
        Paint(cabin, 1, 1, 46, 4, Dark);
        Stripes(cabin, 2, 1, 44, 3);
        foreach (int x in new[] { 3, 44 })
            for (int y = 9; y < 42; y += 8) cabin.SetPixel(x, y, Light2);
        set.elevatorCabin = SaveSprite(cabin, "Ascensor_Cabina", new Vector2(0.5f, 0.5f), new Vector4(10f, 10f, 10f, 10f));

        // las puertas correderas cerradas: dos hojas, la junta en medio, ventanitas con luz calida y el piloto encima
        var doors = Canvas(30, 46);
        Color warm = Hex("FFB45C");
        Paint(doors, 0, 0, 30, 40, Dark);
        Paint(doors, 2, 0, 12, 38, Mid3);
        Paint(doors, 16, 0, 12, 38, Mid3);
        Paint(doors, 2, 37, 26, 1, Light2);
        Paint(doors, 14, 0, 2, 38, Dark);
        Paint(doors, 5, 24, 6, 8, Dark);
        Paint(doors, 19, 24, 6, 8, Dark);
        Paint(doors, 6, 25, 4, 6, warm);
        Paint(doors, 20, 25, 4, 6, warm);
        Paint(doors, 11, 41, 8, 5, Dark);
        Paint(doors, 12, 42, 6, 3, warm);
        set.elevatorDoors = SaveSprite(doors, "Ascensor_Puertas", new Vector2(0.5f, 0f));

        // el computador de seguridad abierto: el mismo panel con lo rojo cambiado a verde
        var panel = new Texture2D(2, 2);
        panel.LoadImage(File.ReadAllBytes(Source + "/Barrier_Control_Panel/control_panel_idle.png"));
        var open = Canvas(panel.width, panel.height);
        for (int y = 0; y < panel.height; y++)
            for (int x = 0; x < panel.width; x++)
            {
                Color c = panel.GetPixel(x, y);
                if (c.r > c.g * 1.3f && c.r > c.b * 1.3f) c = new Color(c.g, c.r, c.b, c.a);
                open.SetPixel(x, y, c);
            }
        Object.DestroyImmediate(panel);
        set.terminalOpen = SaveStrip(open, "Computador_Abierto", 32, 32, new Vector2(0.5f, 0f));

        // puerta de seguridad (2 de alto): chapa gruesa con remaches, junta en medio y franja de peligro
        var door = Canvas(32, 64);
        Paint(door, 0, 0, 32, 64, Dark);
        Paint(door, 2, 0, 28, 64, Dark2);
        Paint(door, 4, 2, 24, 60, Mid2);
        Stripes(door, 4, 26, 24, 12);
        Paint(door, 15, 0, 2, 64, Dark);
        for (int y = 6; y < 64; y += 14)
        {
            door.SetPixel(6, y, Light2);
            door.SetPixel(25, y, Light2);
        }
        Paint(door, 4, 61, 24, 1, Light2);
        set.securityDoor = SaveSprite(door, "Puerta_Seguridad", new Vector2(0.5f, 0f));

        // placa del suelo delante del computador (donde hay que dejar el clon) y el marco blanco que lo distingue
        var pad = Canvas(48, 6);
        Paint(pad, 0, 0, 48, 6, Dark);
        Stripes(pad, 1, 1, 46, 4);
        Paint(pad, 0, 5, 48, 1, Light2);
        set.securityPad = SaveSprite(pad, "Placa_Computador", new Vector2(0.5f, 0f));

        var frame2 = Canvas(44, 44);
        var white = new Color(1f, 1f, 1f, 0.9f);
        foreach (Vector2Int corner in new[] { new Vector2Int(0, 0), new Vector2Int(36, 0), new Vector2Int(0, 36), new Vector2Int(36, 36) })
        {
            Paint(frame2, corner.x, corner.y == 0 ? 0 : 42, 8, 2, white);
            Paint(frame2, corner.x == 0 ? 0 : 42, corner.y, 2, 8, white);
        }
        set.securityFrame = SaveSprite(frame2, "Marco_Computador", new Vector2(0.5f, 0f));

        // montacargas de obra: una plataforma abierta con barandas amarillas a la altura de la cintura,
        // colgada del cable por dos tirantes que se juntan en una anilla
        var cage = Canvas(64, 48);
        Paint(cage, 0, 0, 64, 10, Dark);
        Paint(cage, 1, 1, 62, 8, Mid3);
        Stripes(cage, 1, 1, 62, 3);
        Paint(cage, 1, 9, 62, 1, Light2);
        for (int i = 0; i <= 28; i++)
        {
            int y = 32 + i * 12 / 28;
            Paint(cage, 3 + i, y, 2, 1, Dark);
            Paint(cage, 59 - i, y, 2, 1, Dark);
        }
        foreach (int x in new[] { 1, 59 })
        {
            Paint(cage, x, 10, 4, 24, Dark);
            Paint(cage, x + 1, 10, 2, 24, Yellow);
        }
        foreach (int y in new[] { 19, 30 })
        {
            Paint(cage, 1, y, 62, 4, Dark);
            Paint(cage, 2, y + 1, 60, 2, Yellow);
        }
        Paint(cage, 29, 43, 6, 5, Dark);
        Paint(cage, 30, 44, 4, 3, Light);
        set.liftCage = SaveSprite(cage, "Montacargas_Cabina", new Vector2(0.5f, 0f));

        // contrapeso: chapa de carga arriba y bloques colgando debajo
        var weight = Canvas(64, 48);
        Paint(weight, 0, 40, 64, 8, Dark);
        Paint(weight, 1, 41, 62, 6, Mid3);
        Paint(weight, 1, 46, 62, 1, Light2);
        for (int i = 0; i < 3; i++)
        {
            int y = 2 + i * 13;
            Paint(weight, 6, y, 52, 12, Dark);
            Paint(weight, 7, y + 1, 50, 10, Mid2);
            Paint(weight, 7, y + 10, 50, 1, Light);
        }
        Paint(weight, 30, 0, 4, 40, Dark);
        set.liftWeight = SaveSprite(weight, "Montacargas_Contrapeso", new Vector2(0.5f, 1f));

    }

    // tecla de teclado con las esquinas redondeadas y un pixel de sombra debajo; devuelve donde acaba
    static int Keycap(Texture2D texture, string label, int x, int y)
    {
        int width = PixelText.Width(label) + 4;
        Paint(texture, x + 1, y - 1, width - 2, 1, Hex("8C8879"));
        Paint(texture, x + 1, y, width - 2, 11, Dark);
        Paint(texture, x, y + 1, width, 9, Dark);
        Paint(texture, x + 1, y + 1, width - 2, 9, Hex("ECEAE2"));
        PixelText.Draw(texture, label, x + 2, y + 2, Dark);
        return x + width;
    }

    // tira de cinta cruzando en diagonal una esquina de arriba (side = 1 la izquierda, -1 la derecha)
    static void Tape(Texture2D texture, int x, int y, int side)
    {
        Color tape = Hex("E2D9AE"), edge = Hex("9C9270");
        for (int u = -8; u <= 8; u++)
            for (int v = -8; v <= 8; v++)
            {
                int across = Mathf.Abs(u + v), along = Mathf.Abs(u - v);
                int px = x + u * side, py = y + v;
                if (across > 4 || along > 11 || px < 0 || py < 0 || px >= texture.width || py >= texture.height) continue;
                texture.SetPixel(px, py, across == 4 || along >= 10 ? edge : tape);
            }
    }

    // ---------- cinematica del principio ----------

    static void CreateIntroSprites(Set set)
    {
        // tanque del especimen: 3 de ancho y 5.5 de alto. Base y tapa de metal y un tubo de cristal en medio
        const int width = 96, height = 176, cap = 14;
        var tank = Canvas(width, height);
        Paint(tank, 8, cap, width - 16, height - 2 * cap, new Color(0.06f, 0.12f, 0.11f, 0.9f));
        TankCap(tank, 0, width, cap);
        TankCap(tank, height - cap, width, cap);
        for (int x = 16; x < width - 16; x += 14) Paint(tank, x, 6, 3, 2, Hex("4FE08A"));
        set.specimenTank = SaveSprite(tank, "Tanque_Especimen", new Vector2(0.5f, 0f));

        // liquido verde: mas oscuro abajo y con la superficie clara
        var liquid = Canvas(width, height);
        int surface = height - cap - 10;
        for (int y = cap; y < surface; y++)
            Paint(liquid, 9, y, width - 18, 1, Color.Lerp(new Color(0.08f, 0.4f, 0.22f, 0.8f), new Color(0.3f, 0.9f, 0.5f, 0.55f), (y - cap) / (float)(surface - cap)));
        Paint(liquid, 9, surface, width - 18, 1, new Color(0.75f, 1f, 0.85f, 0.9f));
        set.specimenLiquid = SaveSprite(liquid, "Tanque_Liquido", new Vector2(0.5f, 0f));

        set.specimenGlass = new[]
        {
            SaveSprite(TankGlass(0, width, height, cap), "Tanque_Cristal", new Vector2(0.5f, 0f)),
            SaveSprite(TankGlass(1, width, height, cap), "Tanque_Cristal_Rajado", new Vector2(0.5f, 0f)),
            SaveSprite(TankGlass(2, width, height, cap), "Tanque_Cristal_Roto", new Vector2(0.5f, 0f)),
        };

        // cientificos con bata: cada uno con su piel, su pelo y algo distinto (gafas o carpeta)
        set.scientists = new[]
        {
            ScientistFrames("Cientifico_1", Hex("E8B896"), Hex("5A3A22"), true, false),
            ScientistFrames("Cientifico_2", Hex("B57A52"), Hex("1E1A1A"), false, true),
            ScientistFrames("Cientifico_3", Hex("7A4E33"), Hex("B9B9B9"), true, true),
        };
        set.foregroundScientists = new[]
        {
            ForegroundScientist("Cientifico_Delante_1", false),
            ForegroundScientist("Cientifico_Delante_2", true),
        };
        CreateObservationWindow(set);
    }

    // ventana de la sala de observacion (la cinematica se ve a traves de ella): marco de metal para un hueco
    // de 12x7 tiles, la pared oscura de alrededor y el cristal entero, rajado y roto
    static void CreateObservationWindow(Set set)
    {
        const int width = 384, height = 224, border = 6;
        int outerWidth = width + 2 * border, outerHeight = height + 2 * border;
        var frame = Canvas(outerWidth, outerHeight);
        for (int i = 0; i < border; i++)
        {
            Color c = i == 0 ? Hex("050608") : i == border - 1 ? Light : i < 3 ? Dark : Mid2;
            Paint(frame, i, i, outerWidth - 2 * i, 1, c);
            Paint(frame, i, outerHeight - 1 - i, outerWidth - 2 * i, 1, c);
            Paint(frame, i, i, 1, outerHeight - 2 * i, c);
            Paint(frame, outerWidth - 1 - i, i, 1, outerHeight - 2 * i, c);
        }
        set.windowFrame = SaveSprite(frame, "Ventana_Marco", new Vector2(0.5f, 0.5f));

        var wall = Canvas(32, 32);
        Paint(wall, 0, 0, 32, 32, Hex("0D1016"));
        Paint(wall, 0, 0, 32, 1, Hex("171B23"));
        Paint(wall, 0, 0, 1, 32, Hex("171B23"));
        wall.SetPixel(4, 27, Hex("1D222C"));
        wall.SetPixel(27, 27, Hex("1D222C"));
        set.windowWall = SaveSprite(wall, "Pared_Observacion", new Vector2(0.5f, 0.5f));

        set.windowGlass = new[]
        {
            SaveSprite(WindowGlass(0, width, height), "Ventana_Cristal", new Vector2(0.5f, 0.5f)),
            SaveSprite(WindowGlass(1, width, height), "Ventana_Cristal_Rajado", new Vector2(0.5f, 0.5f)),
            SaveSprite(WindowGlass(2, width, height), "Ventana_Cristal_Roto", new Vector2(0.5f, 0.5f)),
        };
    }

    // cristal: tinte azulado, reflejos en diagonal, mas oscuro junto al marco, huellas y polvo.
    // state: 0 entero, 1 rajado desde un golpe, 2 roto (solo quedan picos pegados al marco)
    static Texture2D WindowGlass(int state, int width, int height)
    {
        var glass = Canvas(width, height);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                int edge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                if (state == 2 && edge >= 4 + Mathf.Abs((x * 7 + y * 13) / 9 % 16 - 8)) continue;
                Color c = Color.Lerp(new Color(0.1f, 0.15f, 0.2f, 0.25f), new Color(0.6f, 0.8f, 0.9f, 0.06f), edge / 12f);
                int diagonal = x + y;
                if (diagonal >= 110 && diagonal < 145) c = new Color(0.85f, 0.95f, 1f, 0.1f);
                if (diagonal >= 152 && diagonal < 157) c = new Color(0.85f, 0.95f, 1f, 0.14f);
                if (diagonal >= 400 && diagonal < 418) c = new Color(0.85f, 0.95f, 1f, 0.08f);
                if (diagonal >= 425 && diagonal < 428) c = new Color(0.85f, 0.95f, 1f, 0.12f);
                glass.SetPixel(x, y, c);
            }

        var rng = new System.Random(11);
        if (state != 2)
        {
            foreach (Vector2Int spot in new[] { new Vector2Int(110, 40), new Vector2Int(250, 52), new Vector2Int(280, 34) })
                for (int i = 0; i < 90; i++)
                {
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f, radius = (float)rng.NextDouble() * 13f;
                    glass.SetPixel(spot.x + (int)(Mathf.Cos(angle) * radius), spot.y + (int)(Mathf.Sin(angle) * radius * 1.3f), new Color(0.8f, 0.85f, 0.9f, 0.09f));
                }
            for (int i = 0; i < 140; i++)
                glass.SetPixel(rng.Next(width), rng.Next(height), new Color(0.75f, 0.78f, 0.8f, 0.12f + (float)rng.NextDouble() * 0.12f));
        }

        // grietas que salen del golpe, con alguna rama
        if (state == 1)
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI * 2f / 10f + (float)rng.NextDouble() * 0.4f, x = 280f, y = 150f;
                int length = rng.Next(40, 120);
                for (int s = 0; s < length; s++)
                {
                    angle += ((float)rng.NextDouble() - 0.5f) * 0.5f;
                    x += Mathf.Cos(angle);
                    y += Mathf.Sin(angle);
                    if (x < 0 || x >= width || y < 0 || y >= height) break;
                    glass.SetPixel((int)x, (int)y, new Color(1f, 1f, 1f, 0.75f));
                    if (s == length / 2) length += rng.Next(0, 20);
                }
            }
        return glass;
    }

    // base o tapa del tanque: chapa con borde claro arriba, sombra abajo y remaches
    static void TankCap(Texture2D texture, int y, int width, int height)
    {
        Paint(texture, 0, y, width, height, Dark);
        Paint(texture, 1, y + 1, width - 2, height - 2, Mid3);
        Paint(texture, 1, y + 1, width - 2, 2, Mid2);
        Paint(texture, 1, y + height - 2, width - 2, 1, Light2);
        for (int x = 5; x < width; x += 9) texture.SetPixel(x, y + height / 2 + 1, Light2);
    }

    // cristal de delante del tanque. state: 0 entero, 1 rajado, 2 roto (quedan dientes en la base y en la tapa)
    static Texture2D TankGlass(int state, int width, int height, int cap)
    {
        var glass = Canvas(width, height);
        var rng = new System.Random(7);
        int left = 8, right = width - 9, bottom = cap, top = height - cap - 1;
        for (int x = left; x <= right; x++)
        {
            int keepBottom = top, keepTop = bottom;
            if (state == 2)
            {
                keepBottom = bottom + 14 - Mathf.Abs(x % 24 - 12) + rng.Next(0, 3);
                keepTop = top - 11 + Mathf.Abs((x + 7) % 18 - 9) - rng.Next(0, 3);
            }
            for (int y = bottom; y <= top; y++)
            {
                if (state == 2 && y > keepBottom && y < keepTop) continue;
                Color c = new Color(0.6f, 0.9f, 1f, 0.08f);
                if (x == left + 5 || x == left + 6 || x == left + 11) c = new Color(1f, 1f, 1f, 0.35f);
                if (x >= right - 5) c = new Color(0.3f, 0.6f, 0.7f, 0.18f);
                if (x == left || x == right) c = new Color(0.75f, 0.95f, 1f, 0.7f);
                glass.SetPixel(x, y, c);
            }
        }

        // grietas que salen de un punto
        if (state == 1)
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7f + (float)rng.NextDouble() * 0.6f, x = width / 2f + 3f, y = height / 2f;
                int length = rng.Next(20, 40);
                for (int s = 0; s < length; s++)
                {
                    angle += ((float)rng.NextDouble() - 0.5f) * 0.6f;
                    x += Mathf.Cos(angle);
                    y += Mathf.Sin(angle);
                    if (x <= left || x >= right || y <= bottom || y >= top) break;
                    glass.SetPixel((int)x, (int)y, new Color(1f, 1f, 1f, 0.9f));
                }
            }
        return glass;
    }

    // cientifico que pasa por delante de la camara: se ve de cintura para arriba (lo corta el borde de la pantalla),
    // casi en silueta y con un filo de luz por delante. 64x128 mirando a la derecha; fotogramas: quieto y 2 pasos
    static Sprite[] ForegroundScientist(string name, bool clipboard)
    {
        Color coat = Hex("232A35"), back = Hex("161A22"), opening = Hex("2E3746"), skin = Hex("2A2224"), hair = Hex("0E0F14");
        var strip = Canvas(64 * 3, 128);
        for (int frame = 0; frame < 3; frame++)
        {
            int ox = frame * 64, up = frame == 1 ? 1 : 0;

            // bata hasta abajo con los hombros redondeados, la espalda en sombra y la abertura delante
            Paint(strip, ox + 15, up, 34, 79, coat);
            Paint(strip, ox + 17, 79 + up, 30, 3, coat);
            Paint(strip, ox + 20, 82 + up, 24, 3, coat);
            Paint(strip, ox + 15, up, 4, 79, back);
            Paint(strip, ox + 40, up, 2, 70, opening);

            // cuello y cabeza redonda con el pelo por arriba y por detras, nariz y el brillo de las gafas
            Paint(strip, ox + 28, 84 + up, 8, 7, skin);
            for (int x = 20; x <= 46; x++)
                for (int y = 89; y <= 115; y++)
                {
                    float dx = x - 33f, dy = y - 102f;
                    if (dx * dx + dy * dy > 13.5f * 13.5f) continue;
                    strip.SetPixel(ox + x, y + up, y > 106 || x < 29 ? hair : skin);
                }
            Paint(strip, ox + 47, 100 + up, 1, 3, skin);
            Paint(strip, ox + 42, 104 + up, 3, 1, Hex("9AA8B8"));

            // brazo de delante: suelto o doblado sujetando una carpeta
            if (clipboard)
            {
                Paint(strip, ox + 40, 46 + up, 8, 34, coat);
                Paint(strip, ox + 40, 40 + up, 16, 8, coat);
                Paint(strip, ox + 50, 34 + up, 11, 26, Hex("2B2119"));
                Paint(strip, ox + 50, 57 + up, 11, 2, Hex("4A4440"));
            }
            else
            {
                Paint(strip, ox + 40, 22 + up, 8, 58, coat);
                Paint(strip, ox + 41, 14 + up, 6, 8, skin);
            }

            // filo de luz: el ultimo pixel de cada fila por delante
            for (int y = 0; y < 128; y++)
                for (int x = ox + 63; x >= ox; x--)
                {
                    if (strip.GetPixel(x, y).a == 0f) continue;
                    strip.SetPixel(x, y, Hex("5B6B80"));
                    break;
                }
        }
        return SaveStrip(strip, name, 64, 128, new Vector2(0.5f, 0f));
    }


    static Sprite[] ScientistFrames(string name, Color skin, Color hair, bool glasses, bool clipboard)
    {
        var strip = Canvas(24 * 5, 48);
        for (int frame = 0; frame < 5; frame++) DrawScientist(strip, frame * 24, frame, skin, hair, glasses, clipboard);
        return SaveStrip(strip, name, 24, 48, new Vector2(0.5f, 0f));
    }

    // cientifico de 24x48 mirando a la derecha. frame 0: quieto; 1 a 4: pasos
    static void DrawScientist(Texture2D t, int ox, int frame, Color skin, Color hair, bool glasses, bool clipboard)
    {
        Color coat = Hex("E4E8EC"), coatShade = Hex("A9B3BE");
        int stride = new[] { 0, 3, 0, -3, 0 }[frame];
        int up = frame == 2 || frame == 4 ? 1 : 0;

        // piernas inclinadas segun el paso (la de atras mas oscura) y zapatos
        for (int y = 2; y <= 14 + up; y++)
        {
            int k = Mathf.RoundToInt(stride * (14f + up - y) / (12f + up));
            Paint(t, ox + 9 - k, y, 2, 1, Hex("232838"));
            Paint(t, ox + 12 + k, y, 2, 1, Hex("2E3546"));
        }
        Paint(t, ox + 9 - stride, 0, 3, 2, Hex("15161A"));
        Paint(t, ox + 12 + stride, 0, 3, 2, Hex("15161A"));

        // bata hasta las rodillas con la espalda en sombra, la abertura, un bolsillo y el cuello de la camisa
        Paint(t, ox + 7, 12 + up, 11, 5, coat);
        Paint(t, ox + 8, 17 + up, 9, 15, coat);
        Paint(t, ox + 9, 32 + up, 7, 2, coat);
        Paint(t, ox + 7, 12 + up, 1, 5, coatShade);
        Paint(t, ox + 8, 17 + up, 1, 15, coatShade);
        Paint(t, ox + 14, 12 + up, 1, 19, coatShade);
        Paint(t, ox + 10, 21 + up, 3, 1, coatShade);
        Paint(t, ox + 13, 31 + up, 2, 3, Hex("6F93C2"));

        // cabeza: pelo por arriba y por detras, el ojo mirando a la derecha
        Paint(t, ox + 10, 34 + up, 7, 8, skin);
        Paint(t, ox + 9, 40 + up, 8, 3, hair);
        Paint(t, ox + 9, 36 + up, 2, 5, hair);
        t.SetPixel(ox + 15, 38 + up, Dark);
        if (glasses)
        {
            t.SetPixel(ox + 14, 38 + up, Light2);
            t.SetPixel(ox + 16, 38 + up, Light2);
        }

        // brazo de delante: con carpeta va doblado; si no, se balancea al contrario que la pierna
        if (clipboard)
        {
            Paint(t, ox + 14, 24 + up, 2, 8, coat);
            Paint(t, ox + 16, 19 + up, 5, 8, Hex("6B4A2B"));
            Paint(t, ox + 17, 20 + up, 3, 6, Hex("EDEDE6"));
            Paint(t, ox + 15, 23 + up, 2, 2, skin);
        }
        else
        {
            for (int y = 19; y <= 31; y++)
            {
                int k = Mathf.RoundToInt(-stride * 0.6f * (31f - y) / 12f);
                Paint(t, ox + 14 + k, y + up, 2, 1, y > 20 ? coat : skin);
                if (y > 20) t.SetPixel(ox + 14 + k, y + up, coatShade);
            }
        }

        // contorno oscuro para que se lea sobre cualquier fondo
        var outline = new List<Vector2Int>();
        for (int x = ox; x < ox + 24; x++)
            for (int y = 0; y < 48; y++)
            {
                if (t.GetPixel(x, y).a > 0f) continue;
                if ((x > ox && t.GetPixel(x - 1, y).a > 0f) || (x < ox + 23 && t.GetPixel(x + 1, y).a > 0f) ||
                    (y > 0 && t.GetPixel(x, y - 1).a > 0f) || (y < 47 && t.GetPixel(x, y + 1).a > 0f))
                    outline.Add(new Vector2Int(x, y));
            }
        foreach (Vector2Int p in outline) t.SetPixel(p.x, p.y, Hex("14161D"));
    }

    // franja de peligro amarilla y negra en diagonal
    static void Stripes(Texture2D texture, int x0, int y0, int width, int height)
    {
        for (int x = x0; x < x0 + width; x++)
            for (int y = y0; y < y0 + height; y++)
                texture.SetPixel(x, y, (x + y) / 3 % 2 == 0 ? Yellow : Black);
    }

    static void Circle(Texture2D texture, float cx, float cy, float radius, Color color)
    {
        for (int x = (int)(cx - radius); x <= (int)(cx + radius); x++)
            for (int y = (int)(cy - radius); y <= (int)(cy + radius); y++)
                if ((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy) <= radius * radius)
                    texture.SetPixel(x, y, color);
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }

    // guarda una tira de fotogramas y la corta
    static Sprite[] SaveStrip(Texture2D texture, string name, int width, int height, Vector2 pivot)
    {
        string path = $"{Generated}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        return SliceStrip(path, width, height, pivot);
    }

    // ---------- ayudas ----------

    static SpriteRect NewRect(string name, Rect rect, Vector2 pivot)
    {
        return new SpriteRect { name = name, rect = rect, alignment = SpriteAlignment.Custom, pivot = pivot, spriteID = GUID.Generate() };
    }

    // importa la textura como pixel art de 32 px por unidad y la corta en los rectangulos dados
    static Sprite[] Slice(string path, List<SpriteRect> rects)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        SetPixelArt(importer, SpriteImportMode.Multiple);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        // si ya estaba cortada conserva los mismos IDs, asi no se rompe nada que use esos sprites
        var oldIds = new Dictionary<string, GUID>();
        foreach (SpriteRect old in provider.GetSpriteRects()) oldIds[old.name] = old.spriteID;
        foreach (SpriteRect rect in rects)
            if (oldIds.TryGetValue(rect.name, out GUID id)) rect.spriteID = id;

        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
    }

    static void SetPixelArt(TextureImporter importer, SpriteImportMode mode)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = mode;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        // FullRect hace falta para dibujar el rayo del laser en modo Tiled
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }

    static Vector2Int TextureSize(string path)
    {
        var texture = new Texture2D(2, 2);
        texture.LoadImage(File.ReadAllBytes(path));
        var size = new Vector2Int(texture.width, texture.height);
        Object.DestroyImmediate(texture);
        return size;
    }

    static Texture2D Canvas(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(new Color32[width * height]);
        return texture;
    }

    static void Paint(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int j = y; j < y + height; j++)
            for (int i = x; i < x + width; i++)
                texture.SetPixel(i, j, color);
    }

    // border: los pixeles de cada lado que no se estiran en modo Sliced (izquierda, abajo, derecha, arriba)
    static Sprite SaveSprite(Texture2D texture, string name, Vector2 pivot, Vector4 border = default)
    {
        string path = $"{Generated}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        SetPixelArt(importer, SpriteImportMode.Single);
        importer.spritePivot = pivot;
        importer.spriteBorder = border;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void CreateFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
}
