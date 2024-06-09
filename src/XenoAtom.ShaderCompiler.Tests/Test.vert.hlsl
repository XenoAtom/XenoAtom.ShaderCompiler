#pragma shader_stage(vertex)
#include "hello_world.hlsl"
#include "include2.hlsl"
float4 main(float2 pos : POSITION) : SV_POSITION
{
    return float4(pos, 0, TESTER_VALUE + INCLUDE_VALUE);
}