using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menu del principio, encima de la sala del tanque (como en God of War): Iniciar arranca la cinematica,
// Opciones abre las del volumen (GameMenus) y Salir cierra el juego. Se maneja con raton, teclado (W/S o flechas y Enter) o mando.
public class TitleMenu : MonoBehaviour
{
    [SerializeField] IntroCinematic intro;
    [SerializeField] Button startButton;
    [SerializeField] Button optionsButton;
    [SerializeField] Button quitButton;
    [SerializeField] CanvasGroup group;
    [SerializeField] float fadeTime = 0.6f;

    void Start()
    {
        startButton.onClick.AddListener(Begin);
        quitButton.onClick.AddListener(Application.Quit);
        if (optionsButton != null) optionsButton.onClick.AddListener(Options);
        EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    // mientras estan las opciones el menu se esconde
    void Options()
    {
        group.interactable = false;
        group.alpha = 0f;
        GameMenus.Instance.OpenOptions(() =>
        {
            group.interactable = true;
            group.alpha = 1f;
            GameMenus.Instance.Select(optionsButton);
        });
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
