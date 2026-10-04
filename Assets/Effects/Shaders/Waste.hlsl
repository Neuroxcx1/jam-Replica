// Desechos toxicos para el Shader Graph "Waste": olas en pixeles, burbujas que suben y vetas lentas.
// _SurfaceY lo pone WastePool con la altura real de la superficie (en unidades del mundo).
void Waste_float(float2 UV, float4 VertexColor, float3 PositionWS, out float3 Color, out float Alpha)
{
    float ppu = _PixelsPerUnit;
    float2 px = floor(PositionWS.xy * ppu);
    float t = _Time.y;

    // superficie: dos senos lentos redondeados a pixeles enteros
    float wave = sin(px.x * 0.11 + t * _WaveSpeed) + 0.6 * sin(px.x * 0.27 - t * _WaveSpeed * 1.3);
    float surface = floor(_SurfaceY * ppu) + round(wave * _WaveAmount * 0.5);
    float depth = surface - px.y;
    if (depth < 0.0)
    {
        Color = float3(0, 0, 0);
        Alpha = 0.0;
        return;
    }

    float3 c = lerp(_ColorMid.rgb, _ColorDeep.rgb, saturate(depth / (ppu * 1.5)));

    // vetas horizontales que se mueven despacio
    float streak = sin(px.y * 0.9 + sin(px.x * 0.05 + t * 0.6) * 2.0);
    if (streak > 0.93 && depth > 3.0) c = lerp(c, _ColorMid.rgb, 0.55);

    // burbujas: cada columna de 6 px puede tener una que sube y revienta al llegar arriba
    float column = floor(px.x / 6.0);
    float seed = frac(sin(column * 12.9898) * 43758.5453);
    float phase = frac(t / (2.0 + seed * 3.0) + seed);
    float2 bubble = float2(column * 6.0 + 2.0 + floor(seed * 3.0), floor(surface - (1.0 - phase) * ppu * 1.2));
    float2 d = abs(px - bubble);
    if (seed < _BubbleAmount && d.x + d.y <= 1.0 && depth > 1.0) c = _ColorSurface.rgb;

    // borde de arriba: espuma clara y una franja mas viva debajo
    if (depth < 1.0) c = _ColorFoam.rgb;
    else if (depth < 3.0) c = _ColorSurface.rgb;

    Color = c * VertexColor.rgb;
    Alpha = VertexColor.a * _Opacity;
}
