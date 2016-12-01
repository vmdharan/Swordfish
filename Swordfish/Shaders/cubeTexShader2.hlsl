// cubeTexShader.hlsl

struct VS_INPUT
{
	float4 pos : POSITION;
	float4 norm : NORMAL;
	float2 tuv : TEXCOORD;
	float4 col : COLOR;
};

struct PS_INPUT
{
	float4 pos : SV_POSITION;
	float4 norm : NORMAL;
	float2 tuv : TEXCOORD;
	float4 col : COLOR;
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

Texture2D textureView : register(t0);
SamplerState colorSampler : register(s0);

PS_INPUT VSMain(VS_INPUT input)
{
	PS_INPUT output = (PS_INPUT)0;

	output.pos = mul(input.pos, wvp);
	output.norm = input.norm;
	output.tuv = input.tuv;
	output.col = input.col;

	output.norm = normalize(output.norm);

	return output;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
	float4 finalCol = (float4) 0;
	float3 lightDir = (float3) vLightDir;
	//float3 lightDir = (float3) vLightDir - (float3) input.pos;
	float lightIntensity = 0.0f;
	float4 texCol = textureView.Sample(colorSampler, input.tuv);
	
	//float d = length(lightDir);
	//lightDir /= d;
	
	
	lightDir = mul(lightDir, World);

	finalCol = input.col / 1.67f;
	lightIntensity = (dot((float3)input.norm, lightDir) * vLightCol);
	finalCol += saturate(input.col * lightIntensity);
	finalCol.a = 1.0;
	
	return finalCol * texCol;
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