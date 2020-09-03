Shader "VFX/Special/Liquid Surface" {
    Properties {
	[Header(Textures)]
        _MainTex ("Surface Noise", 2D) = "white" {}
        _NoiseTex ("Color Ramp Noise", 2D) = "white" {}
	[Header(Color)]
        _EmissivePowerColor ("Emissive Power Color", Range(0, 4)) = 1.6
        _EmissivePowerWhite ("Emissive Power White", Range(0, 10)) = 10
        _FresnelExponent ("Fresnel Exponent", Range(0, 0.2)) = 0.05
	_Density ("Density", Range(0.2, 4)) = 1.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
	    fixed4 _MainTex_ST;
            sampler2D _NoiseTex;
	    fixed4 _NoiseTex_ST;
            fixed _FresnelExponent;
            fixed _EmissivePowerColor;
            fixed _EmissivePowerWhite;
	    fixed _Density;

            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord0 : TEXCOORD0;
                fixed4 vertexColor : COLOR;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                fixed4 vertexColor : COLOR;
            };

            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos( v.vertex );
                return o;
            }

            float4 frag(VertexOutput i) : COLOR {                
                float2 customuv = i.uv0.xy + i.uv0.zw;

                fixed4 noise = tex2D(_NoiseTex, TRANSFORM_TEX(customuv, _NoiseTex));
		noise.r = noise.r * noise.a;
                fixed4 tex = tex2D(_MainTex, TRANSFORM_TEX(customuv, _MainTex));
		tex.r = tex.r * tex.a;
            
                tex.g = 1.0 - pow(dot(normalize(i.normalDir), normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz)), _FresnelExponent);
                tex.b = 1.0 - i.vertexColor.a;
		tex.a = _EmissivePowerWhite * saturate(saturate((tex.r * 4.0 - 2.0) * saturate(tex.g * 6.4 - 0.4)) - tex.b) * noise.r;

                noise.rgb = _EmissivePowerColor * lerp((0.3 * i.vertexColor.rgb), i.vertexColor.rgb, noise.r) + tex.a;
		noise.a = round(saturate(saturate(tex.r * _Density - 0.3) - tex.b) * 1.3);
                
                return fixed4(noise.rgb, noise.a);
            }
            ENDCG
        }
    }
}
