Shader "Custom/2DBackgroundBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Size", Range(0.0, 0.015)) = 0.004
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float _BlurSize;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float stepX = _BlurSize;
                float stepY = _BlurSize;

                fixed4 col = fixed4(0,0,0,0);

                // 9-sample Gaussian box blur matrix
                col += tex2D(_MainTex, uv + float2(-stepX, -stepY)) * 0.077;
                col += tex2D(_MainTex, uv + float2( 0.0,   -stepY)) * 0.123;
                col += tex2D(_MainTex, uv + float2( stepX, -stepY)) * 0.077;

                col += tex2D(_MainTex, uv + float2(-stepX,  0.0  )) * 0.123;
                col += tex2D(_MainTex, uv + float2( 0.0,    0.0  )) * 0.200;
                col += tex2D(_MainTex, uv + float2( stepX,  0.0  )) * 0.123;

                col += tex2D(_MainTex, uv + float2(-stepX,  stepY)) * 0.077;
                col += tex2D(_MainTex, uv + float2( 0.0,    stepY)) * 0.123;
                col += tex2D(_MainTex, uv + float2( stepX,  stepY)) * 0.077;

                col *= IN.color;
                col.rgb *= col.a;
                return col;
            }
        ENDCG
        }
    }
}
