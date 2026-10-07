using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Paso a otra escena (de la sala del tanque al tutorial): al cruzar se funde a negro, carga la escena
// y vuelve a aclararse al otro lado. Necesita un collider trigger. La puerta final lo usa con SceneDoor.Go.
public class SceneDoor : MonoBehaviour
{
    [SerializeField] string scene = "Laboratorio";
    [SerializeField] float fadeTime = 0.7f;

    float fade;
    bool crossing;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (crossing || !other.TryGetComponent(out Player _)) return;
        if (!Go(scene, fadeTime)) return;
        crossing = true;
        GetComponent<Collider2D>().enabled = false;
    }

    public static bool Go(string scene, float fadeTime = 0.7f)
    {
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError($"No se puede cargar la escena \"{scene}\": tiene que estar en File > Build Profiles > Scene List");
            return false;
        }

        // un objeto aparte que sobrevive al cambio de escena para aclarar la pantalla al otro lado
        var fader = new GameObject("Cambio de escena").AddComponent<SceneDoor>();
        fader.scene = scene;
        fader.fadeTime = fadeTime;
        fader.crossing = true;
        DontDestroyOnLoad(fader.gameObject);
        fader.StartCoroutine(fader.Cross());
        return true;
    }

    IEnumerator Cross()
    {
        for (; fade < 1f; fade += Time.unscaledDeltaTime / fadeTime) yield return null;
        fade = 1f;
        yield return SceneManager.LoadSceneAsync(scene);
        for (; fade > 0f; fade -= Time.unscaledDeltaTime / fadeTime) yield return null;
        Destroy(gameObject);
    }

    void OnGUI()
    {
        if (!crossing) return;
        GUI.color = new Color(0f, 0f, 0f, fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
    }
}
