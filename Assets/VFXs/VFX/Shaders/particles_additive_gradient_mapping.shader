Shader "VFX/Particles/Additive Gradient Map" {
    Properties {
        [Header(Particles Additive)]
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
        _ColorIn ("Color In", Color) = (1, 1, 1, 1)
        _ColorOut ("Color Out", Color) = (1, 1, 1, 1)
        _GradPos ("Gradient Position", Float) = 0
        _EmissivePower ("Emissive Power", Range(1,30)) = 1.5    
    }
    SubShader {
        Tags {
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
            
            sampler2D _MainTex;
            fixed _EmissivePower;
            fixed _GradPos;
            fixed4 _ColorIn;
            fixed4 _ColorOut;
            
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
                fixed pic = saturate(((tex.r * tex.a) - i.uv0.z)) * i.vertexColor.a;
		        fixed3 color = lerp(_ColorOut, _ColorIn, saturate(pic + _GradPos));
		        color = color + fixed3(1,1,1) * saturate(pic * 0.18 + _GradPos * 0.18);
                return fixed4(pic * color * _EmissivePower, tex.r);
            }
            ENDCG
        }
    }
}