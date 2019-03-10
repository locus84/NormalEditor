Shader "NormalEditor/FillShader"
{
    Properties
    {
		_FillColor("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _FillColor;

			float4 frag(v2f_img i) : COLOR
            {
                return _FillColor;
            }
			ENDCG
        }
    }
}
