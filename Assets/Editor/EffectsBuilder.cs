using System.IO;
using UnityEditor;
using UnityEngine;

// Genera las texturas pixel art, materiales y prefabs de efectos en Assets/Effects.
// Lo usa PrototypeBuilder; todo lo que crea se puede retocar luego en el Inspector.
public static class EffectsBuilder
{
    const string Root = "Assets/Effects";
    // misma resolucion que el juego: 32 pixeles por unidad (personaje de 32 px)
    const int PPU = 32;

    public class EffectSet
    {
        public Material cloneMaterial;
        public Material bodyMaterial;
        public Material afterimageMaterial;
        public ParticleSystem walkDust;
        public GameObject jumpDust;
        public GameObject landDust;
        public GameObject death;
        public GameObject respawn;
        public GameObject solidify;
        public GameObject speedLines;
        public GameObject cloneImpact;
        public GameObject absorb;
        public RecallGhost recallGhost;
        public SplitEffect split;
        public BodyEffect freeze;
        public BodyEffect corpse;
        public SoulOrb soulOrb;
    }

    // paleta carnal: piel, rojo y blanco (el hielo sigue en azules)
    static readonly Color Dust = new Color(0.93f, 0.88f, 0.84f, 0.85f);
    static readonly Color Skin = new Color(0.97f, 0.72f, 0.6f);
    static readonly Color SkinDark = new Color(0.82f, 0.48f, 0.42f);
    static readonly Color Red = new Color(0.92f, 0.2f, 0.22f);
    static readonly Color DeepRed = new Color(0.45f, 0.06f, 0.09f);
    static readonly Color White = new Color(1f, 0.96f, 0.93f);
    static readonly Color Ice = new Color(0.7f, 0.95f, 1f);

