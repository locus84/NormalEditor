Shader "NormalEditor/BrushShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_XCoord("XCoord", Float) = 0.5
		_YCoord("YCoord", Float) = 0.5
		_Radius("Radius", Float) = 0.5
		_Hardness("Hardness", Float) = 0.5
		_Strength("Strength", Float) = 0.5
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100

			Pass
			{
				CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag

				#include "UnityCG.cginc"

				uniform sampler2D _MainTex;
				float _XCoord;
				float _YCoord;
				float _Hardness;
				float _Radius;
				float _Strength;

				float distance(float2 a, float2 b)
				{
					float xDiff = a.x - b.x;
					float yDiff = a.y - b.y;
					return sqrt(xDiff * xDiff + yDiff * yDiff);
				}

				float4 frag(v2f_img i) : COLOR {

					float dist = distance(i.uv, float2(_XCoord, _YCoord));
					float4 col = tex2D(_MainTex, i.uv);
					if (dist > _Radius)
						return col;


					float lerpVal = _Strength;
					float PI_2 = 1.57079632679489661923;
					float remainder = dist - (_Radius * _Hardness);
					if (dist > _Radius * _Hardness)
					{
						lerpVal = cos((PI_2 * remainder) / (_Radius * (1 - _Hardness))) * _Strength;
					}

					col.xyz = lerp(col.xyz, float3(1,0,0), lerpVal);
					return col;
				}
				ENDCG
			}
		}
}
