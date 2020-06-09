#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

void AdditionalLightsAngle_float(float3 WorldNormal, float3 WorldPos, out float cosAngle){
    WorldNormal = normalize(WorldNormal);
        cosAngle = 0;

    #ifndef SHADERGRAPH_PREVIEW

    int pixelLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < pixelLightCount; ++i)
    {
        Light light = GetAdditionalLight(i, WorldPos);
        cosAngle += dot(WorldNormal, light.direction);
    }
    #endif
}

void AdditionalLightsAngle_half(half3 WorldNormal, half3 WorldPos, out half cosAngle){
    WorldNormal = normalize(WorldNormal);
        cosAngle = 0;

        #ifndef SHADERGRAPH_PREVIEW

    int pixelLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < pixelLightCount; ++i)
    {
        Light light = GetAdditionalLight(i, WorldPos);
        cosAngle += dot(WorldNormal, light.direction);
    }   
     #endif

}
#endif
