// Liquidos en pixeles para el Shader Graph "Liquid": el agua con corriente y el acido del nivel tutorial.
// LiquidSurface le pasa la altura de la superficie (_SurfaceY), donde acaba el sprite (_Bounds: min x, min y, max x,
// max y), sus bordes (_Edges) y donde le caen chorros (_Splash). Todo va en coordenadas del mundo: al estirar
// el sprite no se repite nada.
// _Fall = 1 es un chorro que cae (vetas que bajan, sin olas). _Electric: la corriente. _BubbleAmount: las burbujas.

float LiquidHash(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

void Liquid_float(float2 UV, float4 VertexColor, float3 PositionWS, out float3 Color, out float Alpha)
{
    float ppu = _PixelsPerUnit;
    float2 px = floor(PositionWS.xy * ppu);
    float t = _Time.y;
    bool fall = _Fall > 0.5;
    float left = floor(_Bounds.x * ppu), bottom = floor(_Bounds.y * ppu), right = floor(_Bounds.z * ppu) - 1.0;

    // superficie: dos senos lentos redondeados a pixeles enteros
    float wave = fall ? 0.0 : sin(px.x * 0.13 + t * _WaveSpeed) + 0.5 * sin(px.x * 0.31 - t * _WaveSpeed * 1.4);
    float surface = floor(_SurfaceY * ppu) + round(wave * _WaveAmount * 0.5);
    bool foam = false;

    if (fall)
    {
        // si se derrama de un charco, arriba hace una curva: lleno del lado del que viene y cayendo hacia el otro
        if (abs(_Edges.y) > 0.5)
        {
            float across = _Edges.y < 0.0 ? px.x - left : right - px.x;
            float u = saturate(across / max(right - left, 1.0));
            surface -= round(u * u * 6.0);
        }
    }
    else
    {
        // donde cae un chorro: la superficie borbotea con espuma y salen ondas hacia los lados
        for (int i = 0; i < 2; i++)
        {
            float width = (i == 0 ? _Splash.y : _Splash.w) * ppu;
            if (width <= 0.0) continue;
            float dx = abs(px.x - floor((i == 0 ? _Splash.x : _Splash.z) * ppu));
            if (dx < width + 3.0)
            {
                surface += round(1.5 + sin(t * 18.0 + px.x * 0.9) * 1.2);
                foam = true;
            }
            else if (dx < width + 40.0 && frac((dx - t * 12.0) / 14.0) < 0.12) surface += 1.0;
        }

        // hacia los bordes libres baja poco a poco hasta quedar en una pelicula de 1 pixel, como el agua derramada
        // de verdad (contra una pared o donde se derrama en un chorro llega llena). La bajada ocupa hasta _Taper
        // pixeles y nunca mas de un tercio del charco; con smoothstep casi no cambia en el borde ni en el centro
        float fromLeft = _Edges.x > 0.5 ? 1e5 : px.x - left + 0.5;
        float fromRight = _Edges.y > 0.5 ? 1e5 : right - px.x + 0.5;
        float slope = clamp((right - left + 1.0) / 3.0, 4.0, _Taper);
        float edge = smoothstep(0.0, 1.0, saturate(min(fromLeft, fromRight) / slope));
        float full = surface - bottom;
        surface = min(surface, bottom + max(1.0, round(full * edge)));
    }

    float depth = surface - px.y;
    if (depth < 0.0)
    {
        Color = float3(0, 0, 0);
        Alpha = 0.0;
        return;
    }

    // mas oscuro y mas opaco cuanto mas hondo
    float k = saturate(depth / (ppu * _Depth));
    float3 c = lerp(_ColorSurface.rgb, _ColorDeep.rgb, k);
    float a = lerp(_OpacityTop, _OpacityDeep, k);

    if (fall)
    {
        // vetas claras que bajan, cada columna a su ritmo
        float seed = LiquidHash(float2(px.x, 3.0));
        float flow = px.y + t * _FallSpeed * ppu;
        float v = frac(flow / (8.0 + seed * 12.0) + seed);
        if (v < 0.3) c = lerp(c, _ColorFoam.rgb, 0.3 + 0.3 * seed);

        float aboveBottom = px.y - bottom;
        if (_Edges.x > 0.5)
        {
            // cae sobre algo: espuma en los ultimos pixeles
            if (aboveBottom < 4.0 && LiquidHash(px + floor(t * 15.0)) < 0.6) c = _ColorFoam.rgb;
        }
        else if (aboveBottom < 12.0)
        {
            // cae al vacio: se va deshaciendo en gotas que bajan con el agua
            if (LiquidHash(float2(px.x, floor(flow / 2.0))) < 1.0 - aboveBottom / 12.0)
            {
                Color = float3(0, 0, 0);
                Alpha = 0.0;
                return;
            }
        }
    }
    else
    {
        // reflejos: rayitas claras que van y vienen despacio
        float x = px.x + 64.0 + round(sin(t * 0.8 + floor(depth / 5.0)) * 3.0);
        float h = LiquidHash(float2(floor(x / 10.0), floor(depth / 5.0)));
        if (h < 0.3 && depth > 3.0 && fmod(depth, 5.0) < 1.0 && fmod(x, 10.0) < 3.0) c = lerp(c, _ColorFoam.rgb, 0.45);

        // espuma donde cae el chorro
        if (foam && depth < 5.0 && LiquidHash(px + floor(t * 15.0)) < 0.55) c = _ColorFoam.rgb;
    }

    // burbujas que suben y revientan al llegar arriba
    float column = floor(px.x / 6.0);
    float bseed = LiquidHash(float2(column, 7.0));
    float phase = frac(t / (2.0 + bseed * 3.0) + bseed);
    float2 bubble = float2(column * 6.0 + 2.0 + floor(bseed * 3.0), floor(surface - (1.0 - phase) * ppu * 1.2));
    float2 d = abs(px - bubble);
    if (bseed < _BubbleAmount && d.x + d.y <= 1.0 && depth > 1.0) c = _ColorFoam.rgb;

    // corriente: cambia unas 14 veces por segundo
    float slot = floor(t * 14.0);
    if (fall)
    {
        // en el chorro, chispas sueltas
        if (LiquidHash(floor(px / 3.0) + slot * 1.7) < _Electric * 0.05)
        {
            c = _ColorSpark.rgb;
            a = 1.0;
        }
    }
    else
    {
        // en el charco, arcos en zigzag cerca de la superficie: cada tramo de 20 px puede tener uno
        float zone = floor(px.x / 20.0);
        if (LiquidHash(float2(zone, slot)) < _Electric)
        {
            float start = zone * 20.0 + floor(LiquidHash(float2(zone + 0.5, slot)) * 8.0);
            float len = 6.0 + floor(LiquidHash(float2(slot, zone + 0.3)) * 10.0);
            float row = 2.0 + floor(LiquidHash(float2(slot + 0.7, zone)) * _ArcDepth) + round(sin(px.x * 1.3 + slot * 2.1));
            if (px.x >= start && px.x < start + len && abs(depth - row) < 0.5)
            {
                c = _ColorSpark.rgb;
                a = 1.0;
            }
        }
    }

    // borde de arriba: una linea clara
    if (depth < 1.0)
    {
        c = _ColorFoam.rgb;
        a = max(a, 0.9);
    }

    Color = c * VertexColor.rgb;
    Alpha = a * VertexColor.a;
}
