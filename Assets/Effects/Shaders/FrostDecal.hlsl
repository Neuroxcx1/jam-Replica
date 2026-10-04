// Lo usa FrostDecal.shadergraph (nodo Custom Function).
// Escarcha que crece desde el centro. La textura guarda datos:
// R = tono, G = cuando aparece cada pixel (0 centro, 1 puntas), B = destellos, A = forma.
#ifndef FROST_DECAL_INCLUDED
#define FROST_DECAL_INCLUDED

void FrostDecal_float(float2 UV, float4 VertexColor, float3 PositionWS, out float3 Color, out float Alpha)
{
    float4 data = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UV);
    float visible = step(0.5, data.a) * step(data.g, _Progress);

    float3 c = lerp(_ColorDark.rgb, _ColorLight.rgb, data.r);

    // el borde que acaba de crecer brilla un momento
    float fresh = saturate(1.0 - (_Progress - data.g) * 8.0);
    c = lerp(c, float3(1, 1, 1), fresh * 0.7);

    // destellos que se encienden y apagan
    float2 cell = floor(UV * _MainTex_TexelSize.zw);
    float h = frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453);
    float twinkle = step(0.5, data.b) * step(0.8, frac(h + _Time.y * 1.3));
    c = lerp(c, float3(1, 1, 1), twinkle);

    Color = c;
    Alpha = visible * _Opacity * VertexColor.a;
}

#endif
