#include "shared_include1.hlsl"
#include <SubFolder/shared_include2.hlsl>
float4 main(float4 position : POSITION) : SV_POSITION
{
    return position + DEFAULT_VALUE + DEFAULT_VALUE_2;
}
