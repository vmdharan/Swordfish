// TerrainShader.hlsl
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
	float4 pos2 : POSITION;
};

cbuffer ConstantBuffer : register(b0)
{
	float4x4 wvp;
	float4x4 World;
	float4x4 ModelViewIT;
	float4x4 View;
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

	output.norm = mul(input.norm, World);
	output.norm = normalize(output.norm);
	//output.norm.w = 0.0f;
	
	output.pos2 = input.pos;

	return output;
}

float4 PSMain(PS_INPUT input) : SV_TARGET
{
	float4 finalCol = (float4) 0;
	float3 lightDir = (float3) -vLightDir;
	//float3 lightDir = (float3) vLightDir - (float3) input.pos;
	float lightIntensity = 0.0f;
	float4 texCol = textureView.Sample(colorSampler, input.tuv);

	float4 WorldPos = mul(input.pos2, World);

	// Emissive term, Ke
	// Property that allows the material to emit light.
	float Ke = 0.20f;
	// Ambient term, Ka
	// Global ambient property.
	float Ka = 0.70f;
	// Diffuse term, Kd
	// Diffuse contribution of all light sources.
	float Kd = 1.0f;
	// Specular term, Ks
	// Shinyness of the material.
	float Ks = 0.75f;
	// Specular power
	float specularPower = 4.0f;

	// Emissive component
	float4 emissive = Ke;

	// Ambient component
	float4 ambient = Ka * vLightCol;
	
	
	lightDir = normalize(lightDir);

	// Point light
	lightDir = normalize((float3) vLightDir - (float3) WorldPos);

	// Diffuse component
	//lightDir = mul(lightDir, World);
	float diffuseLight = max(dot(input.norm, lightDir), 0);
	float4 diffuse = Kd * vLightCol * diffuseLight;

	
	
	// Specular component
	// Blinn-Phong lighting model
	WorldPos.w = 0.0f;
	float4 eyePosition = float4(0.0f, 0.0f, 0.0f, 1.0f);
	float4 V = normalize(eyePosition - WorldPos);
	float4 H = float4(normalize(lightDir + V),1.0f);
	float specularLight = pow(dot(input.norm, H), specularPower);
	if (diffuseLight <= 0)
	{
		//specularLight = 0;
	}
	float4 specular = Ks * vLightCol * specularLight;


	finalCol = input.col;// / 1.67f;
	//lightIntensity = dot(input.norm, lightDir);
	//finalCol += saturate(input.col * lightIntensity * vLightCol);
	//finalCol.a = 1.0;
	
	//return finalCol *texCol;

	//float spotPower = 0.10f;
	//float spotScale = pow(max(dot(float4(lightDir, 1.0f), -vLightDir), 0), spotPower);
	//float4 finalLight = emissive + ambient + (diffuse + specular)*spotScale;

	float4 finalLight = emissive + ambient + diffuse + specular;
	finalCol *= finalLight;

	return finalCol * texCol;
}


technique10 Render
{
	pass P0
	{
		SetVertexShader(CompileShader(vs_5_0, VSMain()));
		SetPixelShader(CompileShader(ps_5_0, PSMain()));
	}
}