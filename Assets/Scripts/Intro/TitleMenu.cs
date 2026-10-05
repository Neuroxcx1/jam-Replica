using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menu del principio, encima de la sala del tanque (como en God of War): Iniciar arranca la cinematica
// y Salir cierra el juego. Se maneja con raton, teclado (W/S o flechas y Enter) o mando.
public class TitleMenu : MonoBehaviour
{
    [SerializeField] IntroCinematic intro;
    [SerializeField] Button startButton;
    [SerializeField] Button quitButton;
    [SerializeField] CanvasGroup group;
    [SerializeField] float fadeTime = 0.6f;

    void Start()
    {
        startButton.onClick.AddListener(Begin);
        quitButton.onClick.AddListener(Application.Quit);
        EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    void Begin()
    {
        group.interactable = false;
        intro.Begin();
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            group.alpha = 1f - t / fadeTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
