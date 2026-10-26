using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.AdaptivePerformance.Editor;
using UnityEngine;


public class VoxelGenerator : MonoBehaviour
{
	public ComputeShader NoiseCS;
	//private RenderTexture noiseRT;
	private ComputeBuffer noiseBuffer1;
	private ComputeBuffer erosionBuffer;
	private ComputeBuffer noiseBuffer2;

	private float[,,] noiseArray1;
	private float[,,] noiseArray2;
	private float[,,] erosionArray;

	private float[,,] noiseSand;
	private float[,,] noiseSilt;
	private float[,,] noiseClay;

	private float[,,] noiseCaves;
	private bool[,,] exposedToSky;


	[SerializeField]
	private Vector3Int worldSize = new Vector3Int(512, 64, 512);
	private WorldGenSettings worldSettings;
	private Voxel[,,] voxels;

	public float Scale = 0.02f;
	public float HeightRange = 10f;
	public float HeightOffset = 40f;
	public int Octaves = 3;
	public float OctaveStrength = 0.3f;
	public float OctaveScale = 1.5f;

	public int Seed = 1;
	private float seedOffset;

	public AnimationCurve CaveHeightCurve;
	public float CaveThreshold = 0.3f;
	public float CaveWidth = 0.08f;
	public bool InvertCaves;

	public bool DoStoneLayers = false;
	public Vector3 SoilWeight = new Vector3(1f, 1f, 1f);
	private Vector3 soilNorm;


	public void Generate(Vector3Int worldVoxelSize, WorldGenSettings worldGenSettings)
	{
		worldSize = worldVoxelSize;
		worldSettings = worldGenSettings;

		soilNorm = SoilWeight.normalized;

		voxels = new Voxel[worldSize.x, worldSize.y, worldSize.z];

		UnityEngine.Random.InitState(Seed);
		seedOffset = UnityEngine.Random.Range(0f, 999999f);


		GenerateNoiseGPU(out noiseArray1, 0.2f * Vector3.one, Seed, Octaves);
		//GenerateNoiseGPU(out noiseArray2, 0.2f * Vector3.one, Seed + 1, Octaves);
		ErosionGPU();

		LoopXZ(BuildTerrain);


		GenerateNoiseGPU(out noiseSand, new Vector3(1f, 0.9f, 1f), Seed + 2, Octaves + 2);
		GenerateNoiseGPU(out noiseSilt, new Vector3(1f, 0.9f, 1f), Seed + 3, Octaves + 2);
		GenerateNoiseGPU(out noiseClay, new Vector3(1f, 0.9f, 1f), Seed + 4, Octaves + 2);
		LoopXYZ(SoilNoise);

		//GenerateNoiseGPU(out noiseCaves, 2f * new Vector3(0.3f, 0.6f, 0.3f), Seed + 5, Octaves, "GenerateNoiseCaves");
		//LoopXYZ(CarveCaves);

		exposedToSky = new bool[worldSize.x, worldSize.y, worldSize.z];
		LoopXZ(CheckSkyExposure);

		LoopXYZ(AddGrass);

	}


	public Voxel[,,] GetGeneratedVoxels()
	{
		return voxels;
	}

	#region GET/SET
	private bool SetVoxel(int x, int y, int z, Voxel voxel)
	{
		if (x < worldSize.x - 1 && y < worldSize.y - 1 && z < worldSize.z - 1 && x > 0 && y > 0 && z > 0)
		{
			voxels[x, y, z] = voxel;
			return true;
		}
		else
		{
			return false;
		}
	}
	private Voxel GetVoxel(int x, int y, int z)
	{
		if (x < worldSize.x - 1 && y < worldSize.y - 1 && z < worldSize.z - 1 && x > 0 && y > 0 && z > 0)
		{
			return voxels[x, y, z];
		}
		else
		{
			return new Voxel(BlockID.Invalid, 0, 0);
		}
	}
	#endregion

	#region LOOPS
	private void LoopXYZ(Action<int, int, int> loopFunction)
	{
		Vector3Int Size3D = new Vector3Int(worldSize.x, worldSize.y, worldSize.z);
		for (int x = 0; x < Size3D.x; x++)
		{
			for (int z = 0; z < Size3D.z; z++)
			{
				for (int y = 0; y < Size3D.y; y++)
				{
					loopFunction(x, y, z);
				}
			}
		}
	}
	private void LoopXZ(Action<int, int> loopFunction)
	{
		Vector2Int Size2D = new Vector2Int(worldSize.x, worldSize.z);
		for (int x = 0; x < Size2D.x; x++)
		{
			for (int z = 0; z < Size2D.y; z++)
			{
				loopFunction(x, z);
			}
		}
	}
	#endregion

