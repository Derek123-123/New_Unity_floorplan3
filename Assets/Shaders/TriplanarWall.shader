Shader "Custom/TriplanarWall"
{
    Properties
    {
    _Color("Color Tint", Color) = (1,1,1,1)
    _MainTex("Albedo", 2D) = "white" {}
    _NormalMap("Normal Map", 2D) = "bump" {}
    _NormalScale("Normal Scale", Range(0,2)) = 1
    _Tiling("Tiling (repeats per meter)", Float) = 1.0
    _BlendSharpness("Blend Sharpness", Range(1,16)) = 4.0
    _Metallic("Metallic", Range(0,1)) = 0.0
    _Smoothness("Smoothness", Range(0,1)) = 0.5
    }
        SubShader
    {
    Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
    LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalMap;
        float4 _Color;
        float _Tiling;
        float _BlendSharpness;
        half _NormalScale;
        half _Metallic;
        half _Smoothness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        // Helper to sample normal map as if it were in tangent space but used per-axis
        inline float3 UnpackNormalSafe(float4 c)
        {
            float3 n;
            n.xy = c.ag * 2 - 1;
            n.z = sqrt(saturate(1.0 - dot(n.xy, n.xy)));
            return n;
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.worldNormal = UnityObjectToWorldNormal(v.normal);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Axis weights from world normal
            float3 n = normalize(IN.worldNormal);
            float3 an = abs(n);
            // Sharpen blending
            float3 w = pow(an, _BlendSharpness);
            w /= max(dot(w, 1.0), 1e-5);

            // World-space coords scaled by tiling
            float t = max(_Tiling, 1e-5);
            float2 uvX = IN.worldPos.zy * t; // Projection onto YZ for X-facing surfaces
            float2 uvY = IN.worldPos.xz * t; // Projection onto XZ for Y-facing
            float2 uvZ = IN.worldPos.xy * t; // Projection onto XY for Z-facing

            float4 colX = tex2D(_MainTex, uvX);
            float4 colY = tex2D(_MainTex, uvY);
            float4 colZ = tex2D(_MainTex, uvZ);
            float4 albedo = colX * w.x + colY * w.y + colZ * w.z;

            // Normal map triplanar
            float3 nX = UnpackNormalSafe(tex2D(_NormalMap, uvX));
            float3 nY = UnpackNormalSafe(tex2D(_NormalMap, uvY));
            float3 nZ = UnpackNormalSafe(tex2D(_NormalMap, uvZ));

            // Re-orient sampled normals to world axes
            // X-projection uses YZ plane -> tangent axes (0,1,0) and (0,0,1)
            float3 nWX = float3(nX.z, nX.x, nX.y); // map (tangent x,y,z) to world (x,y,z)
            // Y-projection uses XZ plane
            float3 nWY = float3(nY.x, nY.z, nY.y);
            // Z-projection uses XY plane
            float3 nWZ = float3(nZ.x, nZ.y, nZ.z);

            float3 nW = normalize(nWX * w.x + nWY * w.y + nWZ * w.z);
            // Blend with geometric normal if no normal map or to control intensity
            nW = normalize(lerp(normalize(IN.worldNormal), nW, saturate(_NormalScale)));

            o.Albedo = (albedo.rgb * _Color.rgb);
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = 1.0;

            // Transform world normal into tangent basis expected by Standard lighting
            // Unity¡¯s Standard surface shader expects o.Normal in tangent space.
            // We use WorldNormalVector to convert world-space nW to tangent space.
            float3 tSpace = WorldNormalVector(IN, nW);
            o.Normal = tSpace;
        }
        ENDCG
    }
        FallBack "Diffuse"
}