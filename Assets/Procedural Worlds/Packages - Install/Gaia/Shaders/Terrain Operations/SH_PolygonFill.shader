Shader "PWS/PolygonFill"
{
     Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _FillColor ("Fill Color", Color) = (1,1,1,1)
        _UVCount ("UV Count", Float) = 0    
    }
    SubShader
    {
        // No lighting or shadow calculations needed
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _FillColor;
            StructuredBuffer<float2> _UVCoordinates; // Maximum of 10 UV coordinates
            int _UVCount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            };

            // Point in Polygon
            bool PointInPolygon(float2 p)
            {
                int i, j;
                bool c = false;
                for (i = 0, j = _UVCount - 1; i < _UVCount; j = i++)
                {
                    bool condition1 = ( (_UVCoordinates[i].y > p.y) != (_UVCoordinates[j].y > p.y) );
                    float denom = (_UVCoordinates[j].y - _UVCoordinates[i].y);

                    // Prevent division by zero
                    if (denom == 0.0)
                    {
                        continue; // Skip if edge is horizontal
                    }

                    float intersectionX = 
                        ( (_UVCoordinates[j].x - _UVCoordinates[i].x) * (p.y - _UVCoordinates[i].y) / denom ) + _UVCoordinates[i].x;
                    bool condition2 = (p.x < intersectionX);

                    if (condition1 && condition2)
                    {
                        c = !c;
                    }
                }
                return c;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Check if the current pixel is inside the polygon
                bool inside = PointInPolygon(uv);
                if (inside)
                {
                    return _FillColor;
                }
                else
                {
                    return tex2D(_MainTex, uv);
                }
            }
            ENDCG
        }
    }
    FallBack"Diffuse"
}
