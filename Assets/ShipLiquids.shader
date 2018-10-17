Shader "Unlit/ShipLiquids"{
	Properties{
		_PerlinTex_0 ("PerlinTexture_0", 2D) = "white" {}
		_PerlinTex_1 ("PerlinTexture_1", 2D) = "white" {}
		_PerlinTex_2 ("PerlinTexture_2", 2D) = "white" {}
		_ScaleShipX ("ScaleShipX", Int) = 200
		_ScaleShipY ("ScaleShipY", Int) = 50
		_ScalePerlin ("_ScalePerlin", Float) = 0.125
		_ScaleTime ("ScaleTime", Float) = 2.0
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
				fixed2 uvTexture : TEXCOORD0;
				fixed2 uvPerlin : TEXCOORD1;
				fixed color : COLOR;
			};

			struct v2f{
				float4 vertex : SV_POSITION;
				fixed2 uvTexture : TEXCOORD0;
				fixed2 uvPerlin : TEXCOORD1;
				fixed color : COLOR;
			};

			sampler2D _PerlinTex_0;
			sampler2D _PerlinTex_1;
			sampler2D _PerlinTex_2;
			float4 _PerlinTex_0_ST;

			int _ScaleShipX;
			int _ScaleShipY;
			float _ScalePerlin;
			float _ScaleTime;
			
			v2f vert (appdata v){
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvTexture = TRANSFORM_TEX(v.uvTexture, _PerlinTex_0);
				o.uvPerlin = TRANSFORM_TEX(v.uvPerlin, _PerlinTex_0);
				o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target{
				i.uvPerlin.x = i.uvPerlin.x * _ScaleShipX;
				i.uvPerlin.y = i.uvPerlin.y * _ScaleShipY;
				i.uvPerlin.x *= _ScalePerlin;
				i.uvPerlin.y *= _ScalePerlin;
				
				fixed perlin_0 = tex2D(_PerlinTex_0, i.uvPerlin);
				fixed perlin_1 = tex2D(_PerlinTex_1, i.uvPerlin);
				fixed perlin_2 = tex2D(_PerlinTex_2, i.uvPerlin);

				half time = (_Time * _ScaleTime) % 1.0;

				fixed perlinFinal = perlin_0;
				perlinFinal = lerp(perlinFinal, perlin_1, max(0.0, time - 0.0) / 0.33);
				perlinFinal = lerp(perlinFinal, perlin_2, max(0.0, time - 0.33) / 0.33);
				perlinFinal = lerp(perlinFinal, perlin_0, max(0.0, time - 0.66) / 0.33);
				
				return fixed4(step(perlinFinal, i.color), 0.0, 0.0, 1.0);
			}
			ENDCG
		}
	}
}
