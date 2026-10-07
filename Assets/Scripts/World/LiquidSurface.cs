using System.Collections.Generic;
using UnityEngine;

// Para los liquidos con el shader "Liquid" (agua y acido). Le pasa al shader:
// - la altura de la superficie (un poco por debajo del borde de arriba, para que quepan las olas) y donde acaba el sprite
// - en un charco: por que lados no tiene que ir bajando en la punta (toca una pared o se derrama en un chorro)
//   y donde le cae un chorro encima (ahi salpica)
// - en un chorro: si cae sobre algo (hace espuma) o se deshace en gotas, y de que lado viene si se derrama de un charco
// Tambien en el editor, para verlo al colocarlo, y cada fotograma, para seguir a lo que se mueva.
[ExecuteAlways]
public class LiquidSurface : MonoBehaviour
{
    [Tooltip("La superficie va esto por debajo del borde de arriba (para que quepan las olas)")]
    [SerializeField] float below = 0.125f;
    [Tooltip("Marcado: es un chorro que cae. Desmarcado: es un charco o un tanque")]
    [SerializeField] bool falls;

    // lo que se considera "tocar" (en unidades)
    const float Near = 0.3f;

    static readonly List<LiquidSurface> all = new List<LiquidSurface>();
    static readonly int SurfaceId = Shader.PropertyToID("_SurfaceY");
    static readonly int BoundsId = Shader.PropertyToID("_Bounds");
    static readonly int EdgesId = Shader.PropertyToID("_Edges");
    static readonly int SplashId = Shader.PropertyToID("_Splash");

    SpriteRenderer sprite;
    MaterialPropertyBlock block;

    Bounds Area => sprite.bounds;
    float Surface => sprite.bounds.max.y - below;

    void OnEnable()
    {
        sprite = GetComponent<SpriteRenderer>();
        all.Add(this);
    }

    void OnDisable() => all.Remove(this);

    // se recalcula sola al moverla o estirarla; esto lo fuerza en todas (menu Replica > Recalcular agua)
    [ContextMenu("Recalcular agua")]
    public void RecalculateAll()
    {
        foreach (LiquidSurface liquid in all) liquid.Update();
    }

    void Update()
    {
        if (block == null) block = new MaterialPropertyBlock();
        Bounds b = Area;
        sprite.GetPropertyBlock(block);
        block.SetFloat(SurfaceId, Surface);
        block.SetVector(BoundsId, new Vector4(b.min.x, b.min.y, b.max.x, b.max.y));
        block.SetVector(EdgesId, falls ? StreamEdges(b) : PoolEdges(b));
        if (!falls) block.SetVector(SplashId, Splashes(b));
        sprite.SetPropertyBlock(block);
    }

    // x, y: el lado izquierdo / derecho llega lleno hasta el borde (1) o baja poco a poco como una gota (0)
    Vector4 PoolEdges(Bounds b)
    {
        bool left = Wall(b.min.x - 0.1f), right = Wall(b.max.x + 0.1f);
        foreach (LiquidSurface other in all)
        {
            if (!other.falls || Mathf.Abs(other.Area.max.y - Surface) > Near) continue;
            if (Mathf.Abs(other.Area.min.x - b.max.x) < Near) right = true;
            if (Mathf.Abs(other.Area.max.x - b.min.x) < Near) left = true;
        }
        return new Vector4(left ? 1f : 0f, right ? 1f : 0f, 0f, 0f);
    }

    bool Wall(float x) => Physics2D.OverlapPoint(new Vector2(x, Surface - 0.1f), LayerMask.GetMask("Ground")) != null;

    // hasta dos chorros que caen en este charco: x y medio ancho de cada uno (0 = ninguno)
    Vector4 Splashes(Bounds b)
    {
        var splash = Vector4.zero;
        int count = 0;
        foreach (LiquidSurface other in all)
        {
            Bounds s = other.Area;
            if (!other.falls || count == 2 || Mathf.Abs(s.min.y - Surface) > Near) continue;
            if (s.center.x < b.min.x || s.center.x > b.max.x) continue;
            splash[count * 2] = s.center.x;
            splash[count * 2 + 1] = s.extents.x;
            count++;
        }
        return splash;
    }

    // x: cae sobre algo (1) o se deshace en gotas (0); y: viene de un charco por la izquierda (-1), la derecha (1) o no (0)
    Vector4 StreamEdges(Bounds b)
    {
        bool lands = Physics2D.OverlapPoint(new Vector2(b.center.x, b.min.y - 0.1f), LayerMask.GetMask("Ground")) != null;
        float from = 0f;
        foreach (LiquidSurface other in all)
        {
            if (other.falls) continue;
            Bounds p = other.Area;
            if (Mathf.Abs(other.Surface - b.min.y) < Near && b.center.x > p.min.x && b.center.x < p.max.x) lands = true;
            if (Mathf.Abs(other.Surface - b.max.y) > Near) continue;
            if (Mathf.Abs(p.max.x - b.min.x) < Near) from = -1f;
            if (Mathf.Abs(p.min.x - b.max.x) < Near) from = 1f;
        }
        return new Vector4(lands ? 1f : 0f, from, 0f, 0f);
    }
}
