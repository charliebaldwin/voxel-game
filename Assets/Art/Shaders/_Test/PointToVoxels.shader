Shader "Test/PointToVoxels"
{
	Properties
	{
		[MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
		_Size("Size", Float) = 0.5
		_TextureAtlas("Texture Atlas", 2D) = "white" {}
		_Rows("Rows", Int) = 1
		_Cols("Columns", Int) = 8
		_AtlasIndex("Atlas Index", Int) = 4
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

		Pass
		{
			Name "ForwardLit"
			Tags {"LightMode" = "UniversalForward"}
			Cull Back

			HLSLPROGRAM
			#pragma target 4.0
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag

			#pragma shader_feature _RECEIVE_SHADOWS_ON
			#pragma multi_compile _MAIN_LIGHT_SHADOWS

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			
			TEXTURE2D(_TextureAtlas);
			SAMPLER(sampler_TextureAtlas);

			CBUFFER_START(UnityPerMaterial)
				half4 _BaseColor;
				float _Size;
				float4 _TextureAtlas_ST;
				int _Rows;
				int _Cols;
				int _AtlasIndex;
			CBUFFER_END

			struct v
			{
				float4 positionOS : POSITION;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
			};

			struct v2g
			{
				float4 positionOS : SV_POSITION;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
			};
			struct g2f 
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : POSITION1;
				float3 normalWS : NORMAL;
				float2 texCoord : TEXCOORD0;
			};



			v2g vert(v IN)
			{
				v2g OUT;
				OUT.positionOS = IN.positionOS;
				OUT.uv0 = IN.uv0;
				OUT.uv1 = IN.uv1;
				OUT.uv2 = IN.uv2;
				//OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
				return OUT;
			}

			static const float3 FaceN[6] = { float3( -1,0,0), float3( 1,0,0), float3(0,-1,0), float3(0,1,0), float3(0,0,-1), float3(0,0, 1) };
			static const float3 FaceB[6] = { float3(0,1,0),  float3(0,0,1),  float3(0,0,1),  float3(1,0,0),  float3(1,0,0),  float3(0,1,0)  };
			static const float3 FaceT[6] = { float3(0,0,1),  float3(0,1,0),  float3(1,0,0),  float3(0,0,1),  float3(0,1,0),  float3(1,0,0)  };


			static const float2 UVs[6][4] = {
				{ float2(0,0), float2(1,0), float2(0,1), float2(1,1) },
				{ float2(1,0), float2(1,1), float2(0,0), float2(0,1) },
				{ float2(0,0), float2(1,0), float2(0,1), float2(1,1) },
				{ float2(0,0), float2(1,0), float2(0,1), float2(1,1) },
				{ float2(1,0), float2(1,1), float2(0,0), float2(0,1) },
				{ float2(0,0), float2(1,0), float2(0,1), float2(1,1) }
			};
			float2 flipbook(float2 UV, float Width, float Height, float Tile, float2 Invert)
			{
				Tile = fmod(Tile, Width * Height);
				float2 tileCount = float2(1.0, 1.0) / float2(Width, Height);
				float tileY = abs(Invert.y * Height - (floor(Tile * tileCount.x) + Invert.y * 1));
				float tileX = abs(Invert.x * Width - ((Tile - Width * floor(Tile * tileCount.x)) + Invert.x * 1));
				return (UV + float2(tileX, tileY)) * tileCount;
			}
			 
			[maxvertexcount(24)]
			void geo(point v2g input[1], inout TriangleStream<g2f> stream) {
				float3 c = input[0].positionOS;
				float  h =  _Size;

				// Camera in object space, for optional per-face culling
				float3 camOS = TransformWorldToObject(GetCameraPositionWS());

				int t1 = (int)input[0].uv0.x;
				int t2 = max(t1, (int)input[0].uv0.y);
				int t3 = max(t1, (int)input[0].uv0.z);
				int t4 = max(t1, (int)input[0].uv0.w);
				int t5 = max(t1, (int)input[0].uv1.x);
				int t6 = max(t1, (int)input[0].uv1.y);


				int tArray[6] = {t1, t2, t3, t4, t5, t6};

				uint solidFaces = uint(input[0].uv2.x);

				[unroll]
				for (int f = 0; f < 6; f++)
				{
					if ((solidFaces >> f) & 1) {
						float3 n = FaceN[f], t = FaceT[f], b = FaceB[f];
						float3 fc = c + n * h - float3(0,0,0);

						int tex = tArray[f];

						float2 fb1 = flipbook(UVs[f][0], _Cols, _Rows, tex, float2(0,0));
						float2 fb2 = flipbook(UVs[f][1], _Cols, _Rows, tex, float2(0,0));
						float2 fb3 = flipbook(UVs[f][2], _Cols, _Rows, tex, float2(0,0));
						float2 fb4 = flipbook(UVs[f][3], _Cols, _Rows, tex, float2(0,0));


						// Skip faces pointing away from the camera -> "up to 6" faces
						if (dot(n, camOS - fc) <= 0) continue;

						g2f o;
						o.normalWS = TransformObjectToWorldNormal(n);

						//float2 uv = TRANSFORM_TEX(IN.texCoord, _TextureAtlas);

						// Triangle strip order: (-t,-b) (+t,-b) (-t,+b) (+t,+b)
						o.positionCS = TransformObjectToHClip(fc - t*h - b*h); 
						o.positionWS = fc - t*h - b*h;
						o.texCoord = fb1;
						stream.Append(o);

						o.positionCS = TransformObjectToHClip(fc + t*h - b*h); 
						o.positionWS = fc + t*h - b*h;
						o.texCoord = fb2;
						stream.Append(o);

						o.positionCS = TransformObjectToHClip(fc - t*h + b*h);
						o.positionWS = fc - t*h + b*h;
						o.texCoord = fb3;
						stream.Append(o);

						o.positionCS = TransformObjectToHClip(fc + t*h + b*h); 
						o.positionWS = fc + t*h + b*h;
						o.texCoord = fb4;
						stream.Append(o); 

						stream.RestartStrip();
					}
				}
			}

			half4 frag(g2f IN) : SV_Target
			{


				Light l = GetMainLight();
				half light = saturate(dot(normalize(IN.normalWS), l.direction)) * 0.8 + 0.2;

				float3 steppedPos = floor(IN.positionWS * 8) / 8;
				float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
				half shadow = MainLightRealtimeShadow(shadowCoord);
				shadow *= AdditionalLightRealtimeShadow(0, steppedPos);

				half4 texColor = SAMPLE_TEXTURE2D(_TextureAtlas, sampler_TextureAtlas, IN.texCoord);
				//return half4(texColor.rgb * light * shadow, 1);
				return half4(texColor.rgb * light, 1);

				//return half4(IN.texCoord * light, 0, 1);
			}
			ENDHLSL
		}

		Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            Cull[_Cull]

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

           // #include "Assets/Shaders/Texel/Lit/TexelLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
	}
}
