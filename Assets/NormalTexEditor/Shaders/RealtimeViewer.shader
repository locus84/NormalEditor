Shader "NormalEditor/RealtimeViewer"
{
    Properties
    {
        _MainTex ("TextureToPack", 2D) = "bump" {}
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

//#define UNITY_NO_DXT5nm
			sampler2D _MainTex;

			float4 frag(v2f_img i) : COLOR
			{
				float3 col = tex2D(_MainTex, i.uv).xyz;
				return float4(col, 1);
#if defined(UNITY_NO_DXT5nm)
				return float4(col, 1);
#else
				return float4(1, col.y, 1, col.x);
#endif
			}
			ENDCG
		}
	}
}
