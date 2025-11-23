// Made with Amplify Shader Editor v1.9.1.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DLNK Shaders/ASE/Nature/WaterRunning"
{
	Properties
	{
		_TessPhongStrength( "Phong Tess Strength", Range( 0, 1 ) ) = 0.897
		_ColorA("Color A", Color) = (1,1,1,0)
		_ColorB("Color B", Color) = (0,0,0,0)
		_ColorMix("Color Mix", Float) = 1
		_MainTex("Albedo", 2D) = "white" {}
		_MainTex1("Albedo B", 2D) = "white" {}
		_Metalness("Metalness", Float) = 0.5
		_Smoothness("Smoothness", Float) = 0.5
		_BumpMap("Normal A", 2D) = "bump" {}
		_BumpMap1("Normal B", 2D) = "bump" {}
		_NormalScale("NormalScale", Float) = 1
		[Header(Refraction)]
		_ChromaticAberration("Chromatic Aberration", Range( 0 , 0.3)) = 0.1
		_RefractionLevel("Refraction Level", Float) = 1
		_Transparency("Transparency", Float) = 1
		_Tessellation("Tessellation", Range( 0 , 20)) = 1
		_Displacement("Displacement", Float) = 1
		_Offset("Offset", Float) = 1
		_Speed("Speed", Float) = 1
		_SpeedAB("Speed A (XY) B (ZW)", Vector) = (0,1,0,0.5)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		GrabPass{ }
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardUtils.cginc"
		#include "Tessellation.cginc"
		#pragma target 4.6
		#pragma multi_compile _ALPHAPREMULTIPLY_ON
		#pragma surface surf Standard keepalpha finalcolor:RefractionF exclude_path:deferred vertex:vertexDataFunc tessellate:tessFunction tessphong:_TessPhongStrength 
		struct Input
		{
			float2 uv_texcoord;
			float4 screenPos;
			float3 worldPos;
		};

		uniform sampler2D _MainTex;
		uniform float _Speed;
		uniform float4 _SpeedAB;
		uniform sampler2D _MainTex1;
		uniform float _Displacement;
		uniform sampler2D _BumpMap;
		uniform float _NormalScale;
		uniform sampler2D _BumpMap1;
		uniform float _Offset;
		uniform float4 _ColorA;
		uniform float4 _ColorB;
		uniform float _ColorMix;
		uniform float _Metalness;
		uniform float _Smoothness;
		uniform float _Transparency;
		uniform sampler2D _GrabTexture;
		uniform float _ChromaticAberration;
		uniform float _RefractionLevel;
		uniform float _Tessellation;
		uniform float _TessPhongStrength;

		float4 tessFunction( appdata_full v0, appdata_full v1, appdata_full v2 )
		{
			float4 temp_cast_3 = (_Tessellation).xxxx;
			return temp_cast_3;
		}

		void vertexDataFunc( inout appdata_full v )
		{
			float mulTime11 = _Time.y * _Speed;
			float2 appendResult37 = (float2(_SpeedAB.x , _SpeedAB.y));
			float2 panner5 = ( mulTime11 * appendResult37 + v.texcoord.xy);
			float2 appendResult38 = (float2(_SpeedAB.z , _SpeedAB.w));
			float2 panner32 = ( mulTime11 * appendResult38 + v.texcoord.xy);
			float4 lerpResult39 = lerp( tex2Dlod( _MainTex, float4( panner5, 0, 0.0) ) , tex2Dlod( _MainTex1, float4( panner32, 0, 0.0) ) , float4( 0.5,0,0,0 ));
			float3 temp_output_31_0 = BlendNormals( UnpackScaleNormal( tex2Dlod( _BumpMap, float4( panner5, 0, 0.0) ), _NormalScale ) , UnpackScaleNormal( tex2Dlod( _BumpMap1, float4( panner32, 0, 0.0) ), _NormalScale ) );
			float3 ase_worldNormal = UnityObjectToWorldNormal( v.normal );
			float3 ase_worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
			float3x3 tangentToWorld = CreateTangentToWorldPerVertex( ase_worldNormal, ase_worldTangent, v.tangent.w );
			float3 tangentNormal55 = temp_output_31_0;
			float3 modWorldNormal55 = normalize( (tangentToWorld[0] * tangentNormal55.x + tangentToWorld[1] * tangentNormal55.y + tangentToWorld[2] * tangentNormal55.z) );
			v.vertex.xyz += ( ( lerpResult39 * _Displacement * float4( modWorldNormal55 , 0.0 ) ) + float4( ( modWorldNormal55 * _Offset ) , 0.0 ) ).rgb;
			v.vertex.w = 1;
		}

		inline float4 Refraction( Input i, SurfaceOutputStandard o, float indexOfRefraction, float chomaticAberration ) {
			float3 worldNormal = o.Normal;
			float4 screenPos = i.screenPos;
			#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
			#else
				float scale = 1.0;
			#endif
			float halfPosW = screenPos.w * 0.5;
			screenPos.y = ( screenPos.y - halfPosW ) * _ProjectionParams.x * scale + halfPosW;
			#if SHADER_API_D3D9 || SHADER_API_D3D11
				screenPos.w += 0.00000000001;
			#endif
			float2 projScreenPos = ( screenPos / screenPos.w ).xy;
			float3 worldViewDir = normalize( UnityWorldSpaceViewDir( i.worldPos ) );
			float3 refractionOffset = ( indexOfRefraction - 1.0 ) * mul( UNITY_MATRIX_V, float4( worldNormal, 0.0 ) ) * ( 1.0 - dot( worldNormal, worldViewDir ) );
			float2 cameraRefraction = float2( refractionOffset.x, refractionOffset.y );
			float4 redAlpha = tex2D( _GrabTexture, ( projScreenPos + cameraRefraction ) );
			float green = tex2D( _GrabTexture, ( projScreenPos + ( cameraRefraction * ( 1.0 - chomaticAberration ) ) ) ).g;
			float blue = tex2D( _GrabTexture, ( projScreenPos + ( cameraRefraction * ( 1.0 + chomaticAberration ) ) ) ).b;
			return float4( redAlpha.r, green, blue, redAlpha.a );
		}

		void RefractionF( Input i, SurfaceOutputStandard o, inout half4 color )
		{
			#ifdef UNITY_PASS_FORWARDBASE
			color.rgb = color.rgb + Refraction( i, o, _RefractionLevel, _ChromaticAberration ) * ( 1 - color.a );
			color.a = 1;
			#endif
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			float mulTime11 = _Time.y * _Speed;
			float2 appendResult37 = (float2(_SpeedAB.x , _SpeedAB.y));
			float2 panner5 = ( mulTime11 * appendResult37 + i.uv_texcoord);
			float2 appendResult38 = (float2(_SpeedAB.z , _SpeedAB.w));
			float2 panner32 = ( mulTime11 * appendResult38 + i.uv_texcoord);
			float3 temp_output_31_0 = BlendNormals( UnpackScaleNormal( tex2D( _BumpMap, panner5 ), _NormalScale ) , UnpackScaleNormal( tex2D( _BumpMap1, panner32 ), _NormalScale ) );
			o.Normal = temp_output_31_0;
			float4 lerpResult12 = lerp( _ColorA , _ColorB , ( i.uv_texcoord.x * _ColorMix ));
			float4 lerpResult39 = lerp( tex2D( _MainTex, panner5 ) , tex2D( _MainTex1, panner32 ) , float4( 0.5,0,0,0 ));
			float4 temp_output_13_0 = ( lerpResult12 * lerpResult39 );
			o.Albedo = temp_output_13_0.rgb;
			o.Metallic = saturate( ( (lerpResult39).a * _Metalness ) );
			o.Smoothness = saturate( ( (lerpResult39).a * _Smoothness ) );
			o.Alpha = saturate( ( (temp_output_13_0).a * _Transparency ) );
			o.Normal = o.Normal + 0.00001 * i.screenPos * i.worldPos;
		}

		ENDCG
	}
	Fallback "DLNK Shaders/ASE/Nature/WaterSimple"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19102
Node;AmplifyShaderEditor.RangedFloatNode;6;-889.3884,260.3493;Inherit;False;Property;_Speed;Speed;17;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;34;-1258.388,212.3493;Inherit;False;Property;_SpeedAB;Speed A (XY) B (ZW);18;0;Create;False;0;0;0;False;0;False;0,1,0,0.5;-0.8,-0.2,-0.5,0.2;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;38;-1041.388,307.3493;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;37;-1040.388,214.3493;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;11;-883.3884,187.3493;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;1;-1031,69.5;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;5;-816.3884,71.3493;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;32;-891.3884,375.3493;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-834,-164.5;Inherit;False;Property;_ColorMix;Color Mix;2;0;Create;True;0;0;0;False;0;False;1;1.54;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;28;-888.3884,507.3493;Inherit;False;Property;_NormalScale;NormalScale;9;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;15;-390.3884,-415.6507;Inherit;False;Property;_ColorB;Color B;1;0;Create;True;0;0;0;False;0;False;0,0,0,0;0.1604218,0.4437116,0.9716981,0.1921569;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;2;-651,-179.5;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;9;-577.3884,-76.6507;Inherit;True;Property;_MainTex;Albedo;3;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;14;-607.3884,-414.6507;Inherit;False;Property;_ColorA;Color A;0;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;29;-580.3884,124.3493;Inherit;True;Property;_MainTex1;Albedo B;4;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;24;-684.3884,351.3493;Inherit;True;Property;_BumpMap;Normal A;7;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;30;-685.3884,552.3493;Inherit;True;Property;_BumpMap1;Normal B;8;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;39;-256.7491,-133.4002;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0.5,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;12;-379.3884,-228.6507;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.BlendNormalsNode;31;-358.3884,353.3493;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-87.38843,-212.6507;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;43;66.25085,358.5999;Inherit;False;Property;_Displacement;Displacement;15;0;Create;True;0;0;0;False;0;False;1;0.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-306.5418,719.7498;Inherit;False;Property;_Smoothness;Smoothness;6;0;Create;True;0;0;0;False;0;False;0.5;8.8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;51;-298.988,889.1768;Inherit;False;Property;_Metalness;Metalness;5;0;Create;True;0;0;0;False;0;False;0.5;0.49;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;54;68.6438,439.0839;Inherit;False;Property;_Offset;Offset;16;0;Create;True;0;0;0;False;0;False;1;-0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;50;-332.4415,807.1611;Inherit;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-206.3884,140.3493;Inherit;False;Property;_Transparency;Transparency;13;0;Create;True;0;0;0;False;0;False;1;0.22;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;55;-170.3562,404.0839;Inherit;False;True;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;18;-221.3884,62.3493;Inherit;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;47;-319.4921,639.8922;Inherit;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;-104.7405,809.3195;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;56;469.6438,412.0838;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-29.38843,98.3493;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;-115.2867,709.4412;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;42;232.2509,260.5998;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;40;108.2509,127.5998;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-101.7491,257.5998;Inherit;False;Property;_RefractionLevel;Refraction Level;12;0;Create;True;0;0;0;False;0;False;1;0.95;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;52;-110.1363,906.4429;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;45;-119.0163,627.539;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;53;284.6438,389.0839;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;44;74.25085,588.5999;Inherit;False;Property;_Tessellation;Tessellation;14;0;Create;True;0;0;0;False;0;False;1;2;0;20;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;442,-42;Float;False;True;-1;6;ASEMaterialInspector;0;0;Standard;DLNK Shaders/ASE/Nature/WaterRunning;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Translucent;0.5;True;False;0;False;Opaque;;Transparent;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;True;2;15;10;25;True;0.897;True;0;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;DLNK Shaders/ASE/Nature/WaterSimple;-1;-1;10;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;38;0;34;3
WireConnection;38;1;34;4
WireConnection;37;0;34;1
WireConnection;37;1;34;2
WireConnection;11;0;6;0
WireConnection;5;0;1;0
WireConnection;5;2;37;0
WireConnection;5;1;11;0
WireConnection;32;0;1;0
WireConnection;32;2;38;0
WireConnection;32;1;11;0
WireConnection;2;0;1;1
WireConnection;2;1;4;0
WireConnection;9;1;5;0
WireConnection;29;1;32;0
WireConnection;24;1;5;0
WireConnection;24;5;28;0
WireConnection;30;1;32;0
WireConnection;30;5;28;0
WireConnection;39;0;9;0
WireConnection;39;1;29;0
WireConnection;12;0;14;0
WireConnection;12;1;15;0
WireConnection;12;2;2;0
WireConnection;31;0;24;0
WireConnection;31;1;30;0
WireConnection;13;0;12;0
WireConnection;13;1;39;0
WireConnection;50;0;39;0
WireConnection;55;0;31;0
WireConnection;18;0;13;0
WireConnection;47;0;39;0
WireConnection;49;0;50;0
WireConnection;49;1;51;0
WireConnection;56;0;55;0
WireConnection;56;1;54;0
WireConnection;16;0;18;0
WireConnection;16;1;17;0
WireConnection;46;0;47;0
WireConnection;46;1;48;0
WireConnection;42;0;39;0
WireConnection;42;1;43;0
WireConnection;42;2;55;0
WireConnection;40;0;16;0
WireConnection;52;0;49;0
WireConnection;45;0;46;0
WireConnection;53;0;42;0
WireConnection;53;1;56;0
WireConnection;0;0;13;0
WireConnection;0;1;31;0
WireConnection;0;3;52;0
WireConnection;0;4;45;0
WireConnection;0;8;41;0
WireConnection;0;9;40;0
WireConnection;0;11;53;0
WireConnection;0;14;44;0
ASEEND*/
//CHKSM=825CF27DB02F8ADBF0C4E76490C9750B42D43F76