    public static EffectSet Build()
    {
        CreateFolder(Root);
        CreateFolder(Root + "/Textures");
        CreateFolder(Root + "/Materials");
        CreateFolder(Root + "/Prefabs");

        Texture2D puffTex = SaveTexture(PuffSheet(), "Puff", false).texture;
        Texture2D sparkTex = SaveTexture(SparkSheet(), "Spark", false).texture;
        Texture2D ringTex = SaveTexture(RingSheet(), "Ring", false).texture;
        Sprite pixel = SaveTexture(Solid(1, 1), "Pixel", false);
        Sprite orb = SaveTexture(OrbTexture(), "Orb", false);
        Sprite frost = SaveTexture(FrostTexture(), "Frost", true);

        Shader particleShader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/PixelParticle.shader");
        Material puff = SaveMaterial("FX_Puff", particleShader, puffTex);
        Material spark = SaveMaterial("FX_Spark", particleShader, sparkTex);
        Material ring = SaveMaterial("FX_Ring", particleShader, ringTex);
        Material dot = SaveMaterial("FX_Pixel", particleShader, pixel.texture);

        // un solo shader para todos los "looks" de cuerpos y replicas: solo cambian los valores.
        // Las replicas brillan solas (Unlit) y los cuerpos reciben la luz de la sala (Lit)
        Shader replica = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/ReplicaSprite.shadergraph");
        Shader replicaLit = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/ReplicaSpriteLit.shadergraph");
        var set = new EffectSet
        {
            cloneMaterial = ReplicaMaterial("Replica_Clone", replica, DeepRed, Skin, White, Red,
                scan: 0.3f, shine: 0f, flicker: 0.12f, opacity: 0.85f),
            bodyMaterial = ReplicaMaterial("Replica_Body", replicaLit, new Color(0.35f, 0.12f, 0.12f), new Color(0.78f, 0.55f, 0.48f),
                new Color(0.95f, 0.85f, 0.8f), new Color(0.42f, 0.12f, 0.12f), scan: 0f, shine: 0f, flicker: 0f, opacity: 1f),
            afterimageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat")
        };
        Material frozen = ReplicaMaterial("Replica_Frozen", replicaLit, new Color(0.18f, 0.38f, 0.7f), new Color(0.55f, 0.82f, 0.97f),
            new Color(0.92f, 0.99f, 1f), new Color(0.97f, 1f, 1f), scan: 0f, shine: 0.8f, flicker: 0f, opacity: 1f);
        Material dead = ReplicaMaterial("Replica_Dead", replicaLit, new Color(0.25f, 0.18f, 0.2f), new Color(0.62f, 0.52f, 0.5f),
            new Color(0.86f, 0.79f, 0.76f), new Color(0.35f, 0.1f, 0.12f), scan: 0f, shine: 0f, flicker: 0f, opacity: 1f);

        Material frostMat = SaveMaterial("FrostDecal", AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/FrostDecal.shadergraph"), frost.texture);
        frostMat.SetColor("_ColorDark", new Color(0.16f, 0.3f, 0.55f));
        frostMat.SetColor("_ColorLight", new Color(0.8f, 0.96f, 1f));
        frostMat.SetFloat("_Progress", 1f);
        frostMat.SetFloat("_Opacity", 0.85f);

        set.walkDust = SavePrefab(WalkDust(puff), "WalkDust").GetComponent<ParticleSystem>();
        set.jumpDust = SavePrefab(GroundDust("JumpDust", puff, 9, 5f, 1f), "JumpDust");
        set.landDust = SavePrefab(GroundDust("LandDust", puff, 8, 4.5f, 0.3f), "LandDust");
        set.split = SavePrefab(Split(puff, ring, dot), "CloneSplit").GetComponent<SplitEffect>();
        set.freeze = SavePrefab(Freeze(frost, frostMat, frozen, spark, ring, dot), "Freeze").GetComponent<BodyEffect>();
        set.corpse = SavePrefab(Corpse(dead, puff, ring), "Corpse").GetComponent<BodyEffect>();
        set.death = SavePrefab(Death(puff, spark, ring), "Death");
        set.soulOrb = SavePrefab(Soul(orb, dot), "SoulOrb").GetComponent<SoulOrb>();
        set.respawn = SavePrefab(Respawn(puff, spark, ring), "Respawn");
        set.solidify = SavePrefab(Solidify(puff, ring, dot), "CloneSolidify");
        set.speedLines = SavePrefab(SpeedLines(dot), "SpeedLines");
        set.cloneImpact = SavePrefab(CloneImpact(puff, ring, spark, dot), "CloneImpact");
        set.absorb = SavePrefab(Absorb(ring, spark), "Absorb");
        set.recallGhost = SavePrefab(Ghost(set.cloneMaterial, set.afterimageMaterial, dot, set.absorb), "RecallGhost").GetComponent<RecallGhost>();
        return set;
    }

    static Material ReplicaMaterial(string name, Shader shader, Color dark, Color mid, Color light, Color edge,
        float scan, float shine, float flicker, float opacity)
    {
        Material m = SaveMaterial(name, shader, null);
        m.SetColor("_ColorDark", dark);
        m.SetColor("_ColorMid", mid);
        m.SetColor("_ColorLight", light);
        m.SetFloat("_RampAmount", 1f);
        m.SetFloat("_RampSteps", 4f);
        m.SetColor("_EdgeColor", edge);
        m.SetFloat("_EdgeAmount", 1f);
        m.SetColor("_ScanColor", Red);
        m.SetFloat("_ScanAmount", scan);
        m.SetFloat("_ScanSpeed", 12f);
        m.SetFloat("_ShineAmount", shine);
        m.SetFloat("_ShineSpeed", 40f);
        m.SetFloat("_ShinePeriod", 90f);
        m.SetFloat("_FlickerAmount", flicker);
        m.SetFloat("_Opacity", opacity);
        m.SetFloat("_Flash", 0f);
        m.SetFloat("_PixelsPerUnit", PPU);
        return m;
    }

    // ---------- efectos ----------

    static GameObject WalkDust(Material puff)
    {
        ParticleSystem ps = Particles(null, "WalkDust", puff, 2);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startColor = Dust;
        main.gravityModifier = -0.03f;

        var emission = ps.emission;
        emission.rateOverDistance = 3.5f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.3f, 0.02f, 0f);
        shape.position = new Vector3(0f, 0.03f, 0f);

        VelocityDecay(ps, new Vector2(-0.5f, 0.15f), new Vector2(0.5f, 0.6f));
        Sheet(ps, 4, 0.5f, 0.75f);
        FadeOut(ps, 0.5f);
        return ps.gameObject;
    }

