Shader "Test/Shader_ShaderLabTest"
{
	Properties
	{
		[MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
		_TexelSteps("Texel Steps", int) = 16
	}

	SubShader 
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
		}

		// Forward Pass
		Pass
		{

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _RECEIVE_SHADOWS_ON
			#pragma multi_compile _MAIN_LIGHT_SHADOWS

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			float4 _BaseColor;
			int _TexelSteps;


			struct appdata
			{
				float4 positionOS : POSITION;
			};
			struct v2f 
			{
				float4 positionCS : SV_POSITION;	
				float3 positionWS : POSITION1;
				float4 shadowCoord : POSITION2;
			};


			v2f vert(appdata v) 
			{
				v2f o = (v2f)0;
				o.positionCS = TransformObjectToHClip(v.positionOS);
				o.positionWS = GetVertexPositionInputs(v.positionOS).positionWS;

				float4 steppedPos = floor(v.positionOS * _TexelSteps) / _TexelSteps;
				o.shadowCoord = TransformWorldToShadowCoord(steppedPos);
				return o;
			}



			float4 frag(v2f i) : SV_TARGET
			{

				float3 steppedPos = floor(i.positionWS * _TexelSteps) / _TexelSteps;
				float4 shadowCoord = TransformWorldToShadowCoord(steppedPos);
				Light mainLight = GetMainLight(shadowCoord, i.positionWS, 1.0);
				
				half shadow = MainLightRealtimeShadow(shadowCoord);
				shadow *= AdditionalLightRealtimeShadow(0, steppedPos);
				return float4(shadow, shadow, shadow, 1);
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
