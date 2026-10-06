using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Un sonido hecho con varios a la vez o seguidos (el vidrio que revienta, el temblor, un grito...).
// Cada capa es un sonido con su retraso, volumen y tono; de los largos se toca solo un trozo, que se apaga poco a poco.
// Se crea con Create > Replica > Sonido combinado y suena con Play() por el grupo Output del mezclador.
[CreateAssetMenu(menuName = "Replica/Sonido combinado")]
public class SoundMix : ScriptableObject
{
    [Serializable]
    public class Layer
    {
        public AudioClip clip;
        [Tooltip("Segundos hasta que empieza")]
        public float delay;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("1 = normal; menos es mas grave y lento, mas es mas agudo y rapido")]
        public float pitch = 1f;
        [Tooltip("Desde que segundo del sonido empieza")]
        public float start;
        [Tooltip("Cuantos segundos suena (0 = hasta el final)")]
        public float length;
        [Tooltip("Lo que tarda en apagarse al final")]
        public float fadeOut = 0.2f;
    }

    [SerializeField] AudioMixerGroup output;
    [Tooltip("Para todas las capas a la vez")]
    [SerializeField, Range(0f, 1f)] float volume = 1f;
    [SerializeField] Layer[] layers;

    // suena desde el objeto de los menus, que esta en todas las escenas
    public void Play(float delay = 0f)
    {
        if (GameMenus.Instance == null) return;
        foreach (Layer layer in layers)
            if (layer.clip != null) GameMenus.Instance.StartCoroutine(PlayLayer(layer, delay));
    }

    IEnumerator PlayLayer(Layer layer, float delay)
    {
        yield return new WaitForSeconds(layer.delay + delay);
        var source = GameMenus.Instance.gameObject.AddComponent<AudioSource>();
        source.clip = layer.clip;
        source.outputAudioMixerGroup = output;
        source.volume = layer.volume * volume;
        source.pitch = layer.pitch;
        source.time = Mathf.Min(layer.start, layer.clip.length - 0.01f);
        source.Play();

        float left = layer.clip.length - source.time;
        if (layer.length > 0f) left = Mathf.Min(left, layer.length);
        left /= Mathf.Max(0.05f, layer.pitch);
        float fade = Mathf.Min(layer.fadeOut, left);
        yield return new WaitForSeconds(left - fade);

        for (float t = 0f; t < fade; t += Time.deltaTime)
        {
            source.volume = layer.volume * volume * (1f - t / fade);
            yield return null;
        }
        Destroy(source);
    }
}
