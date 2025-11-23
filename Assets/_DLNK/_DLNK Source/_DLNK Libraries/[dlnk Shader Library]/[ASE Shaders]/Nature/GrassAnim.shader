// Made with Amplify Shader Editor v1.9.0.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "DLNK Shaders/ASE/Nature/GrassAnim"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_ColorA("Color A", Color) = (0.5943396,0.5943396,0.5943396,0)
		_ColorB("Color B", Color) = (1,1,1,0)
		_ColorOffset("Color Offset", Float) = 0
		_ColorRange("Color Range", Float) = 1
		_ColorFar("Color Far", Color) = (1,1,1,0)
		_FarFresnel("Far Fresnel", Float) = 1
		_BlendOriginal("Blend Original", Float) = 0
		_MainTex("Albedo", 2D) = "white" {}
		_TextureOverride("Texture Override", Float) = 0.5
		_Occlusion("Occlusion", Float) = 0
		_BumpMap("Normal", 2D) = "bump" {}
		_Smoothness("Smoothness", Float) = 0
		_NormalScale("NormalScale", Float) = 0
		_Depth("Depth", Float) = 0
		_DepthOffset("Depth Offset", Float) = 0
		_DepthCutout("Depth Cutout", Float) = 0
		_WindWaveSpeedScale("Wind Wave Speed Scale", Vector) = (1,1,1,1)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Grass"  "Queue" = "Geometry+0" }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardUtils.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
			float customSurfaceDepth141;
			float customSurfaceDepth161;
		};

		uniform float4 _WindWaveSpeedScale;
		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform float _NormalScale;
		uniform float4 _ColorA;
		uniform float4 _ColorB;
		uniform float _ColorOffset;
		uniform float _ColorRange;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float _TextureOverride;
		uniform float4 _ColorFar;
		uniform float _Depth;
		uniform float _DepthOffset;
		uniform float _FarFresnel;
		uniform float _BlendOriginal;
		uniform float _Smoothness;
		uniform float _Occlusion;
		uniform float _DepthCutout;
		uniform float _Cutoff = 0.5;


		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float2 temp_cast_0 = (_WindWaveSpeedScale.y).xx;
			float2 break19_g6 = temp_cast_0;
			float2 temp_cast_1 = (_WindWaveSpeedScale.z).xx;
			float2 panner56 = ( 1.0 * _Time.y * temp_cast_1 + v.texcoord.xy);
			float simplePerlin2D57 = snoise( panner56*_WindWaveSpeedScale.w );
			simplePerlin2D57 = simplePerlin2D57*0.5 + 0.5;
			float temp_output_1_0_g6 = simplePerlin2D57;
			float sinIn7_g6 = sin( temp_output_1_0_g6 );
			float sinInOffset6_g6 = sin( ( temp_output_1_0_g6 + 1.0 ) );
			float lerpResult20_g6 = lerp( break19_g6.x , break19_g6.y , frac( ( sin( ( ( sinIn7_g6 - sinInOffset6_g6 ) * 91.2228 ) ) * 43758.55 ) ));
			float temp_output_129_0 = ( ( ( v.texcoord.xy.y + -0.5 ) * (0.0 + (( _SinTime.z + 1.0 ) - 0.0) * (_WindWaveSpeedScale.z - 0.0) / (2.0 - 0.0)) * _WindWaveSpeedScale.x ) * ( lerpResult20_g6 + sinIn7_g6 ) );
			float4 appendResult170 = (float4(temp_output_129_0 , 0.0 , temp_output_129_0 , 0.0));
			float4 lerpResult180 = lerp( appendResult170 , float4( 0,0,0,0 ) , ( ( v.texcoord.xy.y * -1.0 ) + 1.0 ));
			v.vertex.xyz += lerpResult180.xyz;
			v.vertex.w = 1;
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 customSurfaceDepth141 = ase_vertex3Pos;
			o.customSurfaceDepth141 = -UnityObjectToViewPos( customSurfaceDepth141 ).z;
			float3 customSurfaceDepth161 = ase_vertex3Pos;
			o.customSurfaceDepth161 = -UnityObjectToViewPos( customSurfaceDepth161 ).z;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_BumpMap = i.uv_texcoord * _BumpMap_ST.xy + _BumpMap_ST.zw;
			o.Normal = UnpackScaleNormal( tex2D( _BumpMap, uv_BumpMap ), _NormalScale );
			float2 temp_cast_0 = (_ColorOffset).xx;
			float2 uv_TexCoord104 = i.uv_texcoord + temp_cast_0;
			float4 lerpResult49 = lerp( _ColorA , _ColorB , saturate( pow( uv_TexCoord104.y , _ColorRange ) ));
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode7 = tex2D( _MainTex, uv_MainTex );
			float4 lerpResult108 = lerp( lerpResult49 , ( tex2DNode7 * lerpResult49 ) , pow( uv_TexCoord104.y , _TextureOverride ));
			float cameraDepthFade141 = (( i.customSurfaceDepth141 -_ProjectionParams.y - _DepthOffset ) / _Depth);
			float temp_output_167_0 = saturate( ( saturate( cameraDepthFade141 ) * _FarFresnel ) );
			float4 lerpResult152 = lerp( saturate( lerpResult108 ) , _ColorFar , temp_output_167_0);
			float4 lerpResult153 = lerp( saturate( lerpResult152 ) , lerpResult108 , _BlendOriginal);
			o.Albedo = lerpResult153.rgb;
			o.Smoothness = _Smoothness;
			o.Occlusion = ( pow( uv_TexCoord104.y , _Occlusion ) + temp_output_167_0 );
			o.Alpha = 1;
			float cameraDepthFade161 = (( i.customSurfaceDepth161 -_ProjectionParams.y - _DepthOffset ) / _DepthCutout);
			float3 temp_cast_2 = (cameraDepthFade161).xxx;
			float temp_output_2_0_g9 = tex2DNode7.a;
			float temp_output_3_0_g9 = ( 1.0 - temp_output_2_0_g9 );
			float3 appendResult7_g9 = (float3(temp_output_3_0_g9 , temp_output_3_0_g9 , temp_output_3_0_g9));
			clip( saturate( ( tex2DNode7.a * ( 1.0 - ( ( temp_cast_2 * temp_output_2_0_g9 ) + appendResult7_g9 ) ) ) ).x - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19002
0;0;1920;1019;1124.029;546.5934;1.3;True;False
Node;AmplifyShaderEditor.RangedFloatNode;105;-721.3457,-658.4017;Inherit;False;Property;_ColorOffset;Color Offset;3;0;Create;True;0;0;0;False;0;False;0;0.06;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;104;-535.4762,-739.7249;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;107;-586.1996,-521.7991;Inherit;False;Property;_ColorRange;Color Range;4;0;Create;True;0;0;0;False;0;False;1;1.87;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;106;-402.8112,-569.6089;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;144;-101.505,-0.3937836;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;50;-211.1837,-870.3237;Inherit;False;Property;_ColorA;Color A;1;0;Create;True;0;0;0;False;0;False;0.5943396,0.5943396,0.5943396,0;0.5760189,0.5849056,0.5545566,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;51;-200.1164,-698.1395;Inherit;False;Property;_ColorB;Color B;2;0;Create;True;0;0;0;False;0;False;1,1,1,0;0.6515704,0.7169812,0.6391955,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;139;-285.705,64.80618;Inherit;False;Property;_Depth;Depth;14;0;Create;True;0;0;0;False;0;False;0;10.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;145;-289.205,136.8062;Inherit;False;Property;_DepthOffset;Depth Offset;15;0;Create;True;0;0;0;False;0;False;0;26.59;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;110;-229.9544,-518.4434;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;7;-484.1969,-443.2019;Inherit;True;Property;_MainTex;Albedo;8;0;Create;False;0;0;0;False;0;False;-1;None;b4151a3e5da931b4fa37d8f1c3fb6e3d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;177;-806.9655,603.8821;Inherit;False;Constant;_Float3;Float 3;18;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SinTimeNode;24;-794.0515,330.3641;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;18;-623.4268,307.7039;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;132;-635.1202,717.1869;Inherit;False;Property;_WindWaveSpeedScale;Wind Wave Speed Scale;17;0;Create;True;0;0;0;False;0;False;1,1,1,1;1,2,1,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;49;35.67157,-718.1121;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;120;-394.0696,-251.7675;Inherit;False;Property;_TextureOverride;Texture Override;9;0;Create;True;0;0;0;False;0;False;0.5;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CameraDepthFade;141;-110.5049,165.2062;Inherit;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;122;-0.06840897,-375.7645;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;156;-286.5335,216.9983;Inherit;False;Property;_FarFresnel;Far Fresnel;6;0;Create;True;0;0;0;False;0;False;1;0.7;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;148;134.2328,211.3803;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;160;453.6664,465.0983;Inherit;False;Property;_DepthCutout;Depth Cutout;16;0;Create;True;0;0;0;False;0;False;0;100;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;-198.3129,-439.1495;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;176;-794.9291,485.5205;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;56;-356.4822,722.2799;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;57;-161.0953,688.9396;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;42;-384.5283,403.8477;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;108;154.1832,-469.4647;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;178;-643.4663,486.5234;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;2;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;155;136.4668,109.9977;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CameraDepthFade;161;572.6664,314.0983;Inherit;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;164;632.6663,440.0983;Inherit;False;Lerp White To;-1;;9;047d7c189c36a62438973bad9d37b1c2;0;2;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;167;323.0635,125.5362;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;151;-301.3671,-102.5198;Inherit;False;Property;_ColorFar;Color Far;5;0;Create;True;0;0;0;False;0;False;1,1,1,0;0.3769134,0.6603774,0.4978581,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-341.2679,509.8662;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;101;14.10099,-156.7143;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;130;-185.9182,587.89;Inherit;False;Noise Sine Wave;-1;;6;a6eff29f739ced848846e3b648af87bd;0;2;1;FLOAT;0;False;2;FLOAT2;-0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;165;797.6663,445.0983;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;184;-216.5374,352.582;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;129;51.98076,695.9817;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;152;131.2351,-30.92023;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;127;460.9714,-71.90961;Inherit;False;Property;_Occlusion;Occlusion;10;0;Create;True;0;0;0;False;0;False;0;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;154;455.3664,-414.6017;Inherit;False;Property;_BlendOriginal;Blend Original;7;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;158;223.1662,-134.4099;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;185;-85.53735,348.582;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;162;828.6663,330.0983;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;16;916.227,-627.3523;Inherit;False;Property;_NormalScale;NormalScale;13;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;170;32.66736,522.6056;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.PowerNode;128;460.0421,1.35038;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;180;192.0533,499.1671;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SaturateNode;196;840.2714,213.9066;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;11;847.6,-530.6999;Inherit;True;Property;_BumpMap;Normal;11;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;55;1078.792,274.4799;Inherit;False;Property;_Smoothness;Smoothness;12;0;Create;True;0;0;0;False;0;False;0;0.58;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;153;461.4664,-331.1017;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;168;605.1877,18.92814;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;195;975.8836,-281.1772;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;DLNK Shaders/ASE/Nature/GrassAnim;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;True;0;False;Grass;;Geometry;All;18;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;104;1;105;0
WireConnection;106;0;104;2
WireConnection;106;1;107;0
WireConnection;110;0;106;0
WireConnection;49;0;50;0
WireConnection;49;1;51;0
WireConnection;49;2;110;0
WireConnection;141;2;144;0
WireConnection;141;0;139;0
WireConnection;141;1;145;0
WireConnection;122;0;104;2
WireConnection;122;1;120;0
WireConnection;148;0;141;0
WireConnection;48;0;7;0
WireConnection;48;1;49;0
WireConnection;176;0;24;3
WireConnection;176;1;177;0
WireConnection;56;0;18;0
WireConnection;56;2;132;3
WireConnection;57;0;56;0
WireConnection;57;1;132;4
WireConnection;42;0;18;2
WireConnection;108;0;49;0
WireConnection;108;1;48;0
WireConnection;108;2;122;0
WireConnection;178;0;176;0
WireConnection;178;4;132;3
WireConnection;155;0;148;0
WireConnection;155;1;156;0
WireConnection;161;2;144;0
WireConnection;161;0;160;0
WireConnection;161;1;145;0
WireConnection;164;1;161;0
WireConnection;164;2;7;4
WireConnection;167;0;155;0
WireConnection;25;0;42;0
WireConnection;25;1;178;0
WireConnection;25;2;132;1
WireConnection;101;0;108;0
WireConnection;130;1;57;0
WireConnection;130;2;132;2
WireConnection;165;0;164;0
WireConnection;184;0;18;2
WireConnection;129;0;25;0
WireConnection;129;1;130;0
WireConnection;152;0;101;0
WireConnection;152;1;151;0
WireConnection;152;2;167;0
WireConnection;158;0;152;0
WireConnection;185;0;184;0
WireConnection;162;0;7;4
WireConnection;162;1;165;0
WireConnection;170;0;129;0
WireConnection;170;2;129;0
WireConnection;128;0;104;2
WireConnection;128;1;127;0
WireConnection;180;0;170;0
WireConnection;180;2;185;0
WireConnection;196;0;162;0
WireConnection;11;5;16;0
WireConnection;153;0;158;0
WireConnection;153;1;108;0
WireConnection;153;2;154;0
WireConnection;168;0;128;0
WireConnection;168;1;167;0
WireConnection;195;0;153;0
WireConnection;195;1;11;0
WireConnection;195;4;55;0
WireConnection;195;5;168;0
WireConnection;195;10;196;0
WireConnection;195;11;180;0
ASEEND*/
//CHKSM=5755A12657BC9638E7896276FCB65020D75AD5A9