// Lo usa ReplicaSprite.shadergraph (nodo Custom Function).
// Recolorea cualquier sprite con una paleta de 3 colores segun su brillo y le anade borde,
// lineas de escaneo, brillo diagonal, parpadeo y destello. Todo se ajusta desde el material,
// asi sirve igual para el cuadrado de pruebas que para el personaje final.
#ifndef REPLICA_SPRITE_INCLUDED
#define REPLICA_SPRITE_INCLUDED

float ReplicaAlphaAt(float2 uv)
{
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
}

void ReplicaSprite_float(float2 UV, float4 VertexColor, float3 PositionWS, out float3 Color, out float Alpha)
{
    float4 src = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UV) * VertexColor;

    // paleta: oscuro / medio / claro segun el brillo, en escalones para que quede pixel art
    float steps = max(_RampSteps, 1.0);
    float lum = floor(saturate(dot(src.rgb, float3(0.299, 0.587, 0.114))) * steps + 0.5) / steps;
    float3 ramp = lum < 0.5
        ? lerp(_ColorDark.rgb, _ColorMid.rgb, lum * 2.0)
        : lerp(_ColorMid.rgb, _ColorLight.rgb, (lum - 0.5) * 2.0);
    float3 col = lerp(src.rgb, ramp, _RampAmount);

    // cuanto mide 1 pixel del juego en UV (funciona con cualquier sprite)
    float2 uvPerWorld = float2(abs(ddx(UV.x)) / max(abs(ddx(PositionWS.x)), 1e-5),
                               abs(ddy(UV.y)) / max(abs(ddy(PositionWS.y)), 1e-5));
    float2 px = uvPerWorld / _PixelsPerUnit;

    // borde de 1 pixel: algun vecino transparente o el borde del sprite
    float neighbours = ReplicaAlphaAt(UV + float2(px.x, 0)) * ReplicaAlphaAt(UV - float2(px.x, 0))
                     * ReplicaAlphaAt(UV + float2(0, px.y)) * ReplicaAlphaAt(UV - float2(0, px.y));
    bool edge = neighbours < 0.5 || UV.x < px.x || UV.x > 1 - px.x || UV.y < px.y || UV.y > 1 - px.y;
    if (edge) col = lerp(col, _EdgeColor.rgb, _EdgeAmount);

    float2 pixel = floor(PositionWS.xy * _PixelsPerUnit);

    // lineas de escaneo que bajan: 1 de cada 3 filas de pixeles
    float row = pixel.y + floor(_Time.y * _ScanSpeed);
    if (fmod(abs(row), 3.0) < 1.0) col = lerp(col, _ScanColor.rgb, _ScanAmount);

    // brillo diagonal de 2 pixeles que pasa cada cierto tiempo
    float d = fmod(pixel.x + pixel.y - _Time.y * _ShineSpeed, _ShinePeriod);
    if (d < 0) d += _ShinePeriod;
    if (d < 2.0) col = lerp(col, float3(1, 1, 1), _ShineAmount);

    float alpha = src.a * _Opacity;
    alpha *= 1.0 - _FlickerAmount * (0.5 + 0.5 * sin(_Time.y * 25.0 + pixel.y * 0.7));

    // destello blanco (lo mueven SpriteFlash y BodyEffect)
    col = lerp(col, float3(1, 1, 1), _Flash);
    alpha = lerp(alpha, src.a, _Flash);

    Color = col;
    Alpha = alpha;
}

#endif
