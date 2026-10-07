using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// En una opcion de menu: al pasar el raton la elige (igual que con el teclado o el mando, y suena al cambiar)
// y al pulsarla suena Select, o Back si es de volver atras. Los sonidos estan en GameMenus.
public class MenuSound : MonoBehaviour, IPointerEnterHandler, ISubmitHandler, IPointerClickHandler
{
    [Tooltip("Marcalo en las que vuelven atras (Volver, Continuar)")]
    [SerializeField] bool back;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current != null && GetComponent<Selectable>().interactable)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSubmit(BaseEventData eventData) => Play();

    public void OnPointerClick(PointerEventData eventData) => Play();

    void Play()
    {
        if (GameMenus.Instance != null) GameMenus.Instance.PlayUi(back ? GameMenus.UiSound.Back : GameMenus.UiSound.Select);
    }
}
