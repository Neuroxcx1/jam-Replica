using UnityEngine;

// Pega el sprite a la rejilla de pixeles del juego (32 por unidad).
// Con la Pixel Perfect Camera evita que el personaje tiemble o cambie de ancho al moverse.
// Va en el hijo que tiene el sprite (Visual); el padre se sigue moviendo con la fisica normal.
public class PixelSnap : MonoBehaviour
{
    [SerializeField] float pixelsPerUnit = 32f;

    Vector3 localOffset;

    void Awake()
    {
        localOffset = transform.localPosition;
    }

    void LateUpdate()
    {
        Vector3 p = transform.parent.TransformPoint(localOffset);
        transform.position = new Vector3(
            Mathf.Round(p.x * pixelsPerUnit) / pixelsPerUnit,
            Mathf.Round(p.y * pixelsPerUnit) / pixelsPerUnit,
            p.z);
    }
}