    // polvo de salto y de aterrizaje: semicirculo de bolitas que se abre y se frena
    // flatten < 1 aplasta el semicirculo para que salga mas hacia los lados
    static GameObject GroundDust(string name, Material puff, int count, float speed, float flatten)
    {
        ParticleSystem ps = Particles(null, name, puff, 2);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.34f);
        main.startSpeed = speed;
        main.startColor = Dust;
        main.stopAction = ParticleSystemStopAction.Destroy;
        Burst(ps, count);

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.arc = 180f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
        shape.scale = new Vector3(1f, flatten, 1f);
        shape.position = new Vector3(0f, 0.1f, 0f);

        Drag(ps, 9f);
        Sheet(ps, 4, 0.25f, 0.25f);
        FadeOut(ps, 0.5f);
        return ps.gameObject;
    }

    // replicarse: membrana que se estira y revienta + humo + anillos + chorro de gotas hacia delante
    static GameObject Split(Material puff, Material ring, Material dot)
    {
        var root = new GameObject("CloneSplit");

        // 3 hebras de carne
        var strands = new LineRenderer[3];
        for (int i = 0; i < strands.Length; i++)
        {
            var strandGo = new GameObject("Strand" + i);
            strandGo.transform.SetParent(root.transform, false);
            var line = strandGo.AddComponent<LineRenderer>();
            line.sharedMaterial = dot;
            line.positionCount = 7;
            line.useWorldSpace = true;
            line.sortingOrder = 4;
            line.numCapVertices = 2;
            line.colorGradient = Gradient2(SkinDark, new Color(1f, 0.84f, 0.76f), SkinDark);
            strands[i] = line;
        }

        ParticleSystem smoke = Particles(root.transform, "Smoke", puff, 2);
        var main = smoke.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        main.startColor = RandomColor(Skin, Red, White);
        Burst(smoke, 7);
        Circle(smoke, 0.5f);
        var smokeShape = smoke.shape;
        smokeShape.radiusThickness = 0f;
        VelocityDecay(smoke, new Vector2(-1.2f, -0.2f), new Vector2(1.2f, 1f));
        Sheet(smoke, 4, 0.25f, 0.25f);
        FadeOut(smoke, 0.5f);

        RingWave(root.transform, ring, new Color(1f, 0.45f, 0.4f), 2f, 0.25f, 5);
        RingWave(root.transform, ring, White, 2.8f, 0.18f, 6);

        // chorro de gotas hacia donde sale la replica (SplitEffect orienta el cono)
        ParticleSystem spray = Particles(root.transform, "Spray", dot, 6);
        main = spray.main;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(2f / PPU, 5f / PPU);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 13f);
        main.startColor = RandomColor(Red, Skin, White);
        main.gravityModifier = 0.9f;
        Burst(spray, 16);
        var sprayShape = spray.shape;
        sprayShape.enabled = true;
        sprayShape.shapeType = ParticleSystemShapeType.Cone;
        sprayShape.angle = 22f;
        sprayShape.radius = 0.15f;
        Drag(spray, 4f);

        ParticleSystem droplets = Particles(root.transform, "Droplets", dot, 5);
        main = droplets.main;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(2f / PPU, 4f / PPU);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startColor = RandomColor(Red, Skin, White);
        main.gravityModifier = 0.7f;
        Burst(droplets, 10);
        Circle(droplets, 0.05f);

        var split = root.AddComponent<SplitEffect>();
        var so = new SerializedObject(split);
        SerializedProperty list = so.FindProperty("strands");
        list.arraySize = strands.Length;
        for (int i = 0; i < strands.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = strands[i];
        so.FindProperty("droplets").objectReferenceValue = droplets;
        so.FindProperty("spray").objectReferenceValue = spray;
        so.ApplyModifiedPropertiesWithoutUndo();
        AddAutoDestroy(root, 1.2f);
        return root;
    }

    // lineas de velocidad mientras el clon sale disparado (Clone las enciende y apaga)
    static GameObject SpeedLines(Material dot)
    {
        ParticleSystem ps = Particles(null, "SpeedLines", dot, 1);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startSize = new ParticleSystem.MinMaxCurve(2f / PPU, 3f / PPU);
        main.startColor = RandomColor(White, Skin, Color.white);

        var emission = ps.emission;
        emission.rateOverDistance = 14f;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.1f, 0.9f, 0f);

        // se mueven un poco hacia atras para que las rayas se alineen en horizontal
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-3f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 12f;
        renderer.velocityScale = 0f;
        FadeOut(ps, 0.3f);
        return ps.gameObject;
    }

    // al recuperar una replica: copia holografica que vuelve volando con estela (la mueve RecallGhost)
    static GameObject Ghost(Material clone, Material afterimageMaterial, Material dot, GameObject absorb)
    {
        var root = new GameObject("RecallGhost");
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = clone;
        sr.sortingOrder = 8;

        var trail = root.AddComponent<TrailRenderer>();
        trail.sharedMaterial = dot;
        trail.time = 0.18f;
        trail.minVertexDistance = 0.05f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 12f / PPU), new Keyframe(1f, 0f));
        trail.colorGradient = Gradient2(new Color(1f, 0.85f, 0.75f, 0.9f), new Color(0.95f, 0.3f, 0.3f, 0.6f), new Color(0.6f, 0.1f, 0.1f, 0f));
        trail.sortingOrder = 7;

        var afterimage = root.AddComponent<Afterimage>();
        var so = new SerializedObject(afterimage);
        so.FindProperty("source").objectReferenceValue = sr;
        so.FindProperty("material").objectReferenceValue = afterimageMaterial;
        so.FindProperty("interval").floatValue = 0.03f;
        so.FindProperty("fadeTime").floatValue = 0.1f;
        so.ApplyModifiedPropertiesWithoutUndo();

        SetRef(root.AddComponent<RecallGhost>(), "absorbEffect", absorb);
        return root;
    }

    // la replica vuelve a ti: anillo que se cierra sobre el jugador y unas chispitas
    static GameObject Absorb(Material ring, Material spark)
    {
        var root = new GameObject("Absorb");
        ParticleSystem wave = RingWave(root.transform, ring, Skin, 2.6f, 0.22f, 7);
        // al reves que una onda normal: el anillo se cierra hacia dentro
        var sheet = wave.textureSheetAnimation;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.99f, 1f, 0f));

        ParticleSystem sparks = Particles(root.transform, "Sparks", spark, 7);
        var main = sparks.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        main.startSize = 9f / PPU;
        main.startColor = RandomColor(Color.white, Skin, White);
        Burst(sparks, 5);
        Circle(sparks, 0.45f);
        VelocityDecay(sparks, new Vector2(-0.6f, 0.4f), new Vector2(0.6f, 1.4f));
        Sheet(sparks, 4, 0f, 0f);

        AddAutoDestroy(root, 0.8f);
        return root;
    }

    // la replica se estampa contra algo nada mas salir: golpe con anillos, polvo, trozos y chispas
    static GameObject CloneImpact(Material puff, Material ring, Material spark, Material dot)
    {
        var root = new GameObject("CloneImpact");
        RingWave(root.transform, ring, White, 3f, 0.22f, 6);
        RingWave(root.transform, ring, Red, 2f, 0.3f, 5);

        ParticleSystem puffs = Particles(root.transform, "Puffs", puff, 5);
        var main = puffs.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        main.startColor = RandomColor(Skin, White, Dust);
        Burst(puffs, 12);
        Circle(puffs, 0.35f);
        VelocityDecay(puffs, new Vector2(-2.5f, -0.6f), new Vector2(2.5f, 2f));
        Sheet(puffs, 4, 0f, 0.25f);
        FadeOut(puffs, 0.5f);

        ParticleSystem bits = Particles(root.transform, "Bits", dot, 6);
        main = bits.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(2f / PPU, 5f / PPU);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startColor = RandomColor(Red, Skin, White);
        main.gravityModifier = 1f;
        Burst(bits, 18);
        Circle(bits, 0.15f);
        Drag(bits, 3f);

        ParticleSystem sparks = Particles(root.transform, "Sparks", spark, 7);
        main = sparks.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSize = 9f / PPU;
        main.startColor = RandomColor(Color.white, White, Skin);
        Burst(sparks, 5);
        Circle(sparks, 0.4f);
        Sheet(sparks, 4, 0f, 0f);

        AddAutoDestroy(root, 1.2f);
        return root;
    }

    // congelarse: escarcha en la pared + cristalitos + destellos
    static GameObject Freeze(Sprite frost, Material frostMat, Material frozen, Material spark, Material ring, Material dot)
    {
        var root = new GameObject("Freeze");

        var frostGo = new GameObject("Frost");
        frostGo.transform.SetParent(root.transform, false);
        var frostSr = frostGo.AddComponent<SpriteRenderer>();
        frostSr.sprite = frost;
        frostSr.sharedMaterial = frostMat;
        frostSr.sortingOrder = -1;

        ParticleSystem shards = Particles(root.transform, "Shards", dot, 6);
        var main = shards.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(2f / PPU, 4f / PPU);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
        main.startColor = RandomColor(Color.white, Ice, new Color(0.45f, 0.75f, 1f));
        main.gravityModifier = 0.5f;
        Burst(shards, 14);
        Circle(shards, 0.2f);
        Drag(shards, 3f);

        ParticleSystem glints = Particles(root.transform, "Glints", spark, 0);
        main = glints.main;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.45f);
        main.startSize = 9f / PPU;
        main.startColor = RandomColor(Color.white, Ice, Color.white);
        var emission = glints.emission;
        emission.rateOverTime = 12f;
        var shape = glints.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2.2f, 2.2f, 0f);
        Sheet(glints, 4, 0f, 0f);

        RingWave(root.transform, ring, Ice, 2.5f, 0.22f, 6);

        // temblor fuerte + el juego se para un instante: sensacion de congelarse
        var freeze = root.AddComponent<BodyEffect>();
        var so = new SerializedObject(freeze);
        so.FindProperty("bodyMaterial").objectReferenceValue = frozen;
        so.FindProperty("decal").objectReferenceValue = frostSr;
        so.FindProperty("shakePixels").floatValue = 5f;
        so.FindProperty("shakeTime").floatValue = 0.35f;
        so.FindProperty("hitStop").floatValue = 0.04f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    // morir en pinchos: el cuerpo se queda (sin congelarse) con un tono apagado y una nube suave
    static GameObject Corpse(Material dead, Material puff, Material ring)
    {
        var root = new GameObject("Corpse");
        RingWave(root.transform, ring, White, 1.5f, 0.2f, 6);

        ParticleSystem puffs = Particles(root.transform, "Puffs", puff, 5);
        var main = puffs.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        main.startColor = RandomColor(Dust, Skin, White);
        Burst(puffs, 8);
        Circle(puffs, 0.35f);
        VelocityDecay(puffs, new Vector2(-1.4f, -0.2f), new Vector2(1.4f, 0.9f));
        Sheet(puffs, 4, 0.25f, 0.25f);
        FadeOut(puffs, 0.5f);

        var corpse = root.AddComponent<BodyEffect>();
        var so = new SerializedObject(corpse);
        so.FindProperty("bodyMaterial").objectReferenceValue = dead;
        so.FindProperty("flashTime").floatValue = 0.2f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    // morir: un "pop" suave con chispas que suben, nada violento
    static GameObject Death(Material puff, Material spark, Material ring)
    {
        var root = new GameObject("Death");
        RingWave(root.transform, ring, Color.white, 2f, 0.25f, 6);

        ParticleSystem sparkles = Particles(root.transform, "Sparkles", spark, 6);
        var main = sparkles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
        main.startSize = 9f / PPU;
        main.startColor = RandomColor(White, Skin, Color.white);
        main.gravityModifier = -0.05f;
        Burst(sparkles, 8);
        Circle(sparkles, 0.25f);
        VelocityDecay(sparkles, new Vector2(-1.2f, 0.6f), new Vector2(1.2f, 1.8f));
        Sheet(sparkles, 4, 0f, 0f);

        ParticleSystem puffs = Particles(root.transform, "Puffs", puff, 5);
        main = puffs.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startColor = RandomColor(Dust, Skin, Dust);
        Burst(puffs, 6);
        Circle(puffs, 0.25f);
        VelocityDecay(puffs, new Vector2(-1.5f, -0.5f), new Vector2(1.5f, 1f));
        Sheet(puffs, 4, 0f, 0.25f);
        FadeOut(puffs, 0.5f);

        AddAutoDestroy(root, 1.2f);
        return root;
    }

    // bolita de luz que vuela al checkpoint
    static GameObject Soul(Sprite orb, Material dot)
    {
        var root = new GameObject("SoulOrb");
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = orb;
        sr.color = new Color(1f, 0.95f, 0.9f);
        sr.sortingOrder = 7;

        var trail = root.AddComponent<TrailRenderer>();
        trail.sharedMaterial = dot;
        trail.time = 0.25f;
        trail.minVertexDistance = 0.05f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 8f / PPU), new Keyframe(1f, 0f));
        trail.colorGradient = Gradient2(new Color(1f, 0.95f, 0.9f, 0.9f), new Color(1f, 0.6f, 0.5f, 0.6f), new Color(0.9f, 0.2f, 0.2f, 0f));
        trail.sortingOrder = 6;

        var soul = root.AddComponent<SoulOrb>();
        SetRef(soul, "sprite", sr);
        return root;
    }

    static GameObject Respawn(Material puff, Material spark, Material ring)
    {
        var root = new GameObject("Respawn");
        RingWave(root.transform, ring, White, 2f, 0.25f, 6);

        ParticleSystem puffs = Particles(root.transform, "Puffs", puff, 5);
        var main = puffs.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startColor = RandomColor(Skin, White, Dust);
        Burst(puffs, 10);
        Circle(puffs, 0.4f);
        VelocityDecay(puffs, new Vector2(-2f, -0.3f), new Vector2(2f, 1.6f));
        Sheet(puffs, 4, 0f, 0.25f);
        FadeOut(puffs, 0.5f);

        ParticleSystem sparks = Particles(root.transform, "Sparks", spark, 6);
        main = sparks.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
        main.startSize = 9f / PPU;
        main.startColor = RandomColor(Color.white, Skin, Color.white);
        Burst(sparks, 4);
        Circle(sparks, 0.3f);
        VelocityDecay(sparks, new Vector2(-0.6f, 0.8f), new Vector2(0.6f, 1.6f));
        Sheet(sparks, 4, 0f, 0f);

        AddAutoDestroy(root, 1f);
        return root;
    }

    // replica que muere y se queda como cuerpo
    static GameObject Solidify(Material puff, Material ring, Material dot)
    {
        var root = new GameObject("CloneSolidify");
        RingWave(root.transform, ring, Red, 1.5f, 0.2f, 5);

        ParticleSystem puffs = Particles(root.transform, "Puffs", puff, 5);
        var main = puffs.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startColor = RandomColor(Skin, White, Dust);
        Burst(puffs, 8);
        Circle(puffs, 0.3f);
        VelocityDecay(puffs, new Vector2(-1.6f, -0.2f), new Vector2(1.6f, 1.2f));
        Sheet(puffs, 4, 0f, 0.25f);
        FadeOut(puffs, 0.5f);

        ParticleSystem bits = Particles(root.transform, "Bits", dot, 5);
        main = bits.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(2f / PPU, 4f / PPU);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startColor = RandomColor(Skin, White, Color.white);
        main.gravityModifier = 0.6f;
        Burst(bits, 6);
        Circle(bits, 0.1f);

        AddAutoDestroy(root, 1f);
        return root;
    }

    static ParticleSystem RingWave(Transform parent, Material ring, Color color, float size, float lifetime, int order)
    {
        ParticleSystem ps = Particles(parent, "Ring", ring, order);
        var main = ps.main;
        main.startLifetime = lifetime;
        main.startSize = size;
        main.startColor = color;
        Burst(ps, 1);
        Sheet(ps, 6, 0f, 0f);
        FadeOut(ps, 0.7f);
        return ps;
    }

    // ---------- ayudas de particulas ----------

    static ParticleSystem Particles(Transform parent, string name, Material material, int order)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.duration = 1f;
        main.startLifetime = 0.4f;
        main.startSpeed = 0f;
        main.startSize = 16f / PPU;
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    static void Burst(ParticleSystem ps, int count)
    {
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
    }

    static void Circle(ParticleSystem ps, float radius)
    {
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
    }

    // velocidad aleatoria entre min y max que se va frenando
    static void VelocityDecay(ParticleSystem ps, Vector2 min, Vector2 max)
    {
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(1f, Decay(min.x), Decay(max.x));
        velocity.y = new ParticleSystem.MinMaxCurve(1f, Decay(min.y), Decay(max.y));
        velocity.z = new ParticleSystem.MinMaxCurve(1f, Decay(0f), Decay(0f));
    }

    static AnimationCurve Decay(float value)
    {
        return new AnimationCurve(new Keyframe(0f, value), new Keyframe(0.3f, value * 0.3f), new Keyframe(1f, value * 0.05f));
    }

    static void Drag(ParticleSystem ps, float drag)
    {
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 100f;
        limit.drag = drag;
    }

    static void Sheet(ParticleSystem ps, int tiles, float startFrameMin, float startFrameMax)
    {
        var sheet = ps.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = tiles;
        sheet.numTilesY = 1;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        // termina justo antes del ultimo fotograma para no volver a empezar la animacion
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 0.99f - startFrameMax));
        sheet.startFrame = new ParticleSystem.MinMaxCurve(startFrameMin, startFrameMax);
    }

    static void FadeOut(ParticleSystem ps, float fadeStart)
    {
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeStart), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
    }

    static ParticleSystem.MinMaxGradient RandomColor(Color a, Color b, Color c)
    {
        var gradient = new Gradient();
        gradient.mode = GradientMode.Fixed;
        gradient.SetKeys(
            new[] { new GradientColorKey(a, 0.33f), new GradientColorKey(b, 0.66f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
    }

    static Gradient Gradient2(Color start, Color middle, Color end)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(middle, 0.5f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(middle.a, 0.5f), new GradientAlphaKey(end.a, 1f) });
        return gradient;
    }

    static void AddAutoDestroy(GameObject go, float lifetime)
    {
        var autoDestroy = go.AddComponent<AutoDestroy>();
        var so = new SerializedObject(autoDestroy);
        so.FindProperty("lifetime").floatValue = lifetime;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- texturas pixel art ----------

    static Texture2D NewTexture(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.SetPixels32(new Color32[width * height]);
        return tex;
    }

    static Texture2D Solid(int width, int height)
    {
        var tex = NewTexture(width, height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, Color.white);
        return tex;
    }

    // 4 bolitas de humo de 16x16 que se van encogiendo
    static Texture2D PuffSheet()
    {
        float[] radius = { 7.2f, 6f, 4.4f, 2.6f };
        var tex = NewTexture(64, 16);
        var shade = new Color(0.8f, 0.72f, 0.68f, 1f);
        for (int f = 0; f < 4; f++)
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float dx = x + 0.5f - 8f, dy = y + 0.5f - 8f;
                    if (dx * dx + dy * dy > radius[f] * radius[f]) continue;
                    bool lowerRight = dx - dy > radius[f] * 0.9f;
                    tex.SetPixel(f * 16 + x, y, lowerRight ? shade : Color.white);
                }
        return tex;
    }

    // 4 chispas de 9x9: cruz grande, mediana, pequena y punto
    static Texture2D SparkSheet()
    {
        int[] arm = { 4, 3, 2, 0 };
        var tex = NewTexture(36, 9);
        for (int f = 0; f < 4; f++)
        {
            int cx = f * 9 + 4, cy = 4;
            tex.SetPixel(cx, cy, Color.white);
            for (int i = 1; i <= arm[f]; i++)
            {
                tex.SetPixel(cx + i, cy, Color.white);
                tex.SetPixel(cx - i, cy, Color.white);
                tex.SetPixel(cx, cy + i, Color.white);
                tex.SetPixel(cx, cy - i, Color.white);
            }
        }
        return tex;
    }

    // 6 anillos de 64x64 que se expanden
    static Texture2D RingSheet()
    {
        var tex = NewTexture(384, 64);
        for (int f = 0; f < 6; f++)
        {
            float r = 6f + f * 4.8f;
            float thickness = f < 3 ? 2.6f : 1.6f;
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = x + 0.5f - 32f, dy = y + 0.5f - 32f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (Mathf.Abs(d - r) <= thickness / 2f) tex.SetPixel(f * 64 + x, y, Color.white);
                }
        }
        return tex;
    }

    static Texture2D OrbTexture()
    {
        var tex = NewTexture(12, 12);
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 12; x++)
            {
                float dx = x + 0.5f - 6f, dy = y + 0.5f - 6f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= 2.4f) tex.SetPixel(x, y, Color.white);
                else if (d <= 6f) tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.55f));
            }
        return tex;
    }

    // Escarcha de 96x96 guardada como datos para el shader:
    // R = tono, G = momento en que aparece (0 centro, 1 puntas), B = destello, A = forma
    static Texture2D FrostTexture()
    {
        const int size = 96;
        float c = size / 2f;
        var rng = new System.Random(7);
        var mask = new bool[size, size];
        var tone = new float[size, size];

        // mancha central irregular
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dy, dx);
                float r = 17f + 4.4f * Mathf.Sin(3f * a + 1f) + 2.6f * Mathf.Sin(5f * a + 2f);
                if (d <= r)
                {
                    mask[x, y] = true;
                    tone[x, y] = 0.45f + 0.4f * (1f - d / r);
                }
            }

        // brazos de cristal con ramitas
        const int arms = 7;
        for (int k = 0; k < arms; k++)
        {
            float angle = k * Mathf.PI * 2f / arms + ((float)rng.NextDouble() - 0.5f) * 0.5f;
            float length = 26f + (float)rng.NextDouble() * 18f;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            for (float s = 0f; s <= length; s += 0.5f)
            {
                float width = Mathf.Lerp(2.8f, 0.8f, s / length);
                Stamp(mask, tone, new Vector2(c, c) + dir * s, width, 0.95f - 0.35f * (s / length));
            }
            float branchAt = length * (0.45f + (float)rng.NextDouble() * 0.2f);
            for (int side = -1; side <= 1; side += 2)
            {
                var branchDir = new Vector2(Mathf.Cos(angle + side * 0.7f), Mathf.Sin(angle + side * 0.7f));
                float branchLength = 6f + (float)rng.NextDouble() * 4f;
                for (float s = 0f; s <= branchLength; s += 0.5f)
                    Stamp(mask, tone, new Vector2(c, c) + dir * branchAt + branchDir * s, 0.8f, 0.75f);
            }
        }

        float maxDist = 0f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (mask[x, y]) maxDist = Mathf.Max(maxDist, Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)));

        var tex = NewTexture(size, size);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (!mask[x, y]) continue;
                bool outline = x == 0 || y == 0 || x == size - 1 || y == size - 1
                    || !mask[x - 1, y] || !mask[x + 1, y] || !mask[x, y - 1] || !mask[x, y + 1];
                float shade = outline ? 0.1f : tone[x, y];
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                float appear = Mathf.Clamp(d / maxDist + ((float)rng.NextDouble() - 0.5f) * 0.08f, 0.01f, 1f);
                float sparkle = !outline && rng.NextDouble() < 0.06 ? 1f : 0f;
                tex.SetPixel(x, y, new Color(shade, appear, sparkle, 1f));
            }
        return tex;
    }

    static void Stamp(bool[,] mask, float[,] tone, Vector2 center, float radius, float value)
    {
        int size = mask.GetLength(0);
        int r = Mathf.CeilToInt(radius) + 1;
        for (int y = (int)center.y - r; y <= (int)center.y + r; y++)
            for (int x = (int)center.x - r; x <= (int)center.x + r; x++)
            {
                if (x < 0 || y < 0 || x >= size || y >= size) continue;
                if (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) > Mathf.Max(radius, 0.5f)) continue;
                mask[x, y] = true;
                tone[x, y] = Mathf.Max(tone[x, y], value);
            }
    }

    // ---------- guardar assets ----------

    static Sprite SaveTexture(Texture2D tex, string name, bool isData)
    {
        string path = $"{Root}/Textures/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.sRGBTexture = !isData;
        importer.alphaIsTransparency = !isData;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Material SaveMaterial(string name, Shader shader, Texture texture)
    {
        string path = $"{Root}/Materials/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        if (texture != null) material.mainTexture = texture;
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Prefabs/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
}