	#region PASSES 
	private void GenerateNoiseGPU(out float[,,] noiseArray, Vector3 scale, int seed, int octaves)
	{
		GenerateNoiseGPU(out noiseArray, scale, seed, octaves, "GenerateNoise");
	}
	private void GenerateNoiseGPU(out float[,,] noiseArray, Vector3 scale, int seed, int octaves, string kernel)
	{
		// prep outputs
		float[,,] result = new float[worldSize.x, worldSize.y, worldSize.z];
		float[] noiseArray1d = new float[worldSize.x * worldSize.y * worldSize.z];
		noiseBuffer1 = new ComputeBuffer(worldSize.x * worldSize.y * worldSize.z, sizeof(float));

		// initialize compute shader params
		int[] size = new int[3] { worldSize.x, worldSize.y, worldSize.z };
		int[] threads = new int[3] { 8, 8, 8 };
		int[] threadSize = new int[3] { size[0] / threads[0], size[1] / threads[1], size[2] / threads[2] };
		int kernelID = NoiseCS.FindKernel(kernel);
		NoiseCS.SetBuffer(kernelID, "NoiseBuffer", noiseBuffer1);
		NoiseCS.SetInts("WorldSize", size);
		NoiseCS.SetInts("ThreadCount", threads);
		NoiseCS.SetInts("ThreadSize", threadSize);
		NoiseCS.SetFloat("Seed", Seed);
		NoiseCS.SetVector("Scale", Scale * scale);
		UnityEngine.Random.InitState(seed);
		Vector3 offset = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(-100000f, 100000f);
		NoiseCS.SetVector("Offset", offset);
		NoiseCS.SetInt("Octaves", octaves);
		NoiseCS.SetFloat("OctaveScale", OctaveScale);
		NoiseCS.SetFloat("OctaveStrength", OctaveStrength);
		NoiseCS.SetFloat("CaveWidth", CaveWidth);

		// dispatch & read back compute shader outputs
		NoiseCS.Dispatch(kernelID, 1, 1, 1);// threads[0], threads[1], threads[2]);
		noiseBuffer1.GetData(noiseArray1d);

		// convert data to 3D array
		for (int x = 0; x < worldSize.x; x++)
			for (int z = 0; z < worldSize.z; z++)
				for (int y = 0; y < worldSize.y; y++)
				{
					result[x, y, z] = noiseArray1d[x + worldSize.x * y + worldSize.x * worldSize.y * z];
				}

		noiseArray = result;
	}

	private void ErosionGPU()
	{
		float[,,] result = new float[worldSize.x, worldSize.y, worldSize.z];
		float[] erosionArray1d = new float[worldSize.x * worldSize.y * worldSize.z];
		erosionBuffer = new ComputeBuffer(worldSize.x * worldSize.y * worldSize.z, sizeof(float));
		int[] size = new int[3] { worldSize.x, worldSize.y, worldSize.z };
		int[] threads = new int[3] { 8, 8, 8 };
		int[] threadSize = new int[3] { size[0] / threads[0], size[1] / threads[1], size[2] / threads[2] };
		int kernelID = NoiseCS.FindKernel("GenerateErosion");
		NoiseCS.SetBuffer(kernelID, "NoiseBuffer", noiseBuffer1);
		NoiseCS.SetBuffer(kernelID, "ErosionBuffer", erosionBuffer);
		NoiseCS.SetInts("WorldSize", size);
		NoiseCS.SetInts("ThreadCount", threads);
		NoiseCS.SetInts("ThreadSize", threadSize);

		// dispatch & read back compute shader outputs
		NoiseCS.Dispatch(kernelID, 1, 1, 1);// threads[0], threads[1], threads[2]);
		erosionBuffer.GetData(erosionArray1d);

		// convert data to 3D array
		for (int x = 0; x < worldSize.x; x++)
			for (int z = 0; z < worldSize.z; z++)
				for (int y = 0; y < worldSize.y; y++)
				{
					result[x, y, z] = erosionArray1d[x + worldSize.x * y + worldSize.x * worldSize.y * z];
				}

		erosionArray = result;
	}


