Shader "PWS/AdditiveBrush"
{
     Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BrushTex ("Brush Texture (RGB)", 2D) = "black" {}
    }
    SubShader
    {
        // No lighting or shadow calculations needed
        Tags { "RenderType"="Opaque" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _VizTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the brush texture
                float brushValue = tex2D(_MainTex, i.uv).r;

                // Add the brush value to the red channel
                fixed4 vizColor = tex2D(_VizTex, i.uv);
                vizColor += brushValue;
                if (vizColor.r > 1.0)
				{
					vizColor.r = 1.0;
				}
                if (vizColor.r < 0.25)
                {
                    vizColor.r = 0;
                    vizColor.g = 0;
                    vizColor.b = 0;
                    vizColor.a = 0;
                }
                return vizColor;
            }
            ENDCG        
        }
    }
    FallBack"Diffuse"
}
