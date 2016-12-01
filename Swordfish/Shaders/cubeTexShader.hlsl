// cubeTexShader.hlsl

struct VS_INPUT
{
	float4 pos : POSITION;
	float2 uv : TEXCOORD;
};

struct PS_INPUT
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD;
};

float4x4 wvp;

Texture2D textureView : register(t0);
SamplerState colorSampler : register(s0);

PS_INPUT VSMain(VS_INPUT input)
{
	PS_INPUT output = (PS_INPUT)0;

	output.pos = mul(input.pos, wvp);
	output.uv = input.uv;

	return output;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
	return textureView.Sample(colorSampler, input.uv);
}


technique10 Render
{
	pass P0
	{
		SetGeometryShader(0);
		SetVertexShader(CompileShader(vs_5_0, VSMain()));
		SetPixelShader(CompileShader(ps_5_0, PSMain()));
	}
}