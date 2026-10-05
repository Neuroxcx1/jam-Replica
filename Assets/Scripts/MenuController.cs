using System.Collections;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private CanvasGroup menuGroup;

    [Header("Transition")]
    [SerializeField] private float fadeDuration = 1.5f;

    private bool starting = false;

     public void StartGame()
    {
        if (starting) return;

        starting = true;
        StartCoroutine(HideMenu());
    }

    private IEnumerator HideMenu()
    {
        // Evitar que se vuelvan a pulsar los botones.
        menuGroup.interactable = false;
        menuGroup.blocksRaycasts = false;

        float elapsed = 0f;
        float initialAlpha = menuGroup.alpha;

        // Desvanecer el título y los botones.
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            menuGroup.alpha = Mathf.Lerp(initialAlpha, 0f, progress);

            yield return null;
        }

        menuGroup.alpha = 0f;

        // Aquí conectaremos la animación de introducción.
        Debug.LogWarning("Menú oculto. Listo para iniciar la intro.");
    }
}
