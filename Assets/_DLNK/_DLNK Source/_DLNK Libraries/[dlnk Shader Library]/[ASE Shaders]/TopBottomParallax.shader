// Made with Amplify Shader Editor v1.9.3.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DLNK Shaders/ASE/TopBottomParallax"
{
	Properties
	{
		_Color("Color", Color) = (0.8207547,0.8207547,0.8207547,0)
		_Tiling("Tiling", Float) = 1
		_MainTex("Albedo", 2D) = "white" {}
		_BumpMap("BumpMap", 2D) = "bump" {}
		_BumpScale("BumpScale", Float) = 1
		_MetallicGlossMap("MetallicGlossMap", 2D) = "white" {}
		_Metallic("Metallic", Float) = 0
		_Glossiness("Glossiness", Float) = 0.5
		_OcclusionMap("Occlusion Map", 2D) = "white" {}
		_OcclusionStrength("Occlusion Strength", Float) = 0
		[Toggle]_Emission("Emission", Float) = 0
		[HDR]_EmissionColor("Emission Color", Color) = (0,0,0,0)
		_EmissionMap("Emission Map", 2D) = "white" {}
		_ParallaxMap("Parallax Map", 2D) = "white" {}
		_Parallax("Parallax", Range( 0 , 1)) = 0.4247461
		_Curvature("Curvature (xy)", Vector) = (0.5,0,0,0)
		_Samplesxy("Samples (xy)", Vector) = (2,30,0,0)
		_ColorTop("ColorTop", Color) = (0.8490566,0.8450516,0.8450516,0)
		_TopTiling("TopTiling", Float) = 1
		[Toggle]_UVWorld("UVWorld", Float) = 0
		_Ammount("Ammount", Float) = 0.5
		_Smooth("Smooth", Float) = 0.5
		_Clamp("Clamp", Vector) = (0,1,0,0)
		[Toggle]_UseHeight("UseHeight", Float) = 1
		_HeightMask("Height Mask", Float) = 1
		_MaskPower("Mask Power", Float) = 1
		_AlbedoTop("AlbedoTop", 2D) = "white" {}
		_BumpMapTop("BumpMapTop", 2D) = "bump" {}
		_BumpScaleTop("BumpScaleTop", Float) = 1
		_MetalnessTop("MetalnessTop", 2D) = "white" {}
		_GlossinessTop("GlossinessTop", Float) = 0.5
		_MetallicTop("MetallicTop", Float) = 0
		_OcclusionTop("Occlusion Top", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
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
			float2 uv_texcoord;
			float3 viewDir;
			INTERNAL_DATA
			float3 worldNormal;
			float3 worldPos;
		};

		uniform sampler2D _BumpMap;
		uniform float _Tiling;
		uniform sampler2D _ParallaxMap;
		uniform float _Parallax;
		uniform float2 _Samplesxy;
		uniform float2 _Curvature;
		uniform float4 _ParallaxMap_ST;
		uniform float _BumpScale;
		uniform sampler2D _BumpMapTop;
		uniform float _UVWorld;
		uniform float _TopTiling;
		uniform float _BumpScaleTop;
		uniform float _UseHeight;
		uniform float _Smooth;
		uniform float _Ammount;
		uniform float2 _Clamp;
		uniform float _MaskPower;
		uniform float _HeightMask;
		uniform float4 _Color;
		uniform sampler2D _MainTex;
		uniform float4 _ColorTop;
		uniform sampler2D _AlbedoTop;
		uniform float _Emission;
		uniform sampler2D _EmissionMap;
		uniform float4 _EmissionColor;
		uniform sampler2D _MetallicGlossMap;
		uniform float _Metallic;
		uniform sampler2D _MetalnessTop;
		uniform float _MetallicTop;
		uniform float _Glossiness;
		uniform float _GlossinessTop;
		uniform sampler2D _OcclusionMap;
		uniform float _OcclusionTop;
		uniform float _OcclusionStrength;


		inline float2 POM( sampler2D heightMap, float2 uvs, float2 dx, float2 dy, float3 normalWorld, float3 viewWorld, float3 viewDirTan, int minSamples, int maxSamples, int sidewallSteps, float parallax, float refPlane, float2 tilling, float2 curv, int index )
		{
			float3 result = 0;
			int stepIndex = 0;
			int numSteps = ( int )lerp( (float)maxSamples, (float)minSamples, saturate( dot( normalWorld, viewWorld ) ) );
			float layerHeight = 1.0 / numSteps;
			float2 plane = parallax * ( viewDirTan.xy / viewDirTan.z );
			uvs.xy += refPlane * plane;
			float2 deltaTex = -plane * layerHeight;
			float2 prevTexOffset = 0;
			float prevRayZ = 1.0f;
			float prevHeight = 0.0f;
			float2 currTexOffset = deltaTex;
			float currRayZ = 1.0f - layerHeight;
			float currHeight = 0.0f;
			float intersection = 0;
			float2 finalTexOffset = 0;
			while ( stepIndex < numSteps + 1 )
			{
			 	currHeight = tex2Dgrad( heightMap, uvs + currTexOffset, dx, dy ).r;
			 	if ( currHeight > currRayZ )
			 	{
			 	 	stepIndex = numSteps + 1;
			 	}
			 	else
			 	{
			 	 	stepIndex++;
			 	 	prevTexOffset = currTexOffset;
			 	 	prevRayZ = currRayZ;
			 	 	prevHeight = currHeight;
			 	 	currTexOffset += deltaTex;
			 	 	currRayZ -= layerHeight;
			 	}
			}
			int sectionSteps = sidewallSteps;
			int sectionIndex = 0;
			float newZ = 0;
			float newHeight = 0;
			while ( sectionIndex < sectionSteps )
			{
			 	intersection = ( prevHeight - prevRayZ ) / ( prevHeight - currHeight + currRayZ - prevRayZ );
			 	finalTexOffset = prevTexOffset + intersection * deltaTex;
			 	newZ = prevRayZ - intersection * layerHeight;
			 	newHeight = tex2Dgrad( heightMap, uvs + finalTexOffset, dx, dy ).r;
			 	if ( newHeight > newZ )
			 	{
			 	 	currTexOffset = finalTexOffset;
			 	 	currHeight = newHeight;
			 	 	currRayZ = newZ;
			 	 	deltaTex = intersection * deltaTex;
			 	 	layerHeight = intersection * layerHeight;
			 	}
			 	else
			 	{
			 	 	prevTexOffset = finalTexOffset;
			 	 	prevHeight = newHeight;
			 	 	prevRayZ = newZ;
			 	 	deltaTex = ( 1 - intersection ) * deltaTex;
			 	 	layerHeight = ( 1 - intersection ) * layerHeight;
			 	}
			 	sectionIndex++;
			}
			return uvs.xy + finalTexOffset;
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 temp_cast_0 = (_Tiling).xx;
			float2 uv_TexCoord151 = i.uv_texcoord * temp_cast_0;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float2 OffsetPOM149 = POM( _ParallaxMap, uv_TexCoord151, ddx(uv_TexCoord151), ddy(uv_TexCoord151), ase_worldNormal, ase_worldViewDir, i.viewDir, (int)_Samplesxy.x, (int)_Samplesxy.y, 2, _Parallax, _Curvature.x, _ParallaxMap_ST.xy, float2(0,0), 0 );
			float2 customUVs148 = OffsetPOM149;
			float2 temp_output_147_0 = ddx( uv_TexCoord151 );
			float2 temp_output_146_0 = ddy( uv_TexCoord151 );
			float3 tex2DNode4 = UnpackScaleNormal( tex2D( _BumpMap, customUVs148, temp_output_147_0, temp_output_146_0 ), _BumpScale );
			float2 temp_cast_4 = (_TopTiling).xx;
			float2 uv_TexCoord110 = i.uv_texcoord * temp_cast_4;
			float4 appendResult132 = (float4(ase_worldPos.x , ase_worldPos.z , 0.0 , 0.0));
			float smoothstepResult118 = smoothstep( 0.0 , _Smooth , normalize( (WorldNormalVector( i , tex2DNode4 )) ).y);
			float temp_output_105_0 = (0.0 + (( smoothstepResult118 * _Ammount ) - _Clamp.x) * (1.0 - 0.0) / (_Clamp.y - _Clamp.x));
			float blendOpSrc165 = pow( ( 1.0 - tex2D( _ParallaxMap, customUVs148, temp_output_147_0, temp_output_146_0 ).r ) , _MaskPower );
			float blendOpDest165 = temp_output_105_0;
			float lerpBlendMode165 = lerp(blendOpDest165, (( blendOpSrc165 > 0.5 ) ? ( 1.0 - ( 1.0 - 2.0 * ( blendOpSrc165 - 0.5 ) ) * ( 1.0 - blendOpDest165 ) ) : ( 2.0 * blendOpSrc165 * blendOpDest165 ) ),_HeightMask);
			float temp_output_16_0 = saturate( (( _UseHeight )?( ( saturate( lerpBlendMode165 )) ):( temp_output_105_0 )) );
			float3 lerpResult11 = lerp( tex2DNode4 , UnpackScaleNormal( tex2D( _BumpMapTop, (( _UVWorld )?( ( appendResult132 * _TopTiling * 0.1 ) ):( float4( uv_TexCoord110, 0.0 , 0.0 ) )).xy, ddx( uv_TexCoord110 ), ddy( uv_TexCoord110 ) ), _BumpScaleTop ) , temp_output_16_0);
			o.Normal = lerpResult11;
			float4 tex2DNode2 = tex2D( _MainTex, customUVs148, temp_output_147_0, temp_output_146_0 );
			float4 lerpResult44 = lerp( ( _Color * tex2DNode2 ) , tex2DNode2 , float4( 0,0,0,0 ));
			float4 lerpResult18 = lerp( lerpResult44 , ( _ColorTop * tex2D( _AlbedoTop, (( _UVWorld )?( ( appendResult132 * _TopTiling * 0.1 ) ):( float4( uv_TexCoord110, 0.0 , 0.0 ) )).xy ) ) , temp_output_16_0);
			o.Albedo = lerpResult18.rgb;
			o.Emission = (( _Emission )?( ( tex2D( _EmissionMap, customUVs148, temp_output_147_0, temp_output_146_0 ) * _EmissionColor ) ):( float4( 0,0,0,0 ) )).rgb;
			float4 tex2DNode6 = tex2D( _MetallicGlossMap, customUVs148, temp_output_147_0, temp_output_146_0 );
			float4 tex2DNode7 = tex2D( _MetalnessTop, (( _UVWorld )?( ( appendResult132 * _TopTiling * 0.1 ) ):( float4( uv_TexCoord110, 0.0 , 0.0 ) )).xy );
			float4 lerpResult14 = lerp( ( tex2DNode6 * _Metallic ) , ( tex2DNode7 * _MetallicTop ) , temp_output_16_0);
			o.Metallic = lerpResult14.r;
			float lerpResult19 = lerp( ( tex2DNode6.a * _Glossiness ) , ( tex2DNode7.a * _GlossinessTop ) , temp_output_16_0);
			o.Smoothness = lerpResult19;
			float4 tex2DNode48 = tex2D( _OcclusionMap, customUVs148, temp_output_147_0, temp_output_146_0 );
			float lerpResult51 = lerp( tex2DNode48.r , ( tex2DNode48.r + ( 1.0 - _OcclusionTop ) ) , temp_output_16_0);
			o.Occlusion = saturate( ( lerpResult51 + ( 1.0 - _OcclusionStrength ) ) );
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows dithercrossfade 

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
				float2 customPack1 : TEXCOORD1;
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
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = IN.tSpace0.xyz * worldViewDir.x + IN.tSpace1.xyz * worldViewDir.y + IN.tSpace2.xyz * worldViewDir.z;
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
	Fallback "DLNK Shaders/ASE/TopBottomSimple"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19303
Node;AmplifyShaderEditor.RangedFloatNode;141;-2176,-48;Float;False;Property;_Tiling;Tiling;1;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;142;-1680,352;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;143;-1968,288;Float;False;Property;_Parallax;Parallax;14;0;Create;True;0;0;0;False;0;False;0.4247461;0.2;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;144;-1872,400;Inherit;False;Property;_Curvature;Curvature (xy);15;0;Create;False;0;0;0;False;0;False;0.5,0;0.5,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;145;-2048,400;Inherit;False;Property;_Samplesxy;Samples (xy);16;0;Create;True;0;0;0;False;0;False;2,30;2,50;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TexturePropertyNode;150;-1920,96;Inherit;True;Property;_ParallaxMap;Parallax Map;13;0;Create;True;0;0;0;False;0;False;None;b5df684f5aaab3346bbf51f7f36cb861;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TextureCoordinatesNode;151;-1920,-48;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ParallaxOcclusionMappingNode;149;-1696,80;Inherit;False;0;16;False;;96;False;;2;0.02;0;False;1,1;False;0,0;11;0;FLOAT2;0,0;False;1;SAMPLER2D;;False;7;SAMPLERSTATE;;False;2;FLOAT;0.02;False;3;FLOAT3;0,0,0;False;8;INT;0;False;9;INT;0;False;10;INT;0;False;4;FLOAT;0;False;5;FLOAT2;0,0;False;6;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DdyOpNode;146;-1616,-112;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DdxOpNode;147;-1616,-176;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;148;-1440,336;Float;False;customUVs;1;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-1152,272;Inherit;False;Property;_BumpScale;BumpScale;4;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;4;-960,240;Inherit;True;Property;_BumpMap;BumpMap;3;0;Create;True;0;0;0;False;0;False;-1;None;95ba15664ab705347a638c2273110859;True;0;True;bump;Auto;True;Object;-1;Derivative;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldNormalVector;15;-640,48;Inherit;False;True;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;119;-576,208;Inherit;False;Property;_Smooth;Smooth;21;0;Create;True;0;0;0;False;0;False;0.5;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-560,288;Inherit;False;Property;_Ammount;Ammount;20;0;Create;True;0;0;0;False;0;False;0.5;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;118;-448,64;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;157;-1024,-64;Inherit;True;Property;_TextureSample0;Texture Sample 0;31;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Derivative;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;117;-304,-80;Inherit;False;Property;_Clamp;Clamp;22;0;Create;True;0;0;0;False;0;False;0,1;0.51,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.OneMinusNode;170;-576,-48;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-400,208;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;171;-208,304;Inherit;False;Property;_MaskPower;Mask Power;25;0;Create;True;0;0;0;False;0;False;1;1.58;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;131;-1616,800;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TFHCRemapNode;105;-224,48;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;155;-208,240;Inherit;False;Property;_HeightMask;Height Mask;24;0;Create;True;0;0;0;False;0;False;1;1.04;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;172;48,304;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-43.20768,1138.505;Inherit;False;Property;_OcclusionTop;Occlusion Top;32;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;111;-1680,704;Inherit;False;Property;_TopTiling;TopTiling;18;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;132;-1408,816;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;134;-1424,656;Inherit;False;Constant;_Float0;Float 0;26;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;165;-16,160;Inherit;False;HardLight;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;57;61.87585,1259.351;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;48;-161.4265,896.813;Inherit;True;Property;_OcclusionMap;Occlusion Map;8;0;Create;True;0;0;0;False;0;False;-1;None;6e0e86f30d7c57d42843aeb334934409;True;0;False;white;Auto;False;Object;-1;Derivative;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;133;-1264,704;Inherit;False;3;3;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;110;-1760,976;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;161;32,-96;Inherit;False;Property;_UseHeight;UseHeight;23;0;Create;True;0;0;0;False;0;False;1;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;53;56.62176,790.4156;Inherit;False;Property;_OcclusionStrength;Occlusion Strength;9;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;56;253.6533,1125.369;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;30;-784,-768;Inherit;False;Property;_Color;Color;0;0;Create;True;0;0;0;False;0;False;0.8207547,0.8207547,0.8207547,0;0.7924528,0.7924528,0.7924528,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-864,-592;Inherit;True;Property;_MainTex;Albedo;2;0;Create;False;0;0;0;False;0;False;-1;None;ec6d49e7fcda0ce46956d3990e63fe7c;True;0;False;white;Auto;False;Object;-1;Derivative;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;16;160,64;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;136;-1232,896;Inherit;False;Property;_UVWorld;UVWorld;19;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DdyOpNode;152;-1472,1088;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DdxOpNode;153;-1472,1024;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-1184,464;Inherit;False;Property;_BumpScaleTop;BumpScaleTop;28;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;-849.9683,923.2254;Inherit;False;Property;_GlossinessTop;GlossinessTop;30;0;Create;True;0;0;0;False;0;False;0.5;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;25;-847.3686,842.6254;Inherit;False;Property;_MetallicTop;MetallicTop;31;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-851.2681,746.4254;Inherit;False;Property;_Glossiness;Glossiness;7;0;Create;True;0;0;0;False;0;False;0.5;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;51;198.4846,911.2618;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-848.6683,665.8253;Inherit;False;Property;_Metallic;Metallic;6;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;59;109.1635,699.7813;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;137;223.3482,-526.7415;Inherit;False;Property;_EmissionColor;Emission Color;11;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;7;-666.1178,732.2252;Inherit;True;Property;_MetalnessTop;MetalnessTop;29;0;Create;True;0;0;0;False;0;False;-1;None;7b309caa282dfb448b324fad3892e41a;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;6;-672,544;Inherit;True;Property;_MetallicGlossMap;MetallicGlossMap;5;0;Create;True;0;0;0;False;0;False;-1;None;4842fda9166395547bb07b332ec0f1e5;True;0;False;white;Auto;False;Object;-1;Derivative;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;138;116.9743,-352.0376;Inherit;True;Property;_EmissionMap;Emission Map;12;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Derivative;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-480,-672;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;31;-560,-464;Inherit;False;Property;_ColorTop;ColorTop;17;0;Create;True;0;0;0;False;0;False;0.8490566,0.8450516,0.8450516,0;0.8490566,0.8450516,0.8450516,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-848,-336;Inherit;True;Property;_AlbedoTop;AlbedoTop;26;0;Create;True;0;0;0;False;0;False;-1;None;59576fa46619dd6429a14e3ed21eb3ad;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;20;-314.3682,543.6255;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-315.6683,641.775;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;58;269.4158,718.1709;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-318.2681,853.6758;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-315.668,756.8263;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;139;465.2676,-254.2243;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;44;-224,-592;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;29;-304,-400;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;5;-960,432;Inherit;True;Property;_BumpMapTop;BumpMapTop;27;0;Create;True;0;0;0;False;0;False;-1;None;9d9fefe3ce0121d48993363165fbff23;True;0;True;bump;Auto;True;Object;-1;Derivative;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;14;-94.6681,554.0255;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;19;-118.0685,698.3256;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;54;294.3732,609.1467;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;140;352,-112;Inherit;False;Property;_Emission;Emission;10;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;18;-80,-448;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;11;-176,416;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;130;411.2387,75.7908;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;DLNK Shaders/ASE/TopBottomParallax;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;DLNK Shaders/ASE/TopBottomSimple;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;151;0;141;0
WireConnection;149;0;151;0
WireConnection;149;1;150;0
WireConnection;149;2;143;0
WireConnection;149;3;142;0
WireConnection;149;8;145;1
WireConnection;149;9;145;2
WireConnection;149;4;144;0
WireConnection;146;0;151;0
WireConnection;147;0;151;0
WireConnection;148;0;149;0
WireConnection;4;1;148;0
WireConnection;4;3;147;0
WireConnection;4;4;146;0
WireConnection;4;5;12;0
WireConnection;15;0;4;0
WireConnection;118;0;15;2
WireConnection;118;2;119;0
WireConnection;157;0;150;0
WireConnection;157;1;148;0
WireConnection;157;3;147;0
WireConnection;157;4;146;0
WireConnection;170;0;157;1
WireConnection;17;0;118;0
WireConnection;17;1;8;0
WireConnection;105;0;17;0
WireConnection;105;1;117;1
WireConnection;105;2;117;2
WireConnection;172;0;170;0
WireConnection;172;1;171;0
WireConnection;132;0;131;1
WireConnection;132;1;131;3
WireConnection;165;0;172;0
WireConnection;165;1;105;0
WireConnection;165;2;155;0
WireConnection;57;0;52;0
WireConnection;48;1;148;0
WireConnection;48;3;147;0
WireConnection;48;4;146;0
WireConnection;133;0;132;0
WireConnection;133;1;111;0
WireConnection;133;2;134;0
WireConnection;110;0;111;0
WireConnection;161;0;105;0
WireConnection;161;1;165;0
WireConnection;56;0;48;1
WireConnection;56;1;57;0
WireConnection;2;1;148;0
WireConnection;2;3;147;0
WireConnection;2;4;146;0
WireConnection;16;0;161;0
WireConnection;136;0;110;0
WireConnection;136;1;133;0
WireConnection;152;0;110;0
WireConnection;153;0;110;0
WireConnection;51;0;48;1
WireConnection;51;1;56;0
WireConnection;51;2;16;0
WireConnection;59;0;53;0
WireConnection;7;1;136;0
WireConnection;6;1;148;0
WireConnection;6;3;147;0
WireConnection;6;4;146;0
WireConnection;138;1;148;0
WireConnection;138;3;147;0
WireConnection;138;4;146;0
WireConnection;28;0;30;0
WireConnection;28;1;2;0
WireConnection;3;1;136;0
WireConnection;20;0;6;0
WireConnection;20;1;22;0
WireConnection;26;0;7;0
WireConnection;26;1;25;0
WireConnection;58;0;51;0
WireConnection;58;1;59;0
WireConnection;27;0;7;4
WireConnection;27;1;24;0
WireConnection;21;0;6;4
WireConnection;21;1;23;0
WireConnection;139;0;138;0
WireConnection;139;1;137;0
WireConnection;44;0;28;0
WireConnection;44;1;2;0
WireConnection;29;0;31;0
WireConnection;29;1;3;0
WireConnection;5;1;136;0
WireConnection;5;3;153;0
WireConnection;5;4;152;0
WireConnection;5;5;13;0
WireConnection;14;0;20;0
WireConnection;14;1;26;0
WireConnection;14;2;16;0
WireConnection;19;0;21;0
WireConnection;19;1;27;0
WireConnection;19;2;16;0
WireConnection;54;0;58;0
WireConnection;140;1;139;0
WireConnection;18;0;44;0
WireConnection;18;1;29;0
WireConnection;18;2;16;0
WireConnection;11;0;4;0
WireConnection;11;1;5;0
WireConnection;11;2;16;0
WireConnection;130;0;18;0
WireConnection;130;1;11;0
WireConnection;130;2;140;0
WireConnection;130;3;14;0
WireConnection;130;4;19;0
WireConnection;130;5;54;0
ASEEND*/
//CHKSM=03645027676C9D5932E8E0354C63E7EC496B15C0