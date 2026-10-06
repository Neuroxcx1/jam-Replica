using UnityEngine;
using UnityEngine.Rendering.Universal;

// Puerta de seguridad: la abre un computador de seguridad (SecurityTerminal) mientras tenga algo delante.
// La hoja sube y se mete en el techo; la luz de encima pasa de rojo a verde.
public class SecurityDoor : MonoBehaviour
{
    [SerializeField] Transform panel;
    [SerializeField] Collider2D blocker;
    [SerializeField] Light2D lamp;
    [Tooltip("Alto de la puerta (lo que sube la hoja al abrirse)")]
    [SerializeField] float height = 2f;
    [Tooltip("Velocidad de la hoja")]
    [SerializeField] float speed = 8f;
    [SerializeField] Color closedColor = new Color(1f, 0.25f, 0.2f);
    [SerializeField] Color openColor = new Color(0.4f, 1f, 0.55f);

    bool open;
    float amount;

    public void SetOpen(bool value) => open = value;

    void FixedUpdate()
    {
        amount = Mathf.MoveTowards(amount, open ? 1f : 0f, speed / height * Time.fixedDeltaTime);
        panel.localPosition = new Vector3(0f, amount * height, 0f);
        blocker.enabled = amount < 0.95f;
        lamp.color = open ? openColor : closedColor;
    }
}