	private void GenerateNoiseCPU3D(out float[,,] noiseArray, Vector3 scale, int seed, int octaves)
	{
		float[,,] result = new float[worldSize.x, worldSize.y, worldSize.z];
		Vector3Int Size3D = new Vector3Int(worldSize.x, worldSize.y, worldSize.z);

		UnityEngine.Random.InitState(seed);
		Vector3 offset = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(-100000f, 100000f);

		for (int x = 0; x < Size3D.x; x++)
		{
			for (int z = 0; z < Size3D.z; z++)
			{
				for (int y = 0; y < Size3D.y; y++)
				{
					result[x, y, z] = Perlin.Fbm(x * worldSettings.NoiseScale * scale.x + offset.x + seedOffset, y * worldSettings.NoiseScale * scale.y + offset.y + seedOffset, z * worldSettings.NoiseScale * scale.z + offset.z + seedOffset, octaves);
				}
			}
		}
		noiseArray = result;
	}
	private void GenerateNoiseCPU2D(out float[,,] noiseArray, Vector3 scale, int seed, int octaves)
	{
		float[,,] result = new float[worldSize.x, worldSize.y, worldSize.z];

		UnityEngine.Random.InitState(seed);
		Vector3 offset = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(-100000f, 100000f);

		for (int x = 0; x < worldSize.x; x++)
		{
			for (int z = 0; z < worldSize.z; z++)
			{
				float n = Perlin.Fbm(x * worldSettings.NoiseScale * scale.x + offset.x + seedOffset, z * worldSettings.NoiseScale * scale.z + offset.z + seedOffset, octaves);

				for (int y = 0; y < worldSize.y; y++)
				{
					result[x, y, z] = n;
				}
			}
		}
		noiseArray = result;
	}

	private void BuildTerrain(int x, int z)
	{
		//float noise1 = noiseArray1[x, 2, z];
		//float noise2 = noiseArray2[x, 2, z];
		//float h = noise1 * HeightRange + HeightOffset;
		//h += noise2 * HeightRange * 4;

		float h = erosionArray[x, 2, z] * HeightRange + HeightOffset;

		for (int y = 0; y < worldSize.y; y++)
		{
			float depth = h - y;
			if (depth < 0f)
			{
				SetVoxel(x, y, z, new Voxel(BlockID.Air));
			}
			else
			{
				SetVoxel(x, y, z, new Voxel(BlockID.Dirt));

				// Stone Layers
				if (DoStoneLayers)
				{
					UnityEngine.Random.InitState(x + y + z);
					int r = Mathf.FloorToInt(UnityEngine.Random.Range(0f, 5.99f));
					OrthoNormal o1 = VoxelHelper.OrthoDirections[r];
					OrthoNormal o2 = VoxelHelper.OrthoDirections[(r + 2) % 5];
					o1 = OrthoNormal.up;
					o2 = OrthoNormal.forward;

					if (depth < 4f)
					{
						SetVoxel(x, y, z, new Voxel(BlockID.Dirt));
					}
					else if (depth < 8f)
					{
						SetVoxel(x, y, z, new Voxel(BlockID.Rocky_Dirt, o1, o2));
					}
					else if (depth < 15f)
					{
						SetVoxel(x, y, z, new Voxel(BlockID.Stone_Sandstone, o1, o2));
					}
					else if (depth < 22f)
					{
						SetVoxel(x, y, z, new Voxel(BlockID.Stone_Limestone, 0, 0, BlockShape.Solid));
					}
					else if (depth < 30f)
					{
						SetVoxel(x, y, z, new Voxel(BlockID.Stone_Dolomite, o1, o2));
					}
					else if (depth < 40f)
					{
						SetVoxel(x, y, z, new Voxel(BlockID.Stone_Shale, o1, o2));
					}
					else if (depth < 52f)
					{
						SetVoxel(x, y, z, new Voxel(BlockID.Stone_Slate, 0, 0, BlockShape.Solid));
					}
					else //if (depth < 65f)
					{
						SetVoxel(x, y, z, new Voxel(BlockID.Stone_Basalt, o1, o2));
					}
				}

				// Caves / Color Testing
				//float color_r = Mathf.Abs(h);
				//float color_g = Mathf.Abs(ReadNoiseTex(x, 20, z, ref noiseArray));
				//float color_b = Mathf.Abs(ReadNoiseTex(x, 40, z, ref noiseArray));

				//float c = ReadNoiseTex(x, 7, z, ref noiseArray2);

				//bool cave = false;
				////if (c > 0.05f) cave = true;
				//if ((((float)y / worldSize.y) - color_g) * 2f < 2f * c)
				//{
				//    cave = true;
				//}
				//Color color = Color.black;
				//if (!cave)
				//{
				//    SetVoxel(x, y, z, new Voxel(BlockID.Air));
				//    color = new Color(color_r, color_g, color_b).NormalizeRGB();
				//    //SetVoxel(x, y, z, new Voxel(color));
				//}   //SetVoxel(x, y, z, new Voxel(Color.black));
				//else
				//{
				//    SetVoxel(x, y, z, new Voxel(BlockID.Dirt));
				//    //SetVoxel(x, y, z, new Voxel(color)
				//}
			}
		}
	}

