Shader "Unlit/ShipLiquids"{
	Properties{
		_ElementsLiquidGasTex_0 ("ElementsLiquidGasTex_0", 2D) = "white" {}
		_ElementsLiquidGasTex_1 ("ElementsLiquidGasTex_1", 2D) = "white" {}
		_ElementsLiquidGasTex_2 ("ElementsLiquidGasTex_2", 2D) = "white" {}
		_ElementsSolidTex ("IceTexture", 2D) = "white" {}
		_ScaleShipX ("ScaleShipX", Int) = 200
		_ScaleShipY ("ScaleShipY", Int) = 50
		_ScaleElements ("_ScalePerlin", Float) = 0.125
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
				fixed2 uvElements : TEXCOORD1;
				fixed4 color : COLOR0;
			};

			struct v2f{
				float4 vertex : SV_POSITION;
				fixed2 uvTexture : TEXCOORD0;
				fixed2 uvElements : TEXCOORD1;
				fixed4 color : COLOR0;
			};

			sampler2D _ElementsLiquidGasTex_0;
			sampler2D _ElementsLiquidGasTex_1;
			sampler2D _ElementsLiquidGasTex_2;
			sampler2D _ElementsSolidTex;
			float4 _ElementsLiquidGasTex_0_ST;

			int _ScaleShipX;
			int _ScaleShipY;
			float _ScaleElements;
			float _ScaleTime;
			
			v2f vert (appdata v){
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvTexture = TRANSFORM_TEX(v.uvTexture, _ElementsLiquidGasTex_0);
				o.uvElements = TRANSFORM_TEX(v.uvElements, _ElementsLiquidGasTex_0);
				o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target{
				i.uvElements.x = i.uvElements.x * _ScaleShipX;
				i.uvElements.y = i.uvElements.y * _ScaleShipY;
				i.uvElements.x *= _ScaleElements;
				i.uvElements.y *= _ScaleElements;
				
				fixed pixelLiquidGas_0 = tex2D(_ElementsLiquidGasTex_0, i.uvElements * 0.5);
				fixed pixelLiquidGas_1 = tex2D(_ElementsLiquidGasTex_1, i.uvElements * 0.5);
				fixed pixelLiquidGas_2 = tex2D(_ElementsLiquidGasTex_2, i.uvElements * 0.5);
				fixed pixelLiquidGasFinal = pixelLiquidGas_0;
				fixed pixelSolid = tex2D(_ElementsSolidTex, i.uvElements * 0.75);


				half time = (_Time * _ScaleTime) % 1.0;
				pixelLiquidGasFinal = lerp(pixelLiquidGasFinal, pixelLiquidGas_1, max(0.0, time - 0.0) / 0.33);
				pixelLiquidGasFinal = lerp(pixelLiquidGasFinal, pixelLiquidGas_2, max(0.0, time - 0.33) / 0.33);
				pixelLiquidGasFinal = lerp(pixelLiquidGasFinal, pixelLiquidGas_0, max(0.0, time - 0.66) / 0.33);

				half solid = round(i.color.b * pixelSolid) - (0.25 * pixelSolid * round(i.color.b * pixelSolid)); // solid
				half liquid = step(pixelLiquidGasFinal, i.color.r); // liquid
				half gas = i.color.g * pow(pixelLiquidGasFinal * (i.color.g + 2.0) * 0.5, 2); // gas

				// TODO: replace with something sent from cpu
				fixed4 solidColor = fixed4(0.75, 0.95, 1.0, 1.0);
				fixed4 liquidColor = fixed4(0.35, 0.75, 1.0, 0.75);
				fixed4 gasColor = fixed4(0.85, 0.95, 1.0, 0.5);

				fixed4 outputColor = fixed4(0.0, 0.0, 0.0, 1.0);
				outputColor = solid * solidColor + liquid * liquidColor + gas * gasColor;
				outputColor.a = 1;

				return outputColor;
			}
			ENDCG
		}
	}
}
