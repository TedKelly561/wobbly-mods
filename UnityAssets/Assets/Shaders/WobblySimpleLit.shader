// A simple, cartoony lit shader for our models. It ships inside our AssetBundles, so our models look right
// even if the game's build left out Unity's Standard shader. Built-in render pipeline (the game uses it).
Shader "WobblyMods/SimpleLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture (optional)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.3
        _RimColor ("Rim color", Color) = (1,1,1,0.15)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf BlinnPhong fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        fixed4 _RimColor;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Specular = _Glossiness;
            o.Gloss = _Glossiness;
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            o.Emission = _RimColor.rgb * pow(rim, 3.0) * _RimColor.a;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
