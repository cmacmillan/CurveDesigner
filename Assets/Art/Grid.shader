Shader "Unlit/Grid"
{
    Properties
    {
		_Scale("_Scale",float) = 1.0
		_LineThickness("_LineThickness",Range(0,.07)) = .1
		_BaseScale("_BaseScale",float) =1.0
		_Mask("_Mask",2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off
		ZTest Always
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
            };

			float _Scale;
			float _LineThickness;
			float _BaseScale;
			sampler2D _Mask;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = (v.uv-.5f)*2;
                return o;
            }

			float mod(float x, float y) { return x - y * floor(x / y); }
			float2 mod(float2 x, float y) {return float2(mod(x.x,y),mod(x.y,y)); }

			#define e 2.71828f

			float logBase10(float x) {
				return log(x) / log(10);
			}

			float sigmoid(float x,float base,float offset) {
				return 1 / (1 + pow(e, -base*x + offset));
			}

			float fadeInOut(float x,float base, float offset) {
				float sig = sigmoid(x,base,offset);
				return saturate(5 * (sig * (1.0 - sig) - .03f));
			}

#define fadeInBaseOffsetConstant 2.8
			float doLines(float2 xz,float gridSize,float fade) {
				float2 modded = mod(xz*_BaseScale*_Scale, gridSize) / (gridSize);
				float m = 1/_LineThickness;
				float val = max(fadeInOut(modded.x, m, m / 2.0), fadeInOut(modded.y,m,m/2.0));
				return val*fadeInOut(fade,fadeInBaseOffsetConstant,fadeInBaseOffsetConstant);
			}

			float stepped(float x,float offset) {
				return floor(x+offset)*3.0f;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float l = logBase10(_Scale);
				float fade = mod(l, 2);
				int x = floor(l);
				int y = x;
				if (mod(x, 2) == 0)
					x -= 1;
				else
					y -= 1;
				float val = max(doLines(i.uv, pow(10, x), mod(fade + 1, 2)), doLines(i.uv, pow(10, y), fade));
				return fixed4(val, val, val, val*tex2D(_Mask, i.uv/2+.5).r);
            }
            ENDCG
        }
    }
}
