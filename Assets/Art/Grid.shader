Shader "Unlit/Grid"
{
    Properties
    {
		_Scale("_Scale",float) = 1.0
		_GridSize("_GridSize",int)=1
		_LineThickness("_LineThickness",Range(0,1)) = .1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float2 modelXZ: TEXCOORD0;
            };

			float _Scale;
			float _LineThickness;
			int _GridSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.modelXZ = v.vertex.xz;
                return o;
            }

			float mod(float x, float y) { return x - y * floor(x / y); }
			float2 mod(float2 x, float y) {return float2(mod(x.x,y),mod(x.y,y)); }

			#define e 2.71828f

			float logBase10(float x) {
				return log(x) / log(10);
			}

			#define c 3.6f
			float sigmoid(float x) {
				return 1 / (1 + pow(e, -c*x + c));
			}

			float fadeInOut(float x) {
				float sig = sigmoid(x);
				return saturate(5 * (sig * (1.0 - sig) - .03f));
			}

			float doLines(float2 xz,float gridSize,float fade) {
				float2 modded = mod(xz*_Scale,gridSize)<_LineThickness*gridSize;
				float val = modded.x || modded.y;
				return val*fadeInOut(fade);
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
				float val = doLines(i.modelXZ,pow(10,x),mod(fade+1,2))+doLines(i.modelXZ,pow(10,y),fade);
                return fixed4(val,val,val,1);
            }
            ENDCG
        }
    }
}
