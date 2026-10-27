Shader "Test/PointToVoxels"
{
	Properties
	{
		[MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
		_Size("Size", Float) = 0.5
		_TextureAtlas("Texture Atlas", 2D) = "white" {}
		_EdgeNormalAtlas("Edge Normal Atlas", 2D) = "white" {}
		_DamageAtlas("Damage Texture Atlas", 2D) = "white" {}
		_DebugTexture("Face Debug Texture", 2D) = "white" {}
		_Rows("Rows", Int) = 1
		_Cols("Columns", Int) = 8
		_AtlasIndex("Atlas Index", Int) = 4
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

		UsePass "Custom/VoxelShadowCaster/SHADOWCASTER"

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }
			Cull Back

			HLSLPROGRAM
			#pragma target 4.0
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag

			#pragma shader_feature _RECEIVE_SHADOWS_ON
			#pragma multi_compile _MAIN_LIGHT_SHADOWS
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
			
			#include "Assets/Art/Shaders/VoxelShaderConstants.hlsl"
			#include "Assets/Art/Shaders/UVFlipbook.hlsl"

			TEXTURE2D(_TextureAtlas);
			SAMPLER(sampler_TextureAtlas);
			TEXTURE2D(_EdgeNormalAtlas);
			SAMPLER(sampler_EdgeNormalAtlas);
			TEXTURE2D(_DamageAtlas);
			SAMPLER(sampler_DamageAtlas);
			TEXTURE2D(_DebugTexture);
			SAMPLER(sampler_DebugTexture);

			CBUFFER_START(UnityPerMaterial)
				half4 _BaseColor;
				float _Size;
				int _Rows;
				int _Cols;
				int _AtlasIndex;
			CBUFFER_END


			// TYPES
			struct Voxel
			{
				float4 positionOS : POSITION;
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
			};

			struct g2f
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : POSITION1;
				float3 normalWS : NORMAL;
				float2 baseUV : TEXCOORD0;
				float2 normalUV : TEXCOORD1;
				float2 damageUV : DAMAGE;
				//float4 tempC : TEXCOORD2;
			};


			// VERTEX SHADER
			Voxel vert(Voxel IN)
			{
				return IN;
			}



			// GEOMETRY SHADER
			[maxvertexcount(24)]
			void geo(point Voxel input[1], inout TriangleStream<g2f> stream)
			{
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

				int damage = input[0].uv2.y;


				[unroll]
				for (int f = 0; f < 6; f++)
				{
					if ((solidFaces >> f) & 1)
					{
						float3 n = FaceN[f], t = FaceT[f], b = FaceB[f];
						float3 fc = c + n * h - float3(0,0,0);

						int tex = tArray[f];

						float2 baseUV1 = flipbook(UVs[f][0], _Cols, _Rows, tex, float2(0,0));
						float2 baseUV2 = flipbook(UVs[f][1], _Cols, _Rows, tex, float2(0,0));
						float2 baseUV3 = flipbook(UVs[f][2], _Cols, _Rows, tex, float2(0,0));
						float2 baseUV4 = flipbook(UVs[f][3], _Cols, _Rows, tex, float2(0,0));


						// DEBUG TEXTURE UVs
						// baseUV1 = flipbook(UVs[f][0], 1, 6, f, float2(0,1));
						// baseUV2 = flipbook(UVs[f][1], 1, 6, f, float2(0,1));
                        // baseUV3 = flipbook(UVs[f][2], 1, 6, f, float2(0,1));
                        // baseUV4 = flipbook(UVs[f][3], 1, 6, f, float2(0,1));

						int normalTile = 0;
						normalTile += 1 * (solidFaces >> EdgeIndices[f][0] & 1);
						normalTile += 2 * (solidFaces >> EdgeIndices[f][1] & 1);
						normalTile += 4 * (solidFaces >> EdgeIndices[f][2] & 1);
						normalTile += 8 * (solidFaces >> EdgeIndices[f][3] & 1);
						//normalTile += 16 * (solidFaces >> EdgeIndices[f][0] & 1);
						//normalTile = 15;
						float2 normalUV1 = flipbook(UVs[f][0], 4, 4, normalTile, float2(0,1));
						float2 normalUV2 = flipbook(UVs[f][1], 4, 4, normalTile, float2(0,1));
						float2 normalUV3 = flipbook(UVs[f][2], 4, 4, normalTile, float2(0,1));
						float2 normalUV4 = flipbook(UVs[f][3], 4, 4, normalTile, float2(0,1));

						float2 damageUV[4] =
						{
							flipbook(UVs[f][0], 6, 1, damage, float2(0,0)),
							flipbook(UVs[f][1], 6, 1, damage, float2(0,0)),
							flipbook(UVs[f][2], 6, 1, damage, float2(0,0)),
							flipbook(UVs[f][3], 6, 1, damage, float2(0,0))
						};

						// Skip faces pointing away from the camera -> "up to 6" faces
						if (dot(n, camOS - fc) <= 0) continue;



						// add points
						g2f o;
						o.normalWS = TransformObjectToWorldNormal(n);

						//float2 uv = TRANSFORM_TEX(IN.texCoord, _TextureAtlas);

						// Triangle strip order: (-t,-b) (+t,-b) (-t,+b) (+t,+b)
						VertexPositionInputs inputs = GetVertexPositionInputs(fc - t*h - b*h);
						o.positionCS = inputs.positionCS;
						o.positionWS = inputs.positionWS;
						o.baseUV = baseUV1;
						o.normalUV = normalUV1;
						o.damageUV = damageUV[0];
						stream.Append(o);

						inputs = GetVertexPositionInputs(fc + t*h - b*h);
						o.positionCS = inputs.positionCS;
						o.positionWS = inputs.positionWS;
						o.baseUV = baseUV2;
						o.normalUV = normalUV2;
						o.damageUV = damageUV[1];
						stream.Append(o);


						inputs = GetVertexPositionInputs(fc - t*h + b*h);
						o.positionCS = inputs.positionCS;
						o.positionWS = inputs.positionWS;
						o.baseUV = baseUV3;
						o.normalUV = normalUV3;
						o.damageUV = damageUV[2];
						stream.Append(o);

						inputs = GetVertexPositionInputs(fc + t*h + b*h);
						o.positionCS = inputs.positionCS;
						o.positionWS = inputs.positionWS;
						o.baseUV = baseUV4;
						o.normalUV = normalUV4;
						o.damageUV = damageUV[3];
						stream.Append(o);

						stream.RestartStrip();
					}
				}
			}



			// FRAGMENT SHADER
			half4 frag(g2f IN) : SV_Target
			{

				float fogStr = 1.0 - ComputeFogIntensity(ComputeFogFactor(IN.positionCS.z));
				fogStr = clamp(pow(1.0 - clamp(IN.positionCS.z * 30000.0, 0, 1), 6.0), 0.0, 1.0);


				float3 steppedWS = floor(IN.positionWS * 16 + 0.001) / 16;
				float4 shadowCoord = TransformWorldToShadowCoord(steppedWS);
				half shadow = MainLightRealtimeShadow(shadowCoord);
				shadow = clamp(shadow, 0.3, 1.0);

				Light l = GetMainLight(shadowCoord, IN.positionWS, 1.0);
				half light = saturate(dot(normalize(IN.normalWS), l.direction)) * 0.8 + 0.2;
				light *= shadow;

				half4 baseColor = SAMPLE_TEXTURE2D(_TextureAtlas, sampler_TextureAtlas, IN.baseUV);
				baseColor += baseColor * SAMPLE_TEXTURE2D(_EdgeNormalAtlas, sampler_EdgeNormalAtlas, IN.normalUV);

				float damageMask = clamp(SAMPLE_TEXTURE2D(_DamageAtlas, sampler_DamageAtlas, IN.damageUV).a, 0, .75);

				half3 c = lerp(baseColor, half3(0,0,0), damageMask) * light;
				c = lerp(c, unity_FogColor, fogStr);

				//return half4(baseColor.rgb * light * shadow, 1);
				return half4(c, 1);

				//return half4(IN.baseUV * light, 0, 1);
			}
			ENDHLSL
		}
		

	}
}
