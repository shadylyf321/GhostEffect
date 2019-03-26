// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/GhostEffect"
{
	Properties
	{
		_MainTex ("", 2D) = "" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	struct v2f{
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};

	sampler2D _MainTex;
	sampler2D _Tex0;
	sampler2D _Tex1;
	float _fadeout;

	v2f vert( appdata_img v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		return o;
	}

	float4 fragAdd(v2f pixelData) : COLOR0
	{
		float4 col1 = tex2D(_Tex1, pixelData.uv);
		float4 colM = tex2D(_MainTex, pixelData.uv);
		float4 col;
		col = colM + col1 * (1 - colM) * 0.7f;
		col *= _fadeout;
		return col;
	}

	float4 fragBind(v2f pixelData) :COLOR0
	{
		float y = pixelData.uv.y;
		#if UNITY_UV_STARTS_AT_TOP
			y = 1 - y;
		#endif
		y = 1 - y;
		float2 uv = float2(pixelData.uv.x, y);
		float4 col0 = tex2D(_Tex0, uv);
		float4 col1 = tex2D(_Tex1, pixelData.uv);
		float4 col;
		col = col1 * col1.a + col0 * (1 - col1.a);
		return col;
	}
	ENDCG

	SubShader{
		Pass{

			ZTest Always Cull Off Zwrite Off
			Fog { Mode off }

			CGPROGRAM
			#pragma glsl
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment fragAdd
			#pragma target 3.0
			ENDCG
		}

		Pass{

			ZTest Always Cull Off Zwrite Off
			Fog { Mode off }

			CGPROGRAM
			#pragma glsl
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment fragBind
			#pragma target 3.0
			ENDCG
		}
	}
	FallBack off
}
