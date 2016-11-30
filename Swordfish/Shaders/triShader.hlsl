// triShader.hlsl

struct VS_INPUT
{
	float4 pos : POSITION;
	float4 col : COLOR;
};

struct PS_INPUT
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
};

PS_INPUT VSMain(VS_INPUT input)
{
	PS_INPUT output = (PS_INPUT)0;

	output.pos = input.pos;
	output.col = input.col;

	return output;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
	return input.col;
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