// Basic HLSL vertex and pixel shader
float4 VSMain(float4 position : POSITION) : SV_POSITION
{
    return position;
}

float4 PSMain(float4 position : SV_POSITION) : SV_TARGET
{
    return float4(1.0, 0.0, 0.0, 1.0);
} 