	private void CheckSkyExposure(int x, int z)
	{
		bool stillExposed = true;
		for (int y = worldSize.y - 2; y > 0; y--)
		{
			if (stillExposed)
			{
				exposedToSky[x, y, z] = true;
				if (GetVoxel(x, y, z).BlockID != BlockID.Air)
				{
					// SetVoxel(x, y, z, new Voxel(BlockID.Clay));
					stillExposed = false;
				}
			}
			else
			{
				exposedToSky[x, y, z] = false;
			}
		}
	}

	private void SoilNoise(int x, int y, int z)
	{
		if (GetVoxel(x, y, z).BlockID == BlockID.Dirt)
		{

			float sand = noiseSand[x, y, z];
			float silt = noiseSilt[x, y, z];
			float clay = noiseClay[x, y, z];

			Color soilMix = new Color(sand, silt, clay);
			Color.RGBToHSV(soilMix, out float hue, out float sat, out float val);
			if (sat < 0.02f)
			{
				SetVoxel(x, y, z, new Voxel(BlockID.Stone)); // even mixture (loam)
			}
			else
			{
				if (hue < soilNorm.x) // red (sand)
				{
					SetVoxel(x, y, z, new Voxel(BlockID.Sand));
				}
				else if (hue < soilNorm.x + soilNorm.y) // green (silt)
				{
					SetVoxel(x, y, z, new Voxel(BlockID.Silt));
				}
				else // blue (clay)
				{
					SetVoxel(x, y, z, new Voxel(BlockID.Clay));
				}
			}
		}

	}
	private void AddGrass(int x, int y, int z)
	{
		if (exposedToSky[x, y, z])
		{
			if (GetVoxel(x, y, z).BlockID == BlockID.Dirt)
			{
				SetVoxel(x, y, z, new Voxel(BlockID.Grass));
				//SetVoxel(x, y + 1, z, new Voxel(BlockID.Foliage_Grass, BlockShape.Foliage));

			}
			else if (GetVoxel(x, y, z).BlockID == BlockID.Sand)
			{
				SetVoxel(x, y, z, new Voxel(BlockID.GrassySand));
				//SetVoxel(x, y + 1, z, new Voxel(BlockID.Foliage_Grass, BlockShape.Foliage));

			}
			else if (GetVoxel(x, y, z).BlockID == BlockID.Silt)
			{
				SetVoxel(x, y, z, new Voxel(BlockID.GrassySilt));
				// SetVoxel(x, y+1, z, new Voxel(BlockID.Foliage_Grass, BlockShape.Foliage));
			}
			else if (GetVoxel(x, y, z).BlockID == BlockID.Clay)
			{
				SetVoxel(x, y, z, new Voxel(BlockID.GrassyClay));
				// SetVoxel(x, y + 1, z, new Voxel(BlockID.Foliage_Grass, BlockShape.Foliage));

			}
		}
	}

	private void CarveCaves(int x, int y, int z)
	{
		if (y < 2 || GetVoxel(x, y, z).BlockID == BlockID.Air) return;
		float noise = noiseCaves[x, y, z];

		//float height = (float)y / (float)worldSize.y;
		//height = CaveHeightCurve.Evaluate(height);
		////height = Mathf.Lerp(CaveThreshold, 1f, height);
		//noise *= height;

		if (InvertCaves)
		{
			if (noise < CaveThreshold)
			//if (height > 0.5f)
			{
				SetVoxel(x, y, z, new Voxel(BlockID.Air));
			}
		}
		else
		{
			if (noise > CaveThreshold)
			//if (height > 0.5f)
			{
				SetVoxel(x, y, z, new Voxel(BlockID.Air));
			}
		}

	}

	#endregion

}
