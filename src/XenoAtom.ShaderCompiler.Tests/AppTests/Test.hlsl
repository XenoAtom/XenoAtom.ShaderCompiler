float4 VSMain(float4 position : POSITION) : SV_POSITION
{
    return position + DEFAULT_VALUE;
}
