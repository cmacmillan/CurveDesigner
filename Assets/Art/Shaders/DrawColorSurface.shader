Shader "Custom/DrawColorSurface"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Specular ("Specular", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Specularity ("Specular", Range(0,1)) = 0.5
        _Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf StandardSpecular fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _Specular;

        struct Input
        {
            float2 uv_MainTex;
			float4 color : COLOR;
        };

        half4 _Color;
        half _Glossiness;
        half _Specularity;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            // Albedo comes from a texture tinted by color
			fixed4 c = IN.color*tex2D (_MainTex, IN.uv_MainTex)*_Color;
            o.Albedo = c.rgb;
            fixed4 spec = tex2D(_Specular, IN.uv_MainTex);
            o.Specular = spec.rgb*_Specularity;
            o.Smoothness = _Glossiness * spec.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
