Shader "VFX/Particles/Alpha Blended" {
    Properties {
    [Header(Particles Alpha Blended)]
    [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
    [Toggle] _AlphaGrayscale ("Alpha From Grayscale", float) = 0
    _AlphaPower ("Alpha Power", Range(1, 10)) = 1

    }
    SubShader {
        Tags {
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
	    fixed _AlphaGrayscale;
	    fixed _AlphaPower;
            
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
                return lerp((fixed4(tex.rgb * i.vertexColor.rgb, saturate(saturate(tex.a * i.vertexColor.a * _AlphaPower) - i.uv0.z))), (fixed4(tex.r * i.vertexColor.rgb, saturate(saturate(tex.r * tex.a * i.vertexColor.a * _AlphaPower) - i.uv0.z))), _AlphaGrayscale);
            }
            ENDCG
        }
    }
}