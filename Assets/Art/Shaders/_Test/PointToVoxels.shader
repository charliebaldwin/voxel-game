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
			#pragma exclude_renderers metal

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
			
			#include "Assets/Art/Shaders/VoxelShaderConstants.hlsl"
			#include "Assets/Art/Shaders/VoxelShaderHelpers.hlsl"

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
			struct SourceVoxel
			{
			float4 positionOS 			: POSITION;
				float4 uv0 				: TEXCOORD0;
				float4 uv1 				: TEXCOORD1;
				float4 uv2 				: TEXCOORD2;
			};
			struct Voxel
			{
				float4 	positionOS 		: POSITION;
				float4 	uv0 			: TEXCOORD0;
				float4 	uv1 			: TEXCOORD1;
				float4 	uv2 			: TEXCOORD2;
				uint 	texIndex0		: TEXINDEX0;
				uint 	texIndex1		: TEXINDEX1;	
				uint 	texIndex2		: TEXINDEX2;
				uint 	texIndex3		: TEXINDEX3;
				uint 	texIndex4		: TEXINDEX4;
				uint 	texIndex5		: TEXINDEX5;
				uint    solidFaces		: SOLIDFACES;
				uint	damage 			: DAMAGE;
				uint		solidNeighbors	: SOLIDNEIGHBORS;
			};

			struct Vertex
			{
				float4 positionCS 		: SV_POSITION;
				float3 positionWS 		: POSITION1;
				float3 normalWS 		: NORMAL;
				float2 baseUV 			: TEXCOORD0;
				float2 normalUV 		: TEXCOORD1;
				float2 damageUV 		: TEXCOORD2;
				float  ao 				: AMBIENTOCCLUSION;
				float4 color 			: COLOR;
				//float4 tempC : TEXCOORD2;
			};


			// VOXEL/POINT SHADER
			Voxel vert(SourceVoxel IN)
			{
				Voxel OUT;

				OUT.positionOS = IN.positionOS;

				OUT.texIndex0 = (uint)IN.uv0.x;
				OUT.texIndex1 = (uint)IN.uv0.y;
				OUT.texIndex2 = (uint)IN.uv0.z;
				OUT.texIndex3 = (uint)IN.uv0.w;
				OUT.texIndex4 = (uint)IN.uv1.x;
				OUT.texIndex5 = (uint)IN.uv1.y;

				OUT.solidFaces =(uint)IN.uv2.x;
				OUT.damage = (uint)IN.uv2.y;
				OUT.solidNeighbors = (uint)IN.uv2.z;

				return OUT;
			}



			// GEOMETRY SHADER
			[maxvertexcount(24)]
			void geo(point Voxel IN[1], inout TriangleStream<Vertex> stream)
			{
				float3 posOS = IN[0].positionOS;

				float4 color = float4(0,0,0,1);

				int texArray[6] = {IN[0].texIndex0, IN[0].texIndex1, IN[0].texIndex2, IN[0].texIndex3, IN[0].texIndex4, IN[0].texIndex5};
				uint solidFaces = IN[0].solidFaces;
				uint damage = IN[0].damage;
				
				uint solidNeighbors = IN[0].solidNeighbors;
				float sumNeighbors = 0;
				for (int i = 0; i < 27; i++)
				{
					sumNeighbors += (solidNeighbors >> i) & 1;
				}
				//color.r = sumNeighbors / 27.0;
				color.r = (solidNeighbors >> 8 & 1);
				color.g = (solidNeighbors >> 2 & 1);
				color.b = (solidNeighbors >> 3 & 1);

				// Camera in object space, for optional per-face culling
				float3 camOS = TransformWorldToObject(GetCameraPositionWS());

				[unroll]
				for (int f = 0; f < 6; f++)
				{
					// run per-face
					if ((solidFaces >> f) & 1)
					{
						float3 norm = FaceN[f], tan = FaceT[f], bitan = FaceB[f];
						float3 center = posOS + norm * _Size; // - float3(0,0,0);

						int tex = texArray[f];
						int edgeTile = getEdgeTile(f, solidFaces);			

						// Skip faces pointing away from the camera -> "up to 6" faces
						if (dot(norm, camOS - center) <= 0) continue;

						// Triangle strip order: (-t,-b) (+t,-b) (-t,+b) (+t,+b)
						float3 offset[4] = {
							-tan-bitan,
							+tan-bitan,
							-tan+bitan,
							+tan+bitan,
						};
						float3 offset2[4] = {
							-tan, +tan, -tan, +tan
						};
						float3 offset3[4] = {
							-bitan, -bitan, +bitan, +bitan
						};
						// add points
						Vertex o;
						o.normalWS = TransformObjectToWorldNormal(norm);
						for(int i = 0; i < 4; i++)
						{
							VertexPositionInputs inputs = GetVertexPositionInputs(center + _Size*offset[i]);
							o.positionCS = inputs.positionCS;
							o.positionWS = inputs.positionWS;
							o.baseUV =		flipbook(UVs[f][i], _Cols, _Rows, tex, float2(0,0));
							o.normalUV = 	flipbook(UVs[f][i], 4, 4, edgeTile, float2(0,1));
							o.damageUV = 	flipbook(UVs[f][i], 6, 1, damage, float2(0,0));
							//o.ao = sumNeighbors;// / 27.0;
							o.color = float4((offset[i]+1)/2 , 1);
							o.color *= float4(1,0,0,1);

							uint3 aoPos1 = uint3( offset[i] + norm + 1);
							//aoPos1 = uint3(offset[i]) + 1 + uint3(norm);
							uint3 aoPos2 = uint3(offset2[i] + norm + 1);
							uint3 aoPos3 = uint3(offset3[i] + norm + 1);

							o.color = float4(aoPos1-1, 1);


							o.ao = 	((solidNeighbors >> (aoPos1.x + aoPos1.y * 3 + aoPos1.z * 9)) & 1);
							o.ao += ((solidNeighbors >> (aoPos2.x + aoPos2.y * 3 + aoPos2.z * 9)) & 1);
							o.ao += ((solidNeighbors >> (aoPos3.x + aoPos3.y * 3 + aoPos3.z * 9)) & 1);
							o.ao /= 3.0;
							stream.Append(o);
						}

						stream.RestartStrip();
					}
				}
			}




			// FRAGMENT SHADER

			half4 frag(Vertex IN) : SV_Target
			{
				half shadow = GetShadow(IN.positionWS, IN.normalWS);	
				half light = GetLight(IN.normalWS);
				light *= shadow;

				half4 c = SAMPLE_TEXTURE2D(_TextureAtlas, sampler_TextureAtlas, IN.baseUV);
				half alpha = c.a;
				c = pow(1-IN.ao, 1) * float4(0.5,0.5,0.5,1);//float4(abs(IN.normalWS), 1);
				c += c * .7 * SAMPLE_TEXTURE2D(_EdgeNormalAtlas, sampler_EdgeNormalAtlas, IN.normalUV);

				//if (IN.color.r > 0)
				//c = IN.color;
				//c= lerp(c, half4(0,0,0,0), pow(IN.ao,2));

				float damageMask = clamp(SAMPLE_TEXTURE2D(_DamageAtlas, sampler_DamageAtlas, IN.damageUV).a, 0, .75);
				c = lerp(c, half4(0,0,0,0), damageMask);

				c = c * light;
				if (FOG_LINEAR || FOG_EXP || FOG_EXP2) {
					half fog = GetFog(IN.positionCS);
					c = lerp(c, unity_FogColor, fog*unity_FogColor.a);
				}

				c.a=alpha;

				//c = IN.color;
				return half4(c.rgb, alpha);

			}
			ENDHLSL
		}
		

	}
}
