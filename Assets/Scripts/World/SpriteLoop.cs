using UnityEngine;

// Animacion por fotogramas para trampas, puertas y decorado (sin Animator)
public class SpriteLoop : MonoBehaviour
{
    [SerializeField] Sprite[] frames;
    [SerializeField] float fps = 12f;
    [SerializeField] bool loop = true;
    [SerializeField] bool randomStart = true;

    SpriteRenderer sr;
    float time;

    public bool Finished => !loop && time * fps >= frames.Length;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // asi varias trampas iguales no se mueven a la vez
        if (randomStart && loop && frames.Length > 0) time = Random.value * frames.Length / fps;
    }

    public void Play(Sprite[] newFrames, bool loop)
    {
        frames = newFrames;
        this.loop = loop;
        time = 0f;
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        time += Time.deltaTime;
        int frame = (int)(time * fps);
        sr.sprite = frames[loop ? frame % frames.Length : Mathf.Min(frame, frames.Length - 1)];
    }
}
