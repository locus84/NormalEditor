// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "NormalEditor/NormalWriter"
{
    Properties
    {
        _OriginalNormal ("OriginalNormal", 2D) = "bump" {}
		_PaintingNormal ("PaintingNormal", 2D) = "white" {}
		_WorldNormal("WorldNormal", Color) = (0.5, 0.5, 1.0, 1.0) 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float3 tanNormal : TEXCOORD1;
				float vertexXpos : TEXCOORD2;
				half3x3 worldToTangent : TEXCOORD3;
            };

            sampler2D _OriginalNormal;
			float4 _OriginalNormal_ST;
			sampler2D _PaintingNormal;
			float4 _WorldNormal;

			float3 slerp(float3 start, float3 end, float percent)
			{
				// Dot product - the cosine of the angle between 2 vectors.
				float doted = dot(start, end);
				// Clamp it to be in the range of Acos()
				// This may be unnecessary, but floating point
				// precision can be a fickle mistress.
				doted = clamp(doted, -1.0, 1.0);
				// Acos(dot) returns the angle between start and end,
				// And multiplying that by percent returns the angle between
				// start and the final result.
				float theta = acos(doted)*percent;
				float3 RelativeVec = normalize(end - start * doted); // Orthonormal basis
				// The final result.
				return ((start*cos(theta)) + (RelativeVec*sin(theta)));
			}

			float4 SlerpQuat(float4 p0, float4 p1, float t)
			{
				float dotp = dot(normalize(p0), normalize(p1));
				if ((dotp > 0.9999) || (dotp < -0.9999))
				{
					if (t <= 0.5)
						return p0;
					return p1;
				}
				float theta = acos(dotp);
				float4 P = ((p0*sin((1 - t)*theta) + p1 * sin(t*theta)) / sin(theta));
				P.w = 1;
				return P;
			}

			float3 Slerp(float3 from, float3 to, float step)
			{
				if (step <= 0) return from;
				if (step >= 1.0) return to;

				float theta = acos(dot(from, to));
				//float thetaDegree = theta * RadianToDegree;

				if (theta == 0) return to;

				float sinTheta = sin(theta);
				return (from * (sin((1 - step) * theta) / sinTheta)) + (to * (sin(step * theta) / sinTheta));
			}

			float3 UnityWorldToObjectNormal(float3 norm)
			{
				return normalize(unity_WorldToObject[0].xyz * norm.x + unity_WorldToObject[1].xyz * norm.y + unity_WorldToObject[2].xyz * norm.z);
			}

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _OriginalNormal);
				TANGENT_SPACE_ROTATION;
				float3 worldNormal = _WorldNormal * 2 - 1;
				float3 objectNormal = UnityWorldToObjectNormal(worldNormal);

				o.tanNormal = normalize(mul((float3x3)rotation, objectNormal));// objectNormal);
				o.vertexXpos = v.vertex.x;
				v.vertex = float4(v.uv - 0.5, 0, 1);//mul(unity_WorldToObject, float4(v.uv, 0, 1));
				o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                // sample the texture
				float paintingStrength = tex2D(_PaintingNormal, i.uv).x;
				float4 originalNormal = tex2D(_OriginalNormal, i.uv) * 2 - 1;
				float3 lerped = lerp(originalNormal.xyz, i.tanNormal, paintingStrength);
				return float4((normalize(lerped) + 1) * 0.5, 1);
            }
            ENDCG
        }
    }
}
