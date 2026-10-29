//#include "Assets/Art/Shaders/VoxelShaderConstants.hlsl"


// MESHING

float2 flipbook(float2 UV, float Width, float Height, float Tile, float2 Invert)
{
    Tile = fmod(Tile, Width * Height);
    float2 tileCount = float2(1.0, 1.0) / float2(Width, Height);
    float tileY = abs(Invert.y * Height - (floor(Tile * tileCount.x) + Invert.y * 1));
    float tileX = abs(Invert.x * Width - ((Tile - Width * floor(Tile * tileCount.x)) + Invert.x * 1));
    return (UV + float2(tileX, tileY)) * tileCount;
}

int getEdgeTile(int f, uint solidFaces) {
    int edgeTile = 0;
    edgeTile += 1 * (solidFaces >> EdgeIndices[f][0] & 1);
    edgeTile += 2 * (solidFaces >> EdgeIndices[f][1] & 1);
    edgeTile += 4 * (solidFaces >> EdgeIndices[f][2] & 1);
    edgeTile += 8 * (solidFaces >> EdgeIndices[f][3] & 1);
    return edgeTile;

}


// LIGHTING / SHADING

half GetLight(float3 normalWS) {
    Light mainLight = GetMainLight();
    half light = saturate(dot(normalize(normalWS), mainLight.direction)) * 0.8 + 0.2;
    return light;
}

half GetShadow(float3 positionWS, float3 normalWS) {
    half cascadeIndex = ComputeCascadeIndex(positionWS);
    if (cascadeIndex >= 2) return 1.0;

    float3 steppedWS = floor((positionWS + 0.01 * normalWS) * 16) / 16;
    float4 shadowCoord = TransformWorldToShadowCoord(steppedWS);
    half shadow = MainLightRealtimeShadow(shadowCoord);
    shadow = clamp(shadow, 0.3, 1.0);

    //shadow = lerp(shadow, 1.0, (1.0 - GetMainLightShadowFade(positionWS)));

    shadow = lerp(shadow, 1.0, (cascadeIndex/4));

    return shadow;
}

half GetFog(float4 positionCS) {
    float fog = 1.0 - ComputeFogIntensity(ComputeFogFactor(positionCS.z));
    fog = clamp(pow(1.0 - clamp(positionCS.z * 40000.0, 0, 1), 6.0), 0.0, 1.0);
    return fog;
}


float3x3 AngleAxis3x3(float angle, float3 axis)
{
    float c, s;
    sincos(angle, s, c);

    float t = 1 - c;
    float x = axis.x;
    float y = axis.y;
    float z = axis.z;

    return float3x3(
        t * x * x + c,      t * x * y - s * z,  t * x * z + s * y,
        t * x * y + s * z,  t * y * y + c,      t * y * z - s * x,
        t * x * z - s * y,  t * y * z + s * x,  t * z * z + c
    );
}
