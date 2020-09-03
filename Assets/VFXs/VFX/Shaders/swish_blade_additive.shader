Shader "VFX/Swish/Blade Swish Additive" {
    Properties {
	[Header(Textures)]
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        [NoScaleOffset]_MainTex ("Mask", 2D) = "white" {}
	[Header(Color)]
	_ColorIn ("Color In", Color) = (1, 1, 1, 1)
        _ColorOut ("Color Out", Color) = (1, 1, 1, 1)
        _GradPos ("Gradient Position", Float) = 0
        _EmissivePower ("Emissive Power", Range(0, 20)) = 7
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        Pass {
            Blend One One
            ZWrite Off
	    Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
	    fixed4 _NoiseTex_ST;
            sampler2D _MainTex;
            fixed _GradPos;
            fixed4 _ColorIn;
            fixed4 _ColorOut;
            fixed _EmissivePower;

            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                fixed4 texcoord1 : TEXCOORD1;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                fixed4 uv1 : TEXCOORD1;
            };

            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.uv1 = v.texcoord1;
                o.pos = UnityObjectToClipPos( v.vertex );
                return o;
            }

            fixed4 frag(VertexOutput i) : COLOR {
                float2 swishuv = float2((i.uv0.x + i.uv1.x), i.uv0.y);
                fixed4 noise = tex2D(_NoiseTex, TRANSFORM_TEX(swishuv, _NoiseTex));
		noise.r = noise.r * noise.a;
                fixed4 mask = tex2D(_MainTex, i.uv0);
		mask.r = mask.r * mask.a;
                
                mask.g = i.uv0.x * 6.0 * (1.0 - i.uv0.x);
		mask.g = saturate(mask.g * mask.g * mask.g);

		noise.g = noise.r * mask.r * mask.g;
		noise.b = saturate(i.uv0.x - i.uv1.y);
		noise.a = saturate((1.0 - i.uv0.x) - i.uv1.z);

		noise.r = saturate(noise.g - noise.b - noise.a);
		
                fixed3 color = lerp(_ColorOut, _ColorIn, saturate(noise.r + _GradPos));
		color = color + fixed3(1,1,1) * saturate(noise.r * 0.18 + _GradPos * 0.18);
		color = color * noise.r * _EmissivePower;

                return fixed4(color, noise.r);
            }
            ENDCG
        }
    }
}
