Shader "VFX/Special/Liquid Splat" {
    Properties {
	[Header(Texture)]
        [NoScaleOffset] _MainTex ("Liquid Surface Texture", 2D) = "white" {}
	[Header(Color)]
        _EmissivePower ("Emissive Power", Range(0, 15)) = 2
        _MaximumEmissivePower ("Maximum Emissive Power", Range(0, 2)) = 1.5
        _MaximumPower ("Maximum Power", Range(0, 7)) = 7
	[Header(Liquify Controls)]
        _LiquifyPower ("Liquify Power", Float) = 12
        _LiquifySpeed ("Liquify Speed", Float) = 2.5
    }

    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
	    Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed _LiquifyPower;
            fixed _LiquifySpeed;
            fixed _EmissivePower;
            fixed _MaximumPower;
            fixed _MaximumEmissivePower;

            struct VertexInput {
                float4 vertex : POSITION;
                float4 texcoord0 : TEXCOORD0;
                fixed4 vertexColor : COLOR;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                fixed4 vertexColor : COLOR;
            };

            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.pos = UnityObjectToClipPos( v.vertex );
                return o;
            }

            fixed4 frag(VertexOutput i) : COLOR {
                fixed4 tex = tex2D(_MainTex, i.uv0.xy);
		tex.r = tex.r * tex.a;

                tex.g = _LiquifyPower * tex.r + _Time.y * _LiquifySpeed;
                tex.b = cos(tex.g);
                tex.a = sin(tex.g);
                fixed2 liquify = mul(i.uv0.xy - fixed2(0.5, 0.5), float2x2(tex.b, -tex.a, tex.a, tex.b)) + fixed2(0.5, 0.5);

                liquify.r = (liquify.r + liquify.g) * 0.5;
                liquify.g = smoothstep(clamp(tex.r, 0.0001, 1.0), 0.0, i.uv0.z * 0.9 + 0.1);
                tex.r = saturate(liquify.r) * liquify.g;
                fixed3 color = i.vertexColor.rgb * tex.r * _EmissivePower + pow(tex.r * _MaximumEmissivePower, _MaximumPower);

                return fixed4(color, i.vertexColor.a * liquify.g);
            }
            ENDCG
        }
    }
}
