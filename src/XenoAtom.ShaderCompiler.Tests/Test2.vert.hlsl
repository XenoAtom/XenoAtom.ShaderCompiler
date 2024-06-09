#pragma shader_stage(vertex)
#include "hello_world.hlsl"
float4 main(float2 pos : POSITION) : SV_POSITION
{
    return float4(pos, 0, 2.0);
}