using UnityEngine;
using UnityEngine.UI;

// Recordatorio de un control en el HUD: la tecla, o el boton del mando si se esta jugando con el (cambia solo).
// Son los de las replicas, debajo del contador, con su texto al lado.
public class ControlHint : MonoBehaviour
{
    [SerializeField] ControlIcons icons;
    [SerializeField] Sign.Control control;
    [SerializeField] Image icon;
    [Tooltip("Lo que mide en el HUD cada pixel del icono")]
    [SerializeField] float pixelSize = 1f;

    Sprite shown;

    void Update()
    {
        ControlIcons.CheckDevice();
        var sprites = icons.For(control);
        Sprite sprite = sprites.Count > 0 ? sprites[0] : null;
        if (sprite == shown) return;
        shown = sprite;

        icon.enabled = sprite != null;
        if (sprite == null) return;
        icon.sprite = sprite;
        icon.rectTransform.sizeDelta = sprite.rect.size * pixelSize;
    }
}
