using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Retoques sobre escenas que ya edita el equipo a mano (no se regeneran: se cambia solo lo necesario).
public static partial class LabBuilder
{
    // Nivel Tutorial: un pasillo a la izquierda de la primera sala para aprender el salto. Un escalon bajo que se sube
    // con un toque y otro alto (2 casillas mas) que solo se sube manteniendo el boton, con su cartel y una luz.
    // La puerta de entrada pasa al principio del pasillo, el jugador empieza alli y el sismo espera a que entres en la sala
    [MenuItem("Replica/Tutorial: zona de salto al principio")]
    static void AddJumpIntro()
    {
        const int left = -12, floor = 5, top = 11, room = 2;
        Tilemap terrain = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include).First(t => t.name == "Terreno");
        Tilemap back = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include).First(t => t.name == "Fondo");
        TileBase rock = terrain.GetTile(new Vector3Int(0, 8, 0));
        TileBase wall = back.GetTile(new Vector3Int(room + 3, 8, 0));

        // el pasillo, abierto a la sala (lo que habia encima de la puerta vieja tambien se quita)
        for (int x = left; x <= room; x++)
            for (int y = floor; y <= top; y++)
            {
                terrain.SetTile(new Vector3Int(x, y, 0), null);
                back.SetTile(new Vector3Int(x, y, 0), wall);
            }
        // dintel de la puerta nueva
        for (int y = floor + 3; y <= top; y++) terrain.SetTile(new Vector3Int(left, y, 0), rock);
        // escalon bajo (1 casilla) y alto (2 mas)
        for (int x = left + 4; x <= left + 6; x++) terrain.SetTile(new Vector3Int(x, floor, 0), rock);
        for (int x = left + 7; x <= left + 9; x++)
            for (int y = floor; y <= floor + 2; y++) terrain.SetTile(new Vector3Int(x, y, 0), rock);

        // el colisionador del terreno se rehace ya: si no, la escena guardaria la forma vieja y el jugador quedaria dentro
        var tileCollider = terrain.GetComponent<TilemapCollider2D>();
        if (tileCollider != null) tileCollider.ProcessTilemapChanges();
        var composite = terrain.GetComponent<CompositeCollider2D>();
        if (composite != null) composite.GenerateGeometry();

        // la puerta de entrada al principio del pasillo y el jugador al lado
        GameObject door = GameObject.Find("Decorado/Puerta bloqueada");
        Undo.RecordObject(door.transform, "Zona de salto");
        door.transform.position = new Vector3(left + 0.5f, floor);
        Player player = Object.FindAnyObjectByType<Player>();
        player.transform.position = new Vector3(left + 2.5f, floor + 0.5f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);

        // la camara ya puede llegar al principio del pasillo (media pantalla son 15 casillas)
        var follow = new SerializedObject(Object.FindAnyObjectByType<CameraFollow>());
        Vector2 limits = follow.FindProperty("limitsX").vector2Value;
        follow.FindProperty("limitsX").vector2Value = new Vector2(left + 15f, limits.y);
        follow.ApplyModifiedPropertiesWithoutUndo();

        // el cartel encima de los escalones y una luz
        var sign = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{LevelFolder}/Cartel.prefab"), GameObject.Find("Decorado").transform);
        sign.transform.position = new Vector3(left + 6.5f, 10f);
        var so = new SerializedObject(sign.GetComponent<Sign>());
        so.FindProperty("title").stringValue = "SALTO";
        SerializedProperty rows = so.FindProperty("rows");
        rows.arraySize = 2;
        rows.GetArrayElementAtIndex(0).FindPropertyRelative("control").enumValueIndex = (int)Sign.Control.Saltar;
        rows.GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue = "TOCA: SALTO BAJO";
        rows.GetArrayElementAtIndex(1).FindPropertyRelative("control").enumValueIndex = (int)Sign.Control.Saltar;
        rows.GetArrayElementAtIndex(1).FindPropertyRelative("text").stringValue = "MANTEN: SALTO ALTO";
        so.ApplyModifiedPropertiesWithoutUndo();
        var lamp = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{LevelFolder}/Fluorescente.prefab"), GameObject.Find("Luces").transform);
        lamp.transform.position = new Vector3(left + 6f, top + 1f);

        // el sismo, al entrar en la sala, con el temblor y las luces volviendo con sus chasquidos
        var shock = Object.FindAnyObjectByType<Aftershock>();
        var lightsSound = shock.GetComponent<AudioSource>();
        if (lightsSound == null) lightsSound = shock.gameObject.AddComponent<AudioSource>();
        lightsSound.clip = Clip("Luces");
        lightsSound.outputAudioMixerGroup = MixerGroup("Efectos");
        lightsSound.playOnAwake = false;
        var shockSo = new SerializedObject(shock);
        shockSo.FindProperty("triggerX").floatValue = room + 1.5f;
        shockSo.FindProperty("delay").floatValue = 0.4f;
        shockSo.FindProperty("quakeSound").objectReferenceValue =
            Mix("Temblor corto", 1f, L("Temblor 1", 0f, 0.25f, 1f, 0f, 1.8f, 0.9f), L("Tembloe 2", 0.2f, 0.3f, 0.9f, 5f, 1.6f, 1f));
        shockSo.FindProperty("lightsSound").objectReferenceValue = lightsSound;
        shockSo.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        Debug.Log("Zona de salto puesta al principio del tutorial");
    }
}
