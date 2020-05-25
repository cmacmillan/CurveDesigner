﻿Shader "Custom/Checkers"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_Scale("Scale",Float) = 1.0
		_Cutoff("Cutoff",Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "DisableBatching"= "True"}
        LOD 200
		

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
			float3 worldPos;
			float3 worldNormal;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
		float _Scale;
		float _Cutoff;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

		float mod(float x, float y)
		{
			return x - y * floor(x / y);
		}
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			//float2 uv = float2(IN.worldPos.x + IN.worldPos.z / 2, IN.worldPos.y + IN.worldPos.z / 2);
			//uv *= _Scale;
            //fixed4 c = tex2D (_MainTex, uv) * _Color;
			float lineThickness = .1f*_Scale;
			fixed4 c = 1;
			if ((mod(IN.worldPos.x,_Scale)<lineThickness && abs(IN.worldNormal.x)<_Cutoff)||
				(mod(IN.worldPos.y,_Scale)<lineThickness && abs(IN.worldNormal.y)<_Cutoff)||
				(mod(IN.worldPos.z,_Scale)<lineThickness && abs(IN.worldNormal.z)<_Cutoff)
				) {
				c.rgb = .5;
			}
			else {
				c.rgb = 1;
			}
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}