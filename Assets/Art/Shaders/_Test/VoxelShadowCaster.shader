Shader "Custom/VoxelShadowCaster"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

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


			#pragma vertex vert
            #pragma geometry geo
			#pragma fragment ShadowPassFragment

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
			//#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
			#include "Assets/Art/Shaders/VoxelShaderConstants.hlsl"

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
			};
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            // VERTEX SHADER
			v2g vert(v IN)
			{
				v2g OUT;
				OUT.positionOS = IN.positionOS;
                OUT.uv0 = IN.uv0;
                OUT.uv1 = IN.uv1;
                OUT.uv2 = IN.uv2;
				return OUT;
			}


            // GEOMETRY SHADER
			[maxvertexcount(24)]
			void geo(point v2g input[1], inout TriangleStream<Varyings> stream)
			{
				float3 c = input[0].positionOS;
				float  h =  0.5; //_Size;

				// Camera in object space, for optional per-face culling
				float3 camOS = TransformWorldToObject(GetCameraPositionWS());

				uint solidFaces = uint(input[0].uv2.x);


				[unroll]
				for (int f = 0; f < 6; f++)
				{
					if ((solidFaces >> f) & 1)
					{
						float3 n = FaceN[f], t = FaceT[f], b = FaceB[f];
						float3 fc = c + n * h - float3(0,0,0);

						// Skip faces pointing away from the camera -> "up to 6" faces
						//if (dot(n, camOS - fc) <= 0) continue;

						// add points
						Varyings o;
						//o.normalWS = TransformObjectToWorldNormal(n);

						//float2 uv = TRANSFORM_TEX(IN.texCoord, _TextureAtlas);

						// Triangle strip order: (-t,-b) (+t,-b) (-t,+b) (+t,+b)
						VertexPositionInputs inputs = GetVertexPositionInputs(fc - t*h - b*h);
						o.positionCS = inputs.positionCS;
						stream.Append(o);

						inputs = GetVertexPositionInputs(fc + t*h - b*h);
						o.positionCS = inputs.positionCS;
						stream.Append(o);


						inputs = GetVertexPositionInputs(fc - t*h + b*h);
						o.positionCS = inputs.positionCS;
						stream.Append(o);

						inputs = GetVertexPositionInputs(fc + t*h + b*h);
						o.positionCS = inputs.positionCS;
						stream.Append(o);

						stream.RestartStrip();
					}
				}
			}

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(_ALPHATEST_ON)
                    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
                #endif

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                return 0;
            }


			// #include "Assets/Shaders/Texel/Lit/TexelLitInput.hlsl"
			ENDHLSL
		}
    }
}
