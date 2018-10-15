Shader "Unlit/PerlinTest"{
	Properties{
		_PerlinTex_0 ("PerlinTexture_0", 2D) = "white" {}
		_PerlinTex_1 ("PerlinTexture_1", 2D) = "white" {}
		_PerlinTex_2 ("PerlinTexture_2", 2D) = "white" {}
	}
	SubShader{
		Tags { "RenderType"="Opaque" }

		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata{
				float4 vertex : POSITION;
				fixed2 uv : TEXCOORD0;
				fixed color : COLOR;
			};

			struct v2f{
				float4 vertex : SV_POSITION;
				fixed2 uv : TEXCOORD0;
				fixed color : COLOR;
			};

			sampler2D _PerlinTex_0;
			sampler2D _PerlinTex_1;
			sampler2D _PerlinTex_2;
			float4 _PerlinTex_0_ST;
			
			v2f vert (appdata v){
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _PerlinTex_0);
				o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target{
				i.uv.x = round(i.uv.x * 32) / 32.0;
				i.uv.y = round(i.uv.y * 32) / 32.0;

				fixed vColor = round(i.color * 32) / 32.0;

				fixed perlin_0 = tex2D(_PerlinTex_0, i.uv);
				fixed perlin_1 = tex2D(_PerlinTex_1, i.uv);
				fixed perlin_2 = tex2D(_PerlinTex_2, i.uv);

				half time = (_Time * 5.0/*0.33*/) % 1.0;

				fixed perlinFinal = perlin_0;
				perlinFinal = lerp(perlinFinal, perlin_1, max(0.0, time - 0.0) / 0.33);
				perlinFinal = lerp(perlinFinal, perlin_2, max(0.0, time - 0.33) / 0.33);
				perlinFinal = lerp(perlinFinal, perlin_0, max(0.0, time - 0.66) / 0.33);
				
				return fixed4(step(perlinFinal, vColor), 0.0, 0.0, 1.0);
			}
			ENDCG
		}
	}
}
