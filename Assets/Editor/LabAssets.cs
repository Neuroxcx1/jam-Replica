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

    static Sprite SaveSprite(Texture2D texture, string name, Vector2 pivot)
    {
        string path = $"{Generated}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        SetPixelArt(importer, SpriteImportMode.Single);
        importer.spritePivot = pivot;
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
