#ifndef TEST_NO_INCLUDE
#include "shared_include1.hlsl"
#include <SubFolder/shared_include2.hlsl>
#else
#define DEFAULT_VALUE 2.0
#define DEFAULT_VALUE_2 3.0
#endif

float4 main(float4 position : POSITION) : SV_POSITION
{
    return position + DEFAULT_VALUE + DEFAULT_VALUE_2;
}
