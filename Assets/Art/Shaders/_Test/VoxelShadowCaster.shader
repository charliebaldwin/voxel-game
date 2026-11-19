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
			ZTest Less
			Cull Back


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
			
			struct Voxel
			{
				float4 	positionOS 		: POSITION;
				uint texIndex0:TEXINDEX0; uint texIndex1:TEXINDEX1; uint texIndex2:TEXINDEX2; uint texIndex3:TEXINDEX3; uint texIndex4:TEXINDEX4; uint texIndex5:TEXINDEX5;
				int feature0Index0:FEAT0INDEX0; int feature0Index1:FEAT0INDEX1; int feature0Index2:FEAT0INDEX2; int feature0Index3:FEAT0INDEX3; int feature0Index4:FEAT0INDEX4;int feature0Index5:FEAT0INDEX5;

				//uint 	atlasIndices[6] : ATLASINDICES;
				uint    solidFaces		: SOLIDFACES;
				uint	damage 			: DAMAGE;
				uint	solidNeighbors	: SOLIDNEIGHBORS;
			};

			struct Vertex
			{
				float4 positionCS 		: SV_POSITION;
				float3 positionWS 		: POSITION1;
				//float4 tempC : TEXCOORD2;
			};
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

			StructuredBuffer<Voxel> _VoxelBuffer;

            // VERTEX SHADER
			Voxel vert(uint id : SV_VertexID)
			{
				return _VoxelBuffer[id];
			}


            // GEOMETRY SHADER
			[maxvertexcount(24)]
			void geo(point Voxel IN[1], inout TriangleStream<Varyings> stream)
			{
				float3 c = IN[0].positionOS;
				float  h =  0.5; //_Size;

				// Camera in object space, for optional per-face culling
				float3 camOS = TransformWorldToObject(GetCameraPositionWS());

				uint solidFaces = IN[0].solidFaces;


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
