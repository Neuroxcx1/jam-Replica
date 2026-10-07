using UnityEngine;

// Para lo que va clavado en una pared, el techo o el suelo (el vapor, el laser): al soltarlo en el editor cerca
// de una superficie se pega a ella y apunta hacia fuera. No hay que girarlo a mano.
[ExecuteAlways]
public class SurfaceMount : MonoBehaviour
{
    [Tooltip("A que distancia de la superficie se queda (donde empieza el chorro o el rayo)")]
    [SerializeField] float offset = 0.35f;
    [Tooltip("Hasta donde busca una superficie alrededor (en casillas)")]
    [SerializeField] float reach = 1.5f;
    [Tooltip("Desmarcalo si lo quieres colocar y girar a mano")]
    [SerializeField] bool snap = true;

    static readonly Vector2[] Directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    void Update()
    {
        if (Application.isPlaying || !snap || !transform.hasChanged) return;
        transform.hasChanged = false;

        // la superficie mas cercana
        RaycastHit2D best = default;
        foreach (Vector2 direction in Directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, reach + offset, LayerMask.GetMask("Ground"));
            if (hit && hit.distance > 0f && (!best || hit.distance < best.distance)) best = hit;
        }
        if (!best) return;

        Vector3 position = best.point + best.normal * offset;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, best.normal);
        if ((transform.position - position).sqrMagnitude < 1e-6f && Quaternion.Angle(transform.rotation, rotation) < 0.1f) return;

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(transform, "Pegar a la superficie");
#endif
        transform.SetPositionAndRotation(position, rotation);
#if UNITY_EDITOR
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
#endif
        transform.hasChanged = false;
    }
}
