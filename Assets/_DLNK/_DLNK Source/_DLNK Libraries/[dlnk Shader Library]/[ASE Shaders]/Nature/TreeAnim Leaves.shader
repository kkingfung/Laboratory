// Made with Amplify Shader Editor v1.9.1.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DLNK Shaders/ASE/Nature/TreeAnim Leaves"
{
	Properties
	{
		_ColorA("Color A (AO alpha)", Color) = (0.5943396,0.5943396,0.5943396,1)
		_ColorBAOalpha("Color B  (AO alpha)", Color) = (1,1,1,1)
		_ColorCAOalpha("Color C  (AO alpha)", Color) = (0.5943396,0.5943396,0.5943396,1)
		_ColorDAOalpha("Color D  (AO alpha)", Color) = (1,1,1,1)
		_Tintpowerxyoffsetzw("Tint power (xy) offset (zw)", Vector) = (1,1,0,0)
		_AOPower("AO Power", Float) = 1
		_MainTex("Albedo", 2D) = "white" {}
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_BumpMap("Normal", 2D) = "white" {}
		_Smoothness("Smoothness", Float) = 0
		_NormalScale("Normal Scale", Float) = 0
		_Noisespeed("Noise Speed", Float) = 1
		_MotionValue("MotionValue (xy)", Vector) = (1,1,0,0)
		_WorldNoiseScale("WorldNoiseScale", Float) = 1
		_VerticalOffset("Vertical Offset (xy)", Vector) = (0,1,0,0)
		_NoiseScale("Leaves Noise Scale", Float) = 1
		_LeavesNoisePower("Leaves Noise Power", Float) = 1
		_LeavesOffset("Leaves Offset (xy)", Vector) = (0,1,0,0)
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TreeTransparentCutout"  "Queue" = "Geometry+0" }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardUtils.cginc"
		#pragma target 3.0
		#define ASE_USING_SAMPLING_MACROS 1
		#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))//ASE Sampler Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex.Sample(samplerTex,coord)
		#else//ASE Sampling Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex2D(tex,coord)
		#endif//ASE Sampling Macros

		#pragma surface surf Standard keepalpha addshadow fullforwardshadows dithercrossfade vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
			float2 uv2_texcoord2;
		};

		uniform float _Noisespeed;
		uniform float _WorldNoiseScale;
		uniform float2 _MotionValue;
		uniform float2 _VerticalOffset;
		uniform float _LeavesNoisePower;
		uniform float _NoiseScale;
		uniform float2 _LeavesOffset;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_BumpMap);
		uniform float4 _BumpMap_ST;
		SamplerState sampler_BumpMap;
		uniform float _NormalScale;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_MainTex);
		uniform float4 _MainTex_ST;
		SamplerState sampler_MainTex;
		uniform float4 _ColorA;
		uniform float4 _ColorBAOalpha;
		uniform float4 _Tintpowerxyoffsetzw;
		uniform float4 _ColorCAOalpha;
		uniform float4 _ColorDAOalpha;
		uniform float _Smoothness;
		uniform float _AOPower;
		uniform float _Cutoff = 0.5;


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
			float2 panner95 = ( 1.0 * _Time.y * temp_cast_0 + ase_worldPos.xy);
			float simplePerlin3D76 = snoise( float3( panner95 ,  0.0 )*_WorldNoiseScale );
			simplePerlin3D76 = simplePerlin3D76*0.5 + 0.5;
			float lerpResult147 = lerp( _MotionValue.x , -_MotionValue.x , _CosTime.z);
			float lerpResult159 = lerp( _MotionValue.y , -_MotionValue.y , _CosTime.y);
			float4 appendResult92 = (float4(( simplePerlin3D76 * lerpResult147 ) , 0.0 , ( simplePerlin3D76 * lerpResult159 ) , 0.0));
			float4 transform89 = mul(unity_WorldToObject,appendResult92);
			float temp_output_85_0 = saturate( (_VerticalOffset.x + (v.texcoord1.xy.y - 0.0) * (_VerticalOffset.y - _VerticalOffset.x) / (1.0 - 0.0)) );
			float4 lerpResult86 = lerp( float4( 0,0,0,0 ) , transform89 , temp_output_85_0);
			float2 temp_cast_3 = (_Noisespeed).xx;
			float2 panner108 = ( 1.0 * _Time.y * temp_cast_3 + ase_worldPos.xy);
			float simplePerlin3D105 = snoise( float3( panner108 ,  0.0 )*_NoiseScale );
			simplePerlin3D105 = simplePerlin3D105*0.5 + 0.5;
			float4 appendResult115 = (float4(0.0 , ( _LeavesNoisePower * simplePerlin3D105 ) , 0.0 , 0.0));
			float4 transform116 = mul(unity_WorldToObject,appendResult115);
			float temp_output_112_0 = saturate( (_LeavesOffset.x + (v.texcoord.xy.y - 0.0) * (_LeavesOffset.y - _LeavesOffset.x) / (1.0 - 0.0)) );
			float4 lerpResult109 = lerp( float4( 0,0,0,0 ) , transform116 , temp_output_112_0);
			v.vertex.xyz += ( lerpResult86 + lerpResult109 ).xyz;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_BumpMap = i.uv_texcoord * _BumpMap_ST.xy + _BumpMap_ST.zw;
			o.Normal = UnpackScaleNormal( SAMPLE_TEXTURE2D( _BumpMap, sampler_BumpMap, uv_BumpMap ), _NormalScale );
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode7 = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, uv_MainTex );
			float temp_output_112_0 = saturate( (_LeavesOffset.x + (i.uv_texcoord.y - 0.0) * (_LeavesOffset.y - _LeavesOffset.x) / (1.0 - 0.0)) );
			float4 lerpResult49 = lerp( _ColorA , _ColorBAOalpha , ( pow( temp_output_112_0 , _Tintpowerxyoffsetzw.x ) + _Tintpowerxyoffsetzw.z ));
			float temp_output_85_0 = saturate( (_VerticalOffset.x + (i.uv2_texcoord2.y - 0.0) * (_VerticalOffset.y - _VerticalOffset.x) / (1.0 - 0.0)) );
			float4 lerpResult120 = lerp( _ColorCAOalpha , _ColorDAOalpha , ( pow( temp_output_85_0 , _Tintpowerxyoffsetzw.y ) + _Tintpowerxyoffsetzw.w ));
			o.Albedo = saturate( ( tex2DNode7 * lerpResult49 * lerpResult120 ) ).rgb;
			float temp_output_55_0 = _Smoothness;
			o.Smoothness = temp_output_55_0;
			o.Occlusion = saturate( (( 1.0 - _AOPower ) + ((( lerpResult49 * lerpResult120 )).a - 0.0) * (1.0 - ( 1.0 - _AOPower )) / (1.0 - 0.0)) );
			o.Alpha = 1;
			clip( tex2DNode7.a - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "DLNKShaders/Nature/LeavesAnim"
}
/*ASEBEGIN
Version=19103
Node;AmplifyShaderEditor.TextureCoordinatesNode;61;-738.9388,3.886868;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;71;-524.3994,14.79866;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;108;-992.7294,698.4373;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;106;-617.4734,605.2056;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;112;-589.7747,800.729;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;111;-797.4492,800.7286;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;115;-467.0405,572.8425;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;105;-807.0516,696.1451;Inherit;False;Simplex3D;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldToObjectTransfNode;116;-275.5679,610.8105;Inherit;False;1;0;FLOAT4;0,0,0,1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;60;-1020.633,604.476;Inherit;False;Property;_NoiseScale;Leaves Noise Scale;16;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;18;-1254.12,734.3831;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;109;-72.66518,557.5308;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;127;-988.5227,-330.9091;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;128;-1234.552,-353.1631;Inherit;False;Property;_UseAlbedo;UseAlbedo;10;0;Create;True;0;0;0;False;0;False;1;True;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;129;-1154.191,-478.0323;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;11;-605.7137,-232.3725;Inherit;True;Property;_BumpMap;Normal;8;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;103;54.23404,288.3328;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;55;-1202.429,-243.9999;Inherit;False;Property;_Smoothness;Smoothness;9;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;7;-821.4617,-777.9648;Inherit;True;Property;_MainTex;Albedo;6;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;49;-445.6661,-1237.6;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;50;-671.8662,-1375.597;Inherit;False;Property;_ColorA;Color A (AO alpha);0;0;Create;False;0;0;0;False;0;False;0.5943396,0.5943396,0.5943396,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;51;-666.9892,-1201.207;Inherit;False;Property;_ColorBAOalpha;Color B  (AO alpha);1;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;139;-464.6918,-1093.716;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;140;-310.6314,-1092.503;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;120;-920.4298,-1253.664;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;122;-1141.752,-1217.271;Inherit;False;Property;_ColorDAOalpha;Color D  (AO alpha);3;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;121;-1146.629,-1390.661;Inherit;False;Property;_ColorCAOalpha;Color C  (AO alpha);2;0;Create;True;0;0;0;False;0;False;0.5943396,0.5943396,0.5943396,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;137;-940.914,-1063.079;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;138;-786.8536,-1061.866;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;141;-1220.626,-984.1237;Inherit;False;Property;_Tintpowerxyoffsetzw;Tint power (xy) offset (zw);4;0;Create;True;0;0;0;False;0;False;1,1,0,0;1,1,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;110;-1021.689,525.7573;Inherit;False;Property;_LeavesNoisePower;Leaves Noise Power;17;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;16;-761.8618,-229.5572;Inherit;False;Property;_NormalScale;Normal Scale;11;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;92;-612.8791,227.1768;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;86;-187.4756,118.1329;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SaturateNode;85;-348.0148,25.36537;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;117;-1587.345,418.3628;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;59;-1587.345,322.3628;Inherit;False;Property;_Noisespeed;Noise Speed;12;0;Create;False;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;149;-1251.345,-77.6372;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;147;-1107.345,-93.63721;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;158;-1251.345,66.36277;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;159;-1107.345,50.36276;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosTime;146;-1507.345,-93.63721;Inherit;True;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NoiseGeneratorNode;76;-1073.764,232.7133;Inherit;False;Simplex3D;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;161;-1073.764,328.7134;Inherit;False;Property;_WorldNoiseScale;WorldNoiseScale;14;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;94;-833.7645,312.7134;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;93;-833.7645,184.7133;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;114;-1025.256,862.1838;Inherit;False;Property;_LeavesOffset;Leaves Offset (xy);18;0;Create;False;0;0;0;False;0;False;0,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;102;-937.884,2.588163;Inherit;False;Property;_VerticalOffset;Vertical Offset (xy);15;0;Create;False;0;0;0;False;0;False;0,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;160;-1483.345,90.36279;Inherit;False;Property;_MotionValue;MotionValue (xy);13;0;Create;False;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;95;-1279.764,247.7133;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldToObjectTransfNode;89;-438.2723,210.3329;Inherit;False;1;0;FLOAT4;0,0,0,1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;132;-57.26088,-1065.376;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;133;144.0598,-973.6294;Inherit;False;False;False;False;True;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;-314.055,-887.6369;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;165;201.1968,-880.5307;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;163;21.85025,-869.7394;Inherit;False;Property;_AOPower;AO Power;5;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;166;23.31196,-794.2705;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;310.3,-254;Float;False;True;-1;2;DLNKShaders/Nature/LeavesAnim;0;0;Standard;DLNK Shaders/ASE/Nature/TreeAnim Leaves;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;False;False;Off;0;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;True;0;True;TreeTransparentCutout;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;7;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;True;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.SaturateNode;164;400.4176,-873.8459;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;167;-144.836,-752.9536;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
WireConnection;71;0;61;2
WireConnection;71;3;102;1
WireConnection;71;4;102;2
WireConnection;108;0;117;0
WireConnection;108;2;59;0
WireConnection;106;0;110;0
WireConnection;106;1;105;0
WireConnection;112;0;111;0
WireConnection;111;0;18;2
WireConnection;111;3;114;1
WireConnection;111;4;114;2
WireConnection;115;1;106;0
WireConnection;105;0;108;0
WireConnection;105;1;60;0
WireConnection;116;0;115;0
WireConnection;109;1;116;0
WireConnection;109;2;112;0
WireConnection;127;0;128;0
WireConnection;127;1;55;0
WireConnection;128;0;129;0
WireConnection;129;0;7;1
WireConnection;129;1;7;2
WireConnection;129;2;7;3
WireConnection;11;5;16;0
WireConnection;103;0;86;0
WireConnection;103;1;109;0
WireConnection;49;0;50;0
WireConnection;49;1;51;0
WireConnection;49;2;140;0
WireConnection;139;0;112;0
WireConnection;139;1;141;1
WireConnection;140;0;139;0
WireConnection;140;1;141;3
WireConnection;120;0;121;0
WireConnection;120;1;122;0
WireConnection;120;2;138;0
WireConnection;137;0;85;0
WireConnection;137;1;141;2
WireConnection;138;0;137;0
WireConnection;138;1;141;4
WireConnection;92;0;93;0
WireConnection;92;2;94;0
WireConnection;86;1;89;0
WireConnection;86;2;85;0
WireConnection;85;0;71;0
WireConnection;149;0;160;1
WireConnection;147;0;160;1
WireConnection;147;1;149;0
WireConnection;147;2;146;3
WireConnection;158;0;160;2
WireConnection;159;0;160;2
WireConnection;159;1;158;0
WireConnection;159;2;146;2
WireConnection;76;0;95;0
WireConnection;76;1;161;0
WireConnection;94;0;76;0
WireConnection;94;1;159;0
WireConnection;93;0;76;0
WireConnection;93;1;147;0
WireConnection;95;0;117;0
WireConnection;95;2;59;0
WireConnection;89;0;92;0
WireConnection;132;0;49;0
WireConnection;132;1;120;0
WireConnection;133;0;132;0
WireConnection;48;0;7;0
WireConnection;48;1;49;0
WireConnection;48;2;120;0
WireConnection;165;0;133;0
WireConnection;165;3;166;0
WireConnection;166;0;163;0
WireConnection;0;0;167;0
WireConnection;0;1;11;0
WireConnection;0;4;55;0
WireConnection;0;5;164;0
WireConnection;0;10;7;4
WireConnection;0;11;103;0
WireConnection;164;0;165;0
WireConnection;167;0;48;0
ASEEND*/
//CHKSM=3D9B1838BD2774585DF56C954A9F69EA46302B04