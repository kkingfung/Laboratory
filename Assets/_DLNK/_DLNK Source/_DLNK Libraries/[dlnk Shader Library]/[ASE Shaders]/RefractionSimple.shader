// Made with Amplify Shader Editor v1.9.1.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DLNK Shaders/ASE/RefractionSimple"
{
	Properties
	{
		[HDR]_Color("Color Tint", Color) = (0.5773503,0.5773503,0.5773503,1)
		_MainTex("Albedo", 2D) = "white" {}
		_MetallicGlossMap("Metallic Gloss", 2D) = "white" {}
		_Metallic("Metallic", Float) = 0
		_Glossiness("Glossiness", Float) = 0.5
		_BumpMap("Normal", 2D) = "bump" {}
		_BumpScale("Bump Scale", Float) = 1
		_OclussionMap("Oclussion", 2D) = "white" {}
		_OcclusionStrength("Occlusion Strength", Range( 0 , 1)) = 0
		_DetailMask("Detail Mask", 2D) = "white" {}
		_DetailAlbedo("Detail Albedo", 2D) = "white" {}
		_DetailNormalMap("Detail Normal", 2D) = "bump" {}
		_DetailNormalMapScale("Detail Normal Map Scale", Float) = 1
		[Toggle]_DetailDistorsion("Detail Distorsion", Float) = 1
		[HDR]_ColorTintRefract("Color Tint (Refract)", Color) = (0.5773503,0.5773503,0.5773503,1)
		_Distorsion("Distorsion", Float) = 0.1
		[Toggle]_UseDetMask("Refract Detail Mask (A)", Float) = 1
		_RefractionMaskRemap("Refraction Mask Remap (xy)", Vector) = (0,1,0,0)
		[Toggle]_RefractDepth("Refract Depth", Float) = 1
		_Depth("Depth", Vector) = (0,0,0,0)
		[HDR]_Color1("Color Far", Color) = (0.5773503,0.5773503,0.5773503,1)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
		[Header(Forward Rendering Options)]
		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections("Reflections", Float) = 1.0
	}

	SubShader
	{
		Pass
		{
			ColorMask 0
			ZTest Equal
			ZWrite On
		}

		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		GrabPass{ }
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma shader_feature _SPECULARHIGHLIGHTS_OFF
		#pragma shader_feature _GLOSSYREFLECTIONS_OFF
		#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
		#else
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
		#endif
		#pragma surface surf Standard keepalpha dithercrossfade 
		struct Input
		{
			float2 uv_texcoord;
			float4 screenPos;
		};

		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform float _BumpScale;
		uniform sampler2D _DetailNormalMap;
		uniform float4 _DetailNormalMap_ST;
		uniform float _DetailNormalMapScale;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _DetailAlbedo;
		uniform float4 _DetailAlbedo_ST;
		uniform sampler2D _DetailMask;
		uniform float4 _DetailMask_ST;
		uniform float4 _Color;
		uniform float4 _Color1;
		uniform float _RefractDepth;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float2 _Depth;
		ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
		uniform float _DetailDistorsion;
		uniform float _Distorsion;
		uniform float4 _ColorTintRefract;
		uniform float _UseDetMask;
		uniform float2 _RefractionMaskRemap;
		uniform sampler2D _MetallicGlossMap;
		uniform float4 _MetallicGlossMap_ST;
		uniform float _Metallic;
		uniform float _Glossiness;
		uniform sampler2D _OclussionMap;
		uniform float4 _OclussionMap_ST;
		uniform float _OcclusionStrength;


		inline float4 ASE_ComputeGrabScreenPos( float4 pos )
		{
			#if UNITY_UV_STARTS_AT_TOP
			float scale = -1.0;
			#else
			float scale = 1.0;
			#endif
			float4 o = pos;
			o.y = pos.w * 0.5f;
			o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
			return o;
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_BumpMap = i.uv_texcoord * _BumpMap_ST.xy + _BumpMap_ST.zw;
			float3 tex2DNode2 = UnpackScaleNormal( tex2D( _BumpMap, uv_BumpMap ), _BumpScale );
			float2 uv_DetailNormalMap = i.uv_texcoord * _DetailNormalMap_ST.xy + _DetailNormalMap_ST.zw;
			float3 temp_output_8_0 = BlendNormals( tex2DNode2 , UnpackScaleNormal( tex2D( _DetailNormalMap, uv_DetailNormalMap ), _DetailNormalMapScale ) );
			o.Normal = temp_output_8_0;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float2 uv_DetailAlbedo = i.uv_texcoord * _DetailAlbedo_ST.xy + _DetailAlbedo_ST.zw;
			float2 uv_DetailMask = i.uv_texcoord * _DetailMask_ST.xy + _DetailMask_ST.zw;
			float4 tex2DNode6 = tex2D( _DetailMask, uv_DetailMask );
			float temp_output_2_0_g5 = tex2DNode6.r;
			float temp_output_3_0_g5 = ( 1.0 - temp_output_2_0_g5 );
			float3 appendResult7_g5 = (float3(temp_output_3_0_g5 , temp_output_3_0_g5 , temp_output_3_0_g5));
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float eyeDepth47 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float temp_output_7_0_g6 = _Depth.x;
			float4 lerpResult57 = lerp( _Color , _Color1 , (( _RefractDepth )?( saturate( ( ( eyeDepth47 - temp_output_7_0_g6 ) / ( _Depth.y - temp_output_7_0_g6 ) ) ) ):( 0.0 )));
			float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ase_screenPos );
			float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
			float4 screenColor31 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( float3( (ase_grabScreenPosNorm).xy ,  0.0 ) + ( (( _DetailDistorsion )?( temp_output_8_0 ):( tex2DNode2 )) * _Distorsion ) ).xy);
			float temp_output_7_0_g4 = _RefractionMaskRemap.x;
			float4 lerpResult34 = lerp( ( tex2D( _MainTex, uv_MainTex ) * float4( ( ( tex2D( _DetailAlbedo, uv_DetailAlbedo ).rgb * temp_output_2_0_g5 ) + appendResult7_g5 ) , 0.0 ) * lerpResult57 ) , ( screenColor31 * _ColorTintRefract ) , ( (( _UseDetMask )?( saturate( ( ( tex2DNode6.a - temp_output_7_0_g4 ) / ( _RefractionMaskRemap.y - temp_output_7_0_g4 ) ) ) ):( 0.5 )) * ( 1.0 - (( _RefractDepth )?( saturate( ( ( eyeDepth47 - temp_output_7_0_g6 ) / ( _Depth.y - temp_output_7_0_g6 ) ) ) ):( 0.0 )) ) ));
			o.Albedo = lerpResult34.rgb;
			float2 uv_MetallicGlossMap = i.uv_texcoord * _MetallicGlossMap_ST.xy + _MetallicGlossMap_ST.zw;
			float4 tex2DNode3 = tex2D( _MetallicGlossMap, uv_MetallicGlossMap );
			o.Metallic = saturate( ( tex2DNode3 * _Metallic ) ).r;
			o.Smoothness = saturate( ( tex2DNode3.a * _Glossiness ) );
			float2 uv_OclussionMap = i.uv_texcoord * _OclussionMap_ST.xy + _OclussionMap_ST.zw;
			float temp_output_2_0_g2 = _OcclusionStrength;
			float temp_output_3_0_g2 = ( 1.0 - temp_output_2_0_g2 );
			float3 appendResult7_g2 = (float3(temp_output_3_0_g2 , temp_output_3_0_g2 , temp_output_3_0_g2));
			o.Occlusion = ( ( tex2D( _OclussionMap, uv_OclussionMap ).rgb * temp_output_2_0_g2 ) + appendResult7_g2 ).x;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19103
Node;AmplifyShaderEditor.BlendNormalsNode;8;-617.0845,305.8663;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-246.7479,89.86658;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;-241.8638,192.4345;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;23;-229.3366,440.8794;Inherit;False;Lerp White To;-1;;2;047d7c189c36a62438973bad9d37b1c2;0;2;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;3;-709.0209,85.48942;Inherit;True;Property;_MetallicGlossMap;Metallic Gloss;2;0;Create;False;0;0;0;False;0;False;-1;None;29f1905217b55454bb6d2444bf8ef1f6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;7;-971,319.5;Inherit;True;Property;_DetailNormalMap;Detail Normal;11;0;Create;False;0;0;0;False;0;False;-1;None;8f925442ab1daa748808b04b489fe0e7;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-1031.039,106.9342;Inherit;True;Property;_BumpMap;Normal;5;0;Create;False;0;0;0;False;0;False;-1;None;98b5a9cf13f1efb43b3e5822a713b7bc;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;9;-1181.258,198.0927;Inherit;False;Property;_BumpScale;Bump Scale;6;0;Create;True;0;0;0;False;0;False;1;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;10;-1244.78,389.3452;Inherit;False;Property;_DetailNormalMapScale;Detail Normal Map Scale;12;0;Create;True;0;0;0;False;0;False;1;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;4;-634.3026,464.2763;Inherit;True;Property;_OclussionMap;Oclussion;7;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;12;-514.0845,690.8663;Inherit;False;Property;_OcclusionStrength;Occlusion Strength;8;0;Create;True;0;0;0;False;0;False;0;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-327.0427,-242.093;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;-190.2336,-528.4765;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;101.2374,35.80035;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;DLNK Shaders/ASE/RefractionSimple;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;True;True;False;Back;0;False;;0;False;;False;0;False;;0;False;;True;5;Opaque;5;True;False;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;5;True;0;5;False;;10;False;;0;1;False;;10;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-394.9212,294.9993;Inherit;False;Property;_Glossiness;Glossiness;4;0;Create;True;0;0;0;False;0;False;0.5;0.76;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-406.4114,35.30721;Inherit;False;Property;_Metallic;Metallic;3;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;43;-103.1818,57.78241;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;44;-75.07661,177.2295;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;34;-109.3757,-302.6205;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector2Node;40;-1799.82,-718.8273;Inherit;False;Property;_RefractionMaskRemap;Refraction Mask Remap (xy);17;0;Create;False;0;0;0;False;0;False;0,1;0.2,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;6;-1857.328,-564.923;Inherit;True;Property;_DetailMask;Detail Mask;9;0;Create;True;0;0;0;False;0;False;-1;None;29f1905217b55454bb6d2444bf8ef1f6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;39;-1530.409,-704.7238;Inherit;False;Remap To 0-1;-1;;4;e6e209ac370e7e74da13a6a97e315390;0;3;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;38;-1490.669,-568.8364;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;33;-1534.235,-481.1404;Inherit;False;Property;_UseDetMask;Refract Detail Mask (A);16;0;Create;False;0;0;0;False;0;False;1;True;2;0;FLOAT;0.5;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GrabScreenPosition;26;-1262.472,-843.1511;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-969.1601,-739.4808;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;29;-1046.281,-822.9227;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;30;-801.012,-802.6942;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-1157.537,-664.8886;Inherit;False;Property;_Distorsion;Distorsion;15;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenColorNode;31;-672.9754,-806.16;Inherit;False;Global;_GrabScreen1;Grab Screen 1;9;0;Create;True;0;0;0;False;0;False;Object;-1;False;False;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;1;-709.4554,-535.7344;Inherit;True;Property;_MainTex;Albedo;1;0;Create;False;0;0;0;False;0;False;-1;None;eb3e237ea9444864299c696c46b1e1d7;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;57;-879.0186,-312.733;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;5;-1533.798,-180.2555;Inherit;True;Property;_DetailAlbedo;Detail Albedo;10;0;Create;True;0;0;0;False;0;False;-1;None;0c8795e43e7063a49966f20be610c89a;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;20;-704.4484,-128.609;Inherit;False;Lerp White To;-1;;5;047d7c189c36a62438973bad9d37b1c2;0;2;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;58;-618.0198,-1187.181;Inherit;False;Remap To 0-1;-1;;6;e6e209ac370e7e74da13a6a97e315390;0;3;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenDepthNode;47;-847.1882,-1246.433;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;51;-828.5166,-1151.473;Inherit;False;Property;_Depth;Depth;19;0;Create;True;0;0;0;False;0;False;0,0;10,90;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SaturateNode;53;-447.5647,-1121.018;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;55;-315.7523,-430.3458;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;59;-456.6367,-863.9084;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;54;-584.0867,-979.4812;Inherit;False;Property;_RefractDepth;Refract Depth;18;0;Create;True;0;0;0;False;0;False;1;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;60;-1049.427,-536.4893;Inherit;False;Property;_DetailDistorsion;Detail Distorsion;13;0;Create;True;0;0;0;False;0;False;1;True;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;56;-1097.374,-193.3624;Inherit;False;Property;_Color1;Color Far;20;1;[HDR];Create;False;0;0;0;False;0;False;0.5773503,0.5773503,0.5773503,1;0.5016826,0.563534,0.6563109,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;21;-1098.58,-368.2403;Inherit;False;Property;_Color;Color Tint;0;1;[HDR];Create;False;0;0;0;False;0;False;0.5773503,0.5773503,0.5773503,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;42;-368.4091,-779.1508;Inherit;False;Property;_ColorTintRefract;Color Tint (Refract);14;1;[HDR];Create;True;0;0;0;False;0;False;0.5773503,0.5773503,0.5773503,1;0.5773503,0.5773503,0.5773503,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
WireConnection;8;0;2;0
WireConnection;8;1;7;0
WireConnection;14;0;3;0
WireConnection;14;1;13;0
WireConnection;15;0;3;4
WireConnection;15;1;11;0
WireConnection;23;1;4;0
WireConnection;23;2;12;0
WireConnection;7;5;10;0
WireConnection;2;5;9;0
WireConnection;19;0;1;0
WireConnection;19;1;20;0
WireConnection;19;2;57;0
WireConnection;41;0;31;0
WireConnection;41;1;42;0
WireConnection;0;0;34;0
WireConnection;0;1;8;0
WireConnection;0;3;43;0
WireConnection;0;4;44;0
WireConnection;0;5;23;0
WireConnection;43;0;14;0
WireConnection;44;0;15;0
WireConnection;34;0;19;0
WireConnection;34;1;41;0
WireConnection;34;2;55;0
WireConnection;39;6;6;4
WireConnection;39;7;40;1
WireConnection;39;8;40;2
WireConnection;38;0;39;0
WireConnection;33;1;38;0
WireConnection;28;0;60;0
WireConnection;28;1;27;0
WireConnection;29;0;26;0
WireConnection;30;0;29;0
WireConnection;30;1;28;0
WireConnection;31;0;30;0
WireConnection;57;0;21;0
WireConnection;57;1;56;0
WireConnection;57;2;54;0
WireConnection;20;1;5;0
WireConnection;20;2;6;0
WireConnection;58;6;47;0
WireConnection;58;7;51;1
WireConnection;58;8;51;2
WireConnection;53;0;58;0
WireConnection;55;0;33;0
WireConnection;55;1;59;0
WireConnection;59;0;54;0
WireConnection;54;1;53;0
WireConnection;60;0;2;0
WireConnection;60;1;8;0
ASEEND*/
//CHKSM=1B7AFEBB7672E295880C75DB393C7C6CED02869E