using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Paso a otra escena (de la sala del tanque al laboratorio): al cruzar se funde a negro, carga la escena
// y vuelve a aclararse al otro lado. Necesita un collider trigger.
public class SceneDoor : MonoBehaviour
{
    [SerializeField] string scene = "Laboratorio";
    [SerializeField] float fadeTime = 0.7f;

    float fade;
    bool crossing;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (crossing || !other.TryGetComponent(out Player _)) return;
        crossing = true;
        GetComponent<Collider2D>().enabled = false;
        // sobrevive al cambio de escena para aclarar la pantalla al otro lado
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        StartCoroutine(Cross());
    }

    IEnumerator Cross()
    {
        for (; fade < 1f; fade += Time.deltaTime / fadeTime) yield return null;
        fade = 1f;
        yield return SceneManager.LoadSceneAsync(scene);
        for (; fade > 0f; fade -= Time.deltaTime / fadeTime) yield return null;
        Destroy(gameObject);
    }

    void OnGUI()
    {
        if (!crossing) return;
        GUI.color = new Color(0f, 0f, 0f, fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
    }
}
