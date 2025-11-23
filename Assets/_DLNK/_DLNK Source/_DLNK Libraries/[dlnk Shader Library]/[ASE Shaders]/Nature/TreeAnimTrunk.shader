// Made with Amplify Shader Editor v1.9.1.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DLNK Shaders/ASE/Nature/TreeAnimTrunk"
{
	Properties
	{
		_Color("Color", Color) = (0.8207547,0.8207547,0.8207547,0)
		_MainTex("Albedo", 2D) = "white" {}
		_BumpMap("BumpMap", 2D) = "bump" {}
		_BumpScale("BumpScale", Float) = 1
		_Glossiness("Glossiness", Float) = 0.5
		_OcclusionMap("Occlusion Map", 2D) = "white" {}
		_OcclusionStrength("Occlusion Strength", Float) = 0
		_DetailMask("DetailMask", 2D) = "white" {}
		_DetailAlbedoMap("DetailAlbedoMap", 2D) = "white" {}
		_DetailNormalMap("DetailNormalMap", 2D) = "bump" {}
		_DetailNormalMapScale("DetailNormalMapScale", Float) = 1
		_ColorTop("ColorTop", Color) = (0.8490566,0.8450516,0.8450516,0)
		_TopTiling("TopTiling", Float) = 1
		[Toggle]_UVWorld("UVWorld", Float) = 0
		_Ammount("Ammount", Float) = 0.5
		_Smooth("Smooth", Float) = 0.5
		_Clamp("Clamp (xy)", Vector) = (0,1,0,0)
		_TopVertical("Vertical Top Limit (xy)", Vector) = (0,0,0,0)
		_AlbedoTop("AlbedoTop", 2D) = "white" {}
		_BumpMapTop("BumpMapTop", 2D) = "bump" {}
		_BumpMix("BumpMix", Float) = 1
		_BumpScaleTop("BumpScaleTop", Float) = 1
		_GlossinessTop("GlossinessTop", Float) = 0.5
		_OcclusionTop("Occlusion Top", Float) = 0
		_Noisespeed("Noise Speed", Float) = 1
		_MotionValue("MotionValue (xy)", Vector) = (1,1,0,0)
		_WorldNoiseScale("WorldNoiseScale", Float) = 1
		_VerticalOffset("Vertical Offset (xy)", Vector) = (0,1,0,0)
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
			float3 worldNormal;
			INTERNAL_DATA
			float2 uv2_texcoord2;
		};

		uniform float _Noisespeed;
		uniform float _WorldNoiseScale;
		uniform float2 _MotionValue;
		uniform float2 _VerticalOffset;
		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform float _BumpScale;
		uniform sampler2D _DetailNormalMap;
		uniform float4 _DetailNormalMap_ST;
		uniform float _DetailNormalMapScale;
		uniform sampler2D _DetailMask;
		uniform float4 _DetailMask_ST;
		uniform sampler2D _BumpMapTop;
		uniform float _UVWorld;
		uniform float _TopTiling;
		uniform float _BumpScaleTop;
		uniform float _BumpMix;
		uniform float _Ammount;
		uniform float2 _Clamp;
		uniform float4 _Color;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _DetailAlbedoMap;
		uniform float4 _DetailAlbedoMap_ST;
		uniform float4 _ColorTop;
		uniform sampler2D _AlbedoTop;
		uniform float _Smooth;
		uniform float2 _TopVertical;
		uniform float _Glossiness;
		uniform float _GlossinessTop;
		uniform sampler2D _OcclusionMap;
		uniform float4 _OcclusionMap_ST;
		uniform float _OcclusionTop;
		uniform float _OcclusionStrength;


		float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }

		float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }

		float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }

		float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }

		float snoise( float3 v )
		{
			const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
			float3 i = floor( v + dot( v, C.yyy ) );
			float3 x0 = v - i + dot( i, C.xxx );
			float3 g = step( x0.yzx, x0.xyz );
			float3 l = 1.0 - g;
			float3 i1 = min( g.xyz, l.zxy );
			float3 i2 = max( g.xyz, l.zxy );
			float3 x1 = x0 - i1 + C.xxx;
			float3 x2 = x0 - i2 + C.yyy;
			float3 x3 = x0 - 0.5;
			i = mod3D289( i);
			float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
			float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
			float4 x_ = floor( j / 7.0 );
			float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
			float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 h = 1.0 - abs( x ) - abs( y );
			float4 b0 = float4( x.xy, y.xy );
			float4 b1 = float4( x.zw, y.zw );
			float4 s0 = floor( b0 ) * 2.0 + 1.0;
			float4 s1 = floor( b1 ) * 2.0 + 1.0;
			float4 sh = -step( h, 0.0 );
			float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
			float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
			float3 g0 = float3( a0.xy, h.x );
			float3 g1 = float3( a0.zw, h.y );
			float3 g2 = float3( a1.xy, h.z );
			float3 g3 = float3( a1.zw, h.w );
			float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
			g0 *= norm.x;
			g1 *= norm.y;
			g2 *= norm.z;
			g3 *= norm.w;
			float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
			m = m* m;
			m = m* m;
			float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
			return 42.0 * dot( m, px);
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float2 temp_cast_0 = (_Noisespeed).xx;
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float2 panner164 = ( 1.0 * _Time.y * temp_cast_0 + ase_worldPos.xy);
			float simplePerlin3D165 = snoise( float3( panner164 ,  0.0 )*_WorldNoiseScale );
			simplePerlin3D165 = simplePerlin3D165*0.5 + 0.5;
			float lerpResult159 = lerp( _MotionValue.x , -_MotionValue.x , _CosTime.z);
			float lerpResult161 = lerp( _MotionValue.y , -_MotionValue.y , _CosTime.y);
			float4 appendResult138 = (float4(( simplePerlin3D165 * lerpResult159 ) , 0.0 , ( simplePerlin3D165 * lerpResult161 ) , 0.0));
			float4 transform137 = mul(unity_WorldToObject,appendResult138);
			float4 lerpResult146 = lerp( float4( 0,0,0,0 ) , transform137 , saturate( (_VerticalOffset.x + (v.texcoord1.xy.y - 0.0) * (_VerticalOffset.y - _VerticalOffset.x) / (1.0 - 0.0)) ));
			v.vertex.xyz += lerpResult146.xyz;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_BumpMap = i.uv_texcoord * _BumpMap_ST.xy + _BumpMap_ST.zw;
			float3 tex2DNode4 = UnpackScaleNormal( tex2D( _BumpMap, uv_BumpMap ), _BumpScale );
			float2 uv_DetailNormalMap = i.uv_texcoord * _DetailNormalMap_ST.xy + _DetailNormalMap_ST.zw;
			float2 uv_DetailMask = i.uv_texcoord * _DetailMask_ST.xy + _DetailMask_ST.zw;
			float4 tex2DNode43 = tex2D( _DetailMask, uv_DetailMask );
			float3 lerpResult46 = lerp( tex2DNode4 , BlendNormals( tex2DNode4 , UnpackScaleNormal( tex2D( _DetailNormalMap, uv_DetailNormalMap ), _DetailNormalMapScale ) ) , tex2DNode43.a);
			float2 temp_cast_0 = (_TopTiling).xx;
			float2 uv_TexCoord110 = i.uv_texcoord * temp_cast_0;
			float3 ase_worldPos = i.worldPos;
			float4 appendResult132 = (float4(ase_worldPos.x , ase_worldPos.z , 0.0 , 0.0));
			float3 tex2DNode5 = UnpackScaleNormal( tex2D( _BumpMapTop, (( _UVWorld )?( ( appendResult132 * _TopTiling * 0.1 ) ):( float4( uv_TexCoord110, 0.0 , 0.0 ) )).xy ), _BumpScaleTop );
			float3 lerpResult120 = lerp( tex2DNode5 , BlendNormals( tex2DNode5 , tex2DNode4 ) , _BumpMix);
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_normWorldNormal = normalize( ase_worldNormal );
			float3 lerpResult11 = lerp( lerpResult46 , lerpResult120 , (0.0 + (( ase_normWorldNormal.y * _Ammount ) - _Clamp.x) * (1.0 - 0.0) / (_Clamp.y - _Clamp.x)));
			o.Normal = lerpResult11;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode2 = tex2D( _MainTex, uv_MainTex );
			float2 uv_DetailAlbedoMap = i.uv_texcoord * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
			float4 lerpResult44 = lerp( ( _Color * tex2DNode2 ) , ( tex2DNode2 * tex2D( _DetailAlbedoMap, uv_DetailAlbedoMap ) * _Color ) , tex2DNode43.a);
			float smoothstepResult118 = smoothstep( 0.0 , _Smooth , normalize( (WorldNormalVector( i , lerpResult11 )) ).y);
			float temp_output_16_0 = saturate( ( (0.0 + (( smoothstepResult118 * _Ammount ) - _Clamp.x) * (1.0 - 0.0) / (_Clamp.y - _Clamp.x)) + (_TopVertical.x + (i.uv2_texcoord2.y - 0.0) * (_TopVertical.y - _TopVertical.x) / (1.0 - 0.0)) ) );
			float4 lerpResult18 = lerp( lerpResult44 , ( _ColorTop * tex2D( _AlbedoTop, (( _UVWorld )?( ( appendResult132 * _TopTiling * 0.1 ) ):( float4( uv_TexCoord110, 0.0 , 0.0 ) )).xy ) ) , temp_output_16_0);
			o.Albedo = lerpResult18.rgb;
			float lerpResult19 = lerp( ( 0.0 * _Glossiness ) , ( 0.0 * _GlossinessTop ) , temp_output_16_0);
			o.Smoothness = lerpResult19;
			float2 uv_OcclusionMap = i.uv_texcoord * _OcclusionMap_ST.xy + _OcclusionMap_ST.zw;
			float4 tex2DNode48 = tex2D( _OcclusionMap, uv_OcclusionMap );
			float lerpResult51 = lerp( tex2DNode48.r , ( tex2DNode48.r + ( 1.0 - _OcclusionTop ) ) , temp_output_16_0);
			o.Occlusion = saturate( ( lerpResult51 + ( 1.0 - _OcclusionStrength ) ) );
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows dithercrossfade vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float4 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.customPack1.zw = customInputData.uv2_texcoord2;
				o.customPack1.zw = v.texcoord1;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				surfIN.uv2_texcoord2 = IN.customPack1.zw;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19103
Node;AmplifyShaderEditor.BlendNormalsNode;47;-603.8795,351.5309;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;130;411.2387,75.7908;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;DLNK Shaders/ASE/Nature/TreeAnimTrunk;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-348.7778,756.045;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-346.1777,659.1956;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;19;-207.1566,691.0037;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-506.7227,672.2026;Inherit;False;Property;_Glossiness;Glossiness;4;0;Create;True;0;0;0;False;0;False;0.5;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;-529.2714,758.1539;Inherit;False;Property;_GlossinessTop;GlossinessTop;22;0;Create;True;0;0;0;False;0;False;0.5;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;39;-603.2067,256.1049;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;121;-541.3607,468.8547;Inherit;False;Property;_BumpMix;BumpMix;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;46;-386.6988,250.7465;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;120;-383.3607,385.8547;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;11;-160.0016,301.8399;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;4;-933.4177,242.5252;Inherit;True;Property;_BumpMap;BumpMap;2;0;Create;True;0;0;0;False;0;False;-1;None;e95f053a5488d944b8caa68a915cefea;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;13;-1124.451,456.7495;Inherit;False;Property;_BumpScaleTop;BumpScaleTop;21;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-1103.014,316.8825;Inherit;False;Property;_BumpScale;BumpScale;3;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;43;-1568.311,566.568;Inherit;True;Property;_DetailMask;DetailMask;7;0;Create;True;0;0;0;False;0;False;-1;None;030d9c1f37fa863459d7d360ebbe374d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;59;279.1635,662.7813;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;58;439.4158,681.1709;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;54;464.3732,572.1467;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;53;226.6218,753.4156;Inherit;False;Property;_OcclusionStrength;Occlusion Strength;6;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;51;510.4846,851.2618;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;56;556.6533,1039.369;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;195.7923,1064.505;Inherit;False;Property;_OcclusionTop;Occlusion Top;23;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;57;388.8759,1057.351;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;48;200.5735,838.813;Inherit;True;Property;_OcclusionMap;Occlusion Map;5;0;Create;True;0;0;0;False;0;False;-1;None;030d9c1f37fa863459d7d360ebbe374d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldToObjectTransfNode;137;-889.9286,1066.249;Inherit;False;1;0;FLOAT4;0,0,0,1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;138;-1022.965,1085.837;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;146;-675.6288,996.0486;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SaturateNode;147;-769.7286,906.5483;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;149;-982.0558,892.7141;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;158;-1641.212,833.3729;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;159;-1497.212,817.3729;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;160;-1641.212,977.373;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;161;-1497.212,961.373;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosTime;162;-1897.212,817.3729;Inherit;True;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;164;-1655.631,1159.723;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;165;-1463.631,1143.723;Inherit;False;Simplex3D;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;166;-1463.631,1239.724;Inherit;False;Property;_WorldNoiseScale;WorldNoiseScale;26;0;Create;True;0;0;0;False;0;False;1;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;167;-1223.632,1223.724;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;168;-1223.632,1095.723;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;156;-1887.739,1257.586;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;157;-1887.739,1161.586;Inherit;False;Property;_Noisespeed;Noise Speed;24;0;Create;False;0;0;0;False;0;False;1;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;163;-1849.212,1009.373;Inherit;False;Property;_MotionValue;MotionValue (xy);25;0;Create;False;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;38;-1421.568,191.8361;Inherit;False;Property;_DetailNormalMapScale;DetailNormalMapScale;10;0;Create;True;0;0;0;False;0;False;1;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;35;-1435.232,277.8808;Inherit;True;Property;_DetailNormalMap;DetailNormalMap;9;0;Create;True;0;0;0;False;0;False;-1;None;e95f053a5488d944b8caa68a915cefea;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldPosInputsNode;131;-1866.018,-603.3877;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;132;-1662.018,-590.3878;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ToggleSwitchNode;136;-1511.062,-557.1708;Inherit;False;Property;_UVWorld;UVWorld;13;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;110;-1896.518,-450.1235;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;111;-2020.368,-549.8156;Inherit;False;Property;_TopTiling;TopTiling;12;0;Create;True;0;0;0;False;0;False;1;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;134;-1666.618,-673.6877;Inherit;False;Constant;_Float0;Float 0;26;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;133;-1501.718,-688.7878;Inherit;False;3;3;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;18;213.4965,-410.3837;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-314.5035,-1002.384;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;37;-298.5035,-858.3837;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;44;-90.50352,-858.3837;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;29;-106.5035,-586.3837;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;3;-410.5035,-522.3837;Inherit;True;Property;_AlbedoTop;AlbedoTop;18;0;Create;True;0;0;0;False;0;False;-1;None;384f9a1ad45e901438e288b77eb4a76d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;31;-362.5035,-682.3837;Inherit;False;Property;_ColorTop;ColorTop;11;0;Create;True;0;0;0;False;0;False;0.8490566,0.8450516,0.8450516,0;0.8490566,0.8450516,0.8450516,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;34;-954.5035,-890.3837;Inherit;True;Property;_DetailAlbedoMap;DetailAlbedoMap;8;0;Create;True;0;0;0;False;0;False;-1;None;efe0c35be306bf64dbda5b376fe717fa;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-666.5035,-906.3837;Inherit;True;Property;_MainTex;Albedo;1;0;Create;False;0;0;0;False;0;False;-1;None;efe0c35be306bf64dbda5b376fe717fa;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;30;-586.5035,-1082.384;Inherit;False;Property;_Color;Color;0;0;Create;True;0;0;0;False;0;False;0.8207547,0.8207547,0.8207547,0;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-1236.964,-182.4794;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;1;-1433.441,-230.6855;Inherit;False;True;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TFHCRemapNode;108;-1098.266,-316.8361;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-1380.565,-84.09618;Inherit;False;Property;_Ammount;Ammount;14;0;Create;True;0;0;0;False;0;False;0.5;12;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-669.6461,-252.1648;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;118;-730.7203,-133.9145;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;119;-880.0601,-96.36417;Inherit;False;Property;_Smooth;Smooth;15;0;Create;True;0;0;0;False;0;False;0.5;0.45;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;15;-873.6812,-309.6679;Inherit;False;True;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TFHCRemapNode;105;-514.0482,-209.3007;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;16;-57.18917,-78.33303;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;173;-481.1346,-12.02203;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;117;-1256.68,-323.1543;Inherit;False;Property;_Clamp;Clamp (xy);16;0;Create;False;0;0;0;False;0;False;0,1;0,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;169;-237.2812,-160.8124;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;5;-930.0342,447.7368;Inherit;True;Property;_BumpMapTop;BumpMapTop;19;0;Create;True;0;0;0;False;0;False;-1;None;2a2da984a6af369408b716da8bdd6296;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;178;-789.1295,28.18483;Inherit;False;Property;_TopVertical;Vertical Top Limit (xy);17;0;Create;False;0;0;0;False;0;False;0,0;2,-20;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;154;-1272.568,701.3282;Inherit;False;Property;_VerticalOffset;Vertical Offset (xy);27;0;Create;False;0;0;0;False;0;False;0,1;-0.5,2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;148;-1195.595,843.8022;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
WireConnection;47;0;5;0
WireConnection;47;1;4;0
WireConnection;130;0;18;0
WireConnection;130;1;11;0
WireConnection;130;4;19;0
WireConnection;130;5;54;0
WireConnection;130;11;146;0
WireConnection;27;1;24;0
WireConnection;21;1;23;0
WireConnection;19;0;21;0
WireConnection;19;1;27;0
WireConnection;19;2;16;0
WireConnection;39;0;4;0
WireConnection;39;1;35;0
WireConnection;46;0;4;0
WireConnection;46;1;39;0
WireConnection;46;2;43;4
WireConnection;120;0;5;0
WireConnection;120;1;47;0
WireConnection;120;2;121;0
WireConnection;11;0;46;0
WireConnection;11;1;120;0
WireConnection;11;2;108;0
WireConnection;4;5;12;0
WireConnection;59;0;53;0
WireConnection;58;0;51;0
WireConnection;58;1;59;0
WireConnection;54;0;58;0
WireConnection;51;0;48;1
WireConnection;51;1;56;0
WireConnection;51;2;16;0
WireConnection;56;0;48;1
WireConnection;56;1;57;0
WireConnection;57;0;52;0
WireConnection;137;0;138;0
WireConnection;138;0;168;0
WireConnection;138;2;167;0
WireConnection;146;1;137;0
WireConnection;146;2;147;0
WireConnection;147;0;149;0
WireConnection;149;0;148;2
WireConnection;149;3;154;1
WireConnection;149;4;154;2
WireConnection;158;0;163;1
WireConnection;159;0;163;1
WireConnection;159;1;158;0
WireConnection;159;2;162;3
WireConnection;160;0;163;2
WireConnection;161;0;163;2
WireConnection;161;1;160;0
WireConnection;161;2;162;2
WireConnection;164;0;156;0
WireConnection;164;2;157;0
WireConnection;165;0;164;0
WireConnection;165;1;166;0
WireConnection;167;0;165;0
WireConnection;167;1;161;0
WireConnection;168;0;165;0
WireConnection;168;1;159;0
WireConnection;35;5;38;0
WireConnection;132;0;131;1
WireConnection;132;1;131;3
WireConnection;136;0;110;0
WireConnection;136;1;133;0
WireConnection;110;0;111;0
WireConnection;133;0;132;0
WireConnection;133;1;111;0
WireConnection;133;2;134;0
WireConnection;18;0;44;0
WireConnection;18;1;29;0
WireConnection;18;2;16;0
WireConnection;28;0;30;0
WireConnection;28;1;2;0
WireConnection;37;0;2;0
WireConnection;37;1;34;0
WireConnection;37;2;30;0
WireConnection;44;0;28;0
WireConnection;44;1;37;0
WireConnection;44;2;43;4
WireConnection;29;0;31;0
WireConnection;29;1;3;0
WireConnection;3;1;136;0
WireConnection;9;0;1;2
WireConnection;9;1;8;0
WireConnection;108;0;9;0
WireConnection;108;1;117;1
WireConnection;108;2;117;2
WireConnection;17;0;118;0
WireConnection;17;1;8;0
WireConnection;118;0;15;2
WireConnection;118;2;119;0
WireConnection;15;0;11;0
WireConnection;105;0;17;0
WireConnection;105;1;117;1
WireConnection;105;2;117;2
WireConnection;16;0;169;0
WireConnection;173;0;148;2
WireConnection;173;3;178;1
WireConnection;173;4;178;2
WireConnection;169;0;105;0
WireConnection;169;1;173;0
WireConnection;5;1;136;0
WireConnection;5;5;13;0
ASEEND*/
//CHKSM=722025BFDA773E72315CE2B12AA146B7ACB185B4