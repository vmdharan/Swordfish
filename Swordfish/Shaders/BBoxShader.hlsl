// BBoxShader.hlsl

struct VS_INPUT
{
	float4 pos : POSITION;
};

struct PS_INPUT
{
	float4 pos : SV_POSITION;
};

cbuffer ConstantBuffer : register(b0)
{
	float4x4 wvp;
	float4x4 World;
	float4 View;
	float4 Proj;
	float4 vLightDir;
	float4 vLightCol;
	float4 vLightCol2;
};


PS_INPUT VSMain(VS_INPUT input)
{
	PS_INPUT output = (PS_INPUT) 0;

	output.pos = mul(input.pos, wvp);

	return output;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
	float4 finalCol = (float4) 0;

	finalCol = vLightCol / 2.0f;
	finalCol += vLightCol2;

	return finalCol;
}



technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, VSMain()));
		SetPixelShader(CompileShader(ps_5_0, PSMain()));
	}
}