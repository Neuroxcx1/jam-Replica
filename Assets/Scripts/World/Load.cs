using UnityEngine;

// Lo que pesa encima de una superficie: el jugador, las replicas y los cuerpos.
// Los cuerpos congelados no cuentan: estan en estasis, flotan quietos y no pesan.
public static class Load
{
    public static int On(Collider2D surface)
    {
        Bounds b = surface.bounds;
        var top = new Vector2(b.center.x, b.max.y + 0.08f);
        int load = 0;
        foreach (Collider2D c in Physics2D.OverlapBoxAll(top, new Vector2(b.size.x - 0.05f, 0.12f), 0f))
            if (Weighs(c)) load++;
        return load;
    }

    static bool Weighs(Collider2D c)
    {
        if (c.TryGetComponent(out Player _) || c.TryGetComponent(out Clone _)) return true;
        return c.TryGetComponent(out Body _) && c.attachedRigidbody.bodyType != RigidbodyType2D.Static;
    }
}
