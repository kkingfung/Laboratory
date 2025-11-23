// Made with Amplify Shader Editor v1.9.3.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DLNK Shaders/ASE/Nature/WaterRunningSimple"
{
	Properties
	{
		[HDR]_ColorA("Color A", Color) = (1,1,1,0)
		[HDR]_ColorB("Color B", Color) = (0,0,0,0)
		_NoiseScale("NoiseScale", Float) = 0.5
		_NoiseSpeed("NoiseSpeed", Float) = 1
		_MainTiling("Main Tiling", Vector) = (1,1,0,0)
		_SecondTiling("Second Tiling", Vector) = (1,1,0,0)
		_MainTex("Albedo", 2D) = "white" {}
		_MainTex1("Albedo B", 2D) = "white" {}
		_Metalness("Metalness", Float) = 0.5
		_Smoothness("Smoothness", Float) = 0.5
		_NormalScaleXY("Normal Scale (XY)", Vector) = (1,1,0,0)
		_BumpMap("Normal A", 2D) = "bump" {}
		_BumpMap1("Normal B", 2D) = "bump" {}
		_Distorsion("Distorsion", Float) = 0.1
		_Speed("Speed", Float) = 1
		_SpeedAB("Speed A (XY) B (ZW)", Vector) = (0,1,0,0.5)
		_VertexDisplace("VertexDisplace", Float) = 0.5
		_VertexOffset("VertexOffset", Float) = 0.5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
		Cull Back
		AlphaToMask On
		GrabPass{ }
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardUtils.cginc"
		#pragma target 3.5
		#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
		#else
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
		#endif
		#pragma surface surf Standard keepalpha dithercrossfade vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
			float4 screenPos;
		};

		uniform sampler2D _MainTex;
		uniform half _Speed;
		uniform half4 _SpeedAB;
		uniform half2 _MainTiling;
		uniform sampler2D _MainTex1;
		uniform half2 _SecondTiling;
		uniform half _NoiseSpeed;
		uniform half _NoiseScale;
		uniform half _VertexDisplace;
		uniform half _VertexOffset;
		uniform sampler2D _BumpMap;
		uniform half2 _NormalScaleXY;
		uniform sampler2D _BumpMap1;
		ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
		uniform half _Distorsion;
		uniform half4 _ColorA;
		uniform half4 _ColorB;
		uniform half _Metalness;
		uniform half _Smoothness;


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


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			half mulTime11 = _Time.y * _Speed;
			half2 appendResult37 = (half2(_SpeedAB.x , _SpeedAB.y));
			float2 uv_TexCoord1 = v.texcoord.xy * _MainTiling;
			half2 panner5 = ( mulTime11 * appendResult37 + uv_TexCoord1);
			half2 appendResult38 = (half2(_SpeedAB.z , _SpeedAB.w));
			float2 uv_TexCoord88 = v.texcoord.xy * _SecondTiling;
			half2 panner32 = ( mulTime11 * appendResult38 + uv_TexCoord88);
			half mulTime98 = _Time.y * _NoiseSpeed;
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			half2 panner97 = ( mulTime98 * appendResult37 + ase_worldPos.xy);
			half simplePerlin3D94 = snoise( half3( panner97 ,  0.0 )*_NoiseScale );
			simplePerlin3D94 = simplePerlin3D94*0.5 + 0.5;
			half4 lerpResult39 = lerp( tex2Dlod( _MainTex, float4( panner5, 0, 0.0) ) , tex2Dlod( _MainTex1, float4( panner32, 0, 0.0) ) , simplePerlin3D94);
			half temp_output_47_0 = (lerpResult39).a;
			half3 ase_vertexNormal = v.normal.xyz;
			v.vertex.xyz += ( saturate( ( temp_output_47_0 * _VertexDisplace * ase_vertexNormal ) ) + ( _VertexOffset * ase_vertexNormal ) );
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			half mulTime11 = _Time.y * _Speed;
			half2 appendResult37 = (half2(_SpeedAB.x , _SpeedAB.y));
			float2 uv_TexCoord1 = i.uv_texcoord * _MainTiling;
			half2 panner5 = ( mulTime11 * appendResult37 + uv_TexCoord1);
			half2 appendResult38 = (half2(_SpeedAB.z , _SpeedAB.w));
			float2 uv_TexCoord88 = i.uv_texcoord * _SecondTiling;
			half2 panner32 = ( mulTime11 * appendResult38 + uv_TexCoord88);
			half3 temp_output_31_0 = BlendNormals( UnpackScaleNormal( tex2D( _BumpMap, panner5 ), _NormalScaleXY.x ) , UnpackScaleNormal( tex2D( _BumpMap1, panner32 ), _NormalScaleXY.y ) );
			o.Normal = temp_output_31_0;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ase_screenPos );
			half4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
			half4 screenColor84 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( half3( (ase_grabScreenPosNorm).xy ,  0.0 ) + ( temp_output_31_0 * _Distorsion ) ).xy);
			half mulTime98 = _Time.y * _NoiseSpeed;
			float3 ase_worldPos = i.worldPos;
			half2 panner97 = ( mulTime98 * appendResult37 + ase_worldPos.xy);
			half simplePerlin3D94 = snoise( half3( panner97 ,  0.0 )*_NoiseScale );
			simplePerlin3D94 = simplePerlin3D94*0.5 + 0.5;
			half4 lerpResult12 = lerp( _ColorA , _ColorB , simplePerlin3D94);
			half4 lerpResult39 = lerp( tex2D( _MainTex, panner5 ) , tex2D( _MainTex1, panner32 ) , simplePerlin3D94);
			o.Albedo = ( screenColor84 + ( lerpResult12 * lerpResult39 * screenColor84 ) ).rgb;
			half temp_output_47_0 = (lerpResult39).a;
			o.Metallic = saturate( ( temp_output_47_0 * _Metalness ) );
			o.Smoothness = saturate( ( temp_output_47_0 * _Smoothness ) );
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "DLNK Shaders/ASE/Nature/WaterSimple"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19303
Node;AmplifyShaderEditor.Vector4Node;34;-1258.388,212.3493;Inherit;False;Property;_SpeedAB;Speed A (XY) B (ZW);17;0;Create;False;0;0;0;False;0;False;0,1,0,0.5;0,1,0,0.5;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;89;-1376,16;Inherit;False;Property;_MainTiling;Main Tiling;4;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;91;-1360,464;Inherit;False;Property;_SecondTiling;Second Tiling;5;0;Create;True;0;0;0;False;0;False;1,1;5,5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;6;-896,288;Inherit;False;Property;_Speed;Speed;16;0;Create;True;0;0;0;False;0;False;1;0.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;38;-1041.388,307.3493;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;37;-1040.388,214.3493;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;1;-1104,64;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;88;-1136,448;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;11;-864,192;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;99;-1223.962,-160.1391;Inherit;False;Property;_NoiseSpeed;NoiseSpeed;3;0;Create;True;0;0;0;False;0;False;1;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;32;-891.3884,375.3493;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;5;-800,48;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;92;-944,576;Inherit;False;Property;_NormalScaleXY;Normal Scale (XY);10;0;Create;True;0;0;0;False;0;False;1,1;0.62,0.27;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.WorldPosInputsNode;96;-976,-272;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleTimeNode;98;-1024,-96;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;24;-684.3884,351.3493;Inherit;True;Property;_BumpMap;Normal A;11;0;Create;False;0;0;0;False;0;False;-1;None;36a8f9b5fcf33754387087ece5a99804;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;30;-685.3884,552.3493;Inherit;True;Property;_BumpMap1;Normal B;12;0;Create;False;0;0;0;False;0;False;-1;None;054d24f51dd34704f82c7edd81e94a43;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;95;-528,-192;Inherit;False;Property;_NoiseScale;NoiseScale;2;0;Create;True;0;0;0;False;0;False;0.5;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;97;-752,-224;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GrabScreenPosition;79;-389.4426,-682.9747;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;80;-284.508,-504.7122;Inherit;False;Property;_Distorsion;Distorsion;15;0;Create;True;0;0;0;False;0;False;0.1;0.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;31;-367.1401,315.0606;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;9;-577.3884,-76.6507;Inherit;True;Property;_MainTex;Albedo;6;0;Create;False;0;0;0;False;0;False;-1;None;d2233f0af4a6afc4db1b77caa890a5ca;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;29;-580.3884,124.3493;Inherit;True;Property;_MainTex1;Albedo B;7;0;Create;False;0;0;0;False;0;False;-1;None;eb3e237ea9444864299c696c46b1e1d7;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NoiseGeneratorNode;94;-304,-224;Inherit;False;Simplex3D;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;81;-96.13156,-579.3043;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;82;-173.2524,-662.7464;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;39;-224,-16;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0.5;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;83;72.01645,-642.5178;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;14;-607.3884,-414.6507;Inherit;False;Property;_ColorA;Color A;0;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,0;8,8,8,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;15;-390.3884,-415.6507;Inherit;False;Property;_ColorB;Color B;1;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;47;-80,16;Inherit;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;104;-64,720;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;101;-64,608;Inherit;False;Property;_VertexDisplace;VertexDisplace;18;0;Create;True;0;0;0;False;0;False;0.5;0.4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenColorNode;84;203.5006,-676.6532;Inherit;False;Global;_GrabScreen1;Grab Screen 1;9;0;Create;True;0;0;0;False;0;False;Object;-1;False;False;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;12;-96,-384;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;48;0,192;Inherit;False;Property;_Smoothness;Smoothness;9;0;Create;True;0;0;0;False;0;False;0.5;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;51;16,368;Inherit;False;Property;_Metalness;Metalness;8;0;Create;True;0;0;0;False;0;False;0.5;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;103;128,496;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;105;176,800;Inherit;False;Property;_VertexOffset;VertexOffset;19;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;208,-336;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;192,192;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;208,288;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;102;304,544;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;108;304,672;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;64;-1330.963,-575.7571;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenDepthNode;65;-1330.963,-655.7571;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;72;-1122.963,-447.7569;Float;False;1;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;66;-1106.963,-623.7571;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;67;-914.9631,-511.7569;Inherit;False;Property;_Depth;Depth;13;0;Create;True;0;0;0;False;0;False;0.9;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;68;-1090.963,-527.7571;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;69;-898.9631,-607.7571;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;70;-754.9631,-511.7569;Inherit;False;Property;_Falloff;Falloff;14;0;Create;True;0;0;0;False;0;False;-3;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;71;-754.9631,-607.7571;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;73;-593.1646,-595.3221;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;85;416,-256;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;45;192,96;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;52;208,384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;107;464,608;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;442,-42;Half;False;True;-1;3;ASEMaterialInspector;0;0;Standard;DLNK Shaders/ASE/Nature/WaterRunningSimple;False;False;False;False;False;False;False;False;False;False;False;False;True;False;True;True;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Translucent;0.5;True;False;0;False;Opaque;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;True;0.897;True;0;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;DLNK Shaders/ASE/Nature/WaterSimple;0;-1;15;-1;0;True;0;0;False;;-1;0;False;;0;0;0;False;0;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;38;0;34;3
WireConnection;38;1;34;4
WireConnection;37;0;34;1
WireConnection;37;1;34;2
WireConnection;1;0;89;0
WireConnection;88;0;91;0
WireConnection;11;0;6;0
WireConnection;32;0;88;0
WireConnection;32;2;38;0
WireConnection;32;1;11;0
WireConnection;5;0;1;0
WireConnection;5;2;37;0
WireConnection;5;1;11;0
WireConnection;98;0;99;0
WireConnection;24;1;5;0
WireConnection;24;5;92;1
WireConnection;30;1;32;0
WireConnection;30;5;92;2
WireConnection;97;0;96;0
WireConnection;97;2;37;0
WireConnection;97;1;98;0
WireConnection;31;0;24;0
WireConnection;31;1;30;0
WireConnection;9;1;5;0
WireConnection;29;1;32;0
WireConnection;94;0;97;0
WireConnection;94;1;95;0
WireConnection;81;0;31;0
WireConnection;81;1;80;0
WireConnection;82;0;79;0
WireConnection;39;0;9;0
WireConnection;39;1;29;0
WireConnection;39;2;94;0
WireConnection;83;0;82;0
WireConnection;83;1;81;0
WireConnection;47;0;39;0
WireConnection;84;0;83;0
WireConnection;12;0;14;0
WireConnection;12;1;15;0
WireConnection;12;2;94;0
WireConnection;103;0;47;0
WireConnection;103;1;101;0
WireConnection;103;2;104;0
WireConnection;13;0;12;0
WireConnection;13;1;39;0
WireConnection;13;2;84;0
WireConnection;46;0;47;0
WireConnection;46;1;48;0
WireConnection;49;0;47;0
WireConnection;49;1;51;0
WireConnection;102;0;103;0
WireConnection;108;0;105;0
WireConnection;108;1;104;0
WireConnection;65;0;64;0
WireConnection;66;0;65;0
WireConnection;66;1;72;4
WireConnection;68;0;66;0
WireConnection;69;0;68;0
WireConnection;69;1;67;0
WireConnection;71;0;69;0
WireConnection;71;1;70;0
WireConnection;73;0;71;0
WireConnection;85;0;84;0
WireConnection;85;1;13;0
WireConnection;45;0;46;0
WireConnection;52;0;49;0
WireConnection;107;0;102;0
WireConnection;107;1;108;0
WireConnection;0;0;85;0
WireConnection;0;1;31;0
WireConnection;0;3;52;0
WireConnection;0;4;45;0
WireConnection;0;11;107;0
ASEEND*/
//CHKSM=68ECA039E2080A79F57C299507E975412D0B3FBA