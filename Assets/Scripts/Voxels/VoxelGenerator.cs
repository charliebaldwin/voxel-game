using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System;
using System.Threading;
using Unity.Collections;
using Unity.Mathematics;

//using Unity.Mathematics;
using UnityEditor;
using UnityEditor.AdaptivePerformance.Editor;
using UnityEngine;


public class VoxelGenerator : MonoBehaviour
{
	public ComputeShader NoiseCS;
	public int3 ThreadGroups = new int3(16, 1, 16);
	public int3 Threads = new int3(1, 16, 1);
	//private RenderTexture noiseRT;
	//private ComputeBuffer noiseBuffer1;
	//private ComputeBuffer erosionBuffer;
	//private ComputeBuffer noiseBuffer2;
	ComputeBuffer heightBuffer;
	ComputeBuffer voxelBufferR;
	ComputeBuffer voxelBufferW;
	ComputeBuffer soilBuffer;
	ComputeBuffer randomBuffer;

	private float[,,] noiseArray1;
	private float[,,] noiseArray2;
	private float[,,] erosionArray;
	private int[,,] voxelArray;

	private float[,,] noiseSand;
	private float[,,] noiseSilt;
	private float[,,] noiseClay;

	private float[,,] noiseCaves;
	private bool[,,] exposedToSky;
	private int[,,] randomInts;


	[SerializeField]
	private Vector3Int worldSize = new Vector3Int(512, 64, 512);
	private Vector3Int worldOrigin;// = Vector3Int.zero;
	private WorldGenSettings worldSettings;
	private Voxel[,,] voxels;

	public float Scale = 0.02f;
	public float HeightRange = 10f;
	public float HeightOffset = 40f;
	public int Octaves = 3;
	public float OctaveStrength = 0.3f;
	public float OctaveScale = 1.5f;

	public int Seed = 1;

	public AnimationCurve CaveHeightCurve;
	public float CaveThreshold = 0.3f;
	public float CaveWidth = 0.08f;
	public bool InvertCaves;

	public bool DoStoneLayers = false;
	public Vector3 SoilWeight = new Vector3(1f, 1f, 1f);
	private Vector3 soilNorm;


	int GetNextSeed(int seed)
	{
		UnityEngine.Random.InitState(seed);
		int newSeed = Mathf.FloorToInt(UnityEngine.Random.Range(0, 999999));
		//Debug.Log($"seed:{newSeed}");
		return newSeed;
	}
	public void Generate(Vector3Int worldSize, Vector3Int worldOrigin)
	{
		this.worldSize = worldSize;
		this.worldOrigin = worldOrigin;
		//worldSettings = worldGenSettings;

		soilNorm = SoilWeight.normalized;

		voxels = new Voxel[this.worldSize.x, this.worldSize.y, this.worldSize.z];

		UnityEngine.Random.InitState(Seed);
		int seed = GetNextSeed(Seed);

		int bufferSize = worldSize.x * worldSize.y * worldSize.z;

		float[] noiseArray1d = new float[bufferSize];
		int[] voxelArray1d = new int[bufferSize];
		noiseArray1 = new float[this.worldSize.x, this.worldSize.y, this.worldSize.z];
		voxelArray = new int[this.worldSize.x, this.worldSize.y, this.worldSize.z];
		int[] randomArray1d = new int[bufferSize];
		randomInts = new int[worldSize.x, worldSize.y, worldSize.z];

		// heightBuffer = new ComputeBuffer(bufferSize / worldSize.y, sizeof(float));
		heightBuffer = new ComputeBuffer(bufferSize, sizeof(float));
		voxelBufferR = new ComputeBuffer(bufferSize, sizeof(int));
		voxelBufferW = new ComputeBuffer(bufferSize, sizeof(int));
		soilBuffer = new ComputeBuffer(bufferSize, 3 * sizeof(float));
		randomBuffer = new ComputeBuffer(bufferSize, sizeof(int));

		ComputePass(seed, "InitialHeight");
		//ComputePass(seed, "SetStone");
		// ComputePass(seed, "ErodeSurface");
		// ComputePass(seed, "AddSoilLayer");
		// ComputePass(seed, "AddWater");
		// ComputePass(seed, "RandomInt");
		//GenerateNoiseGPU(0.2f * Vector3.one, seed, Octaves, "AddTrees");

		voxelBufferR.GetData(voxelArray1d);
		randomBuffer.GetData(randomArray1d);

		for (int x = 0; x < worldSize.x; x++)
			for (int z = 0; z < worldSize.z; z++)
				for (int y = 0; y < worldSize.y; y++)
				{
					voxelArray[x, y, z] = voxelArray1d[x + worldSize.x * y + worldSize.x * worldSize.y * z];
					randomInts[x, y, z] = randomArray1d[x + worldSize.x * y + worldSize.x * worldSize.y * z];
				}

		seed = GetNextSeed(seed);
		// GenerateNoiseGPU(out noiseArray2, 0.2f * Vector3.one, seed, Octaves);
		// seed = GetNextSeed(seed);
		//ErosionGPU();

		//LoopXZ(BuildTerrain);
		LoopXYZ(BuildTerrain2);

		// exposedToSky = new bool[this.worldSize.x, this.worldSize.y, this.worldSize.z];
		// LoopXZ(CheckSkyExposure);

		LoopXYZ(AddGrass);

		heightBuffer.Release();
		voxelBufferR.Release();

	}


	public Voxel[,,] GetGeneratedVoxels()
	{
		return voxels;
	}
	public Voxel[,,] GetGeneratedVoxels(int margin)
	{
		int3 croppedSize = new int3(worldSize.x - 2 * margin, worldSize.y - 2 * margin, worldSize.z - 2 * margin);
		Voxel[,,] result = new Voxel[croppedSize.x, croppedSize.y, croppedSize.z];
		for (int x = 0; x < croppedSize.x; x++)
		{
			for (int y = 0; y < croppedSize.y; y++)
			{
				for (int z = 0; z < croppedSize.z; z++)
				{
					result[x, y, z] = voxels[x + margin, y + margin, z + margin];
				}
			}
		}
		return result;
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
			return new Voxel(-1);
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

	private void ComputePass(int seed, string kernel)
	{
		int kernelID = NoiseCS.FindKernel(kernel);

		// buffers
		NoiseCS.SetBuffer(kernelID, "NoiseBuffer", heightBuffer);
		NoiseCS.SetBuffer(kernelID, "SoilBuffer", soilBuffer);
		NoiseCS.SetBuffer(kernelID, "VoxelBufferW", voxelBufferW);
		NoiseCS.SetBuffer(kernelID, "VoxelBufferR", voxelBufferR);
		NoiseCS.SetBuffer(kernelID, "RandomBuffer", randomBuffer);

		// settings
		int[] size = new int[3] { worldSize.x, worldSize.y, worldSize.z };
		int[] origin = new int[3] { worldOrigin.x, worldOrigin.y, worldOrigin.z };
		int[] threads = new int[3] { Threads.x, Threads.y, Threads.z };
		int[] threadSize = new int[3] { size[0] / threads[0], size[1] / threads[1], size[2] / threads[2] };
		int[] threadGroups = new int[3] { ThreadGroups.x, ThreadGroups.y, ThreadGroups.z };

		NoiseCS.SetInts("WorldSize", size);
		NoiseCS.SetInts("WorldOrigin", origin);
		NoiseCS.SetInts("ThreadCount", threads);
		NoiseCS.SetInts("ThreadGroups", threadGroups);
		NoiseCS.SetInts("ThreadSize", threadSize);
		NoiseCS.SetInt("Seed", seed);
		NoiseCS.SetVector("Scale", Scale * Vector3.one);
		NoiseCS.SetInt("Octaves", Octaves);
		NoiseCS.SetFloat("OctaveScale", OctaveScale);
		NoiseCS.SetFloat("OctaveStrength", OctaveStrength);
		NoiseCS.SetFloat("CaveWidth", CaveWidth);


		// DISPATCH
		NoiseCS.Dispatch(kernelID, ThreadGroups.x, ThreadGroups.y, ThreadGroups.z);// threads[0], threads[1], threads[2]);


		voxelBufferR = voxelBufferW;


	}

	private void ErosionGPU()
	{
		float[,,] result = new float[worldSize.x, worldSize.y, worldSize.z];
		float[] erosionArray1d = new float[worldSize.x * worldSize.y * worldSize.z];
		ComputeBuffer erosionBuffer = new ComputeBuffer(worldSize.x * worldSize.y * worldSize.z, sizeof(float));
		ComputeBuffer noiseBuffer1 = new ComputeBuffer(worldSize.x * worldSize.y * worldSize.z, sizeof(float));

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
					result[x, y, z] = Perlin.Fbm(x * scale.x + offset.x, y * scale.y + offset.y, z * scale.z + offset.z, octaves);
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
				float n = Perlin.Fbm(x * scale.x + offset.x, z * scale.z + offset.z, octaves);

				for (int y = 0; y < worldSize.y; y++)
				{
					result[x, y, z] = n;
				}
			}
		}
		noiseArray = result;
	}
	private void BuildTerrain2(int x, int y, int z)
	{
		Voxel v = new Voxel(voxelArray[x, y, z]);
		v.Random = randomInts[x, y, z];
		SetVoxel(x, y, z, v);

	}
	private void BuildTerrain(int x, int z)
	{
		float noise1 = noiseArray1[x, 1, z];
		float noise2 = noiseArray2[x, 2, z];
		float h = noise1 * HeightRange + HeightOffset;
		h += noise2 * HeightRange * 4;

		//float h = erosionArray[x, 2, z] * HeightRange + HeightOffset;

		for (int y = 0; y < worldSize.y; y++)
		{
			float depth = h - y;
			if (depth < 0f)
			{
				SetVoxel(x, y, z, new Voxel(0));
			}
			else
			{
				SetVoxel(x, y, z, new Voxel(1));

				// Stone Layers
				// if (DoStoneLayers)
				// {
				// 	UnityEngine.Random.InitState(x + y + z);
				// 	int r = Mathf.FloorToInt(UnityEngine.Random.Range(0f, 5.99f));
				// 	OrthoNormal o1 = VoxelHelper.OrthoDirections[r];
				// 	OrthoNormal o2 = VoxelHelper.OrthoDirections[(r + 2) % 5];
				// 	o1 = OrthoNormal.up;
				// 	o2 = OrthoNormal.forward;

				// 	if (depth < 4f)
				// 	{
				// 		SetVoxel(x, y, z, new Voxel(Registry.GetVoxelID("silt")));
				// 	}
				// 	else if (depth < 8f)
				// 	{
				// 		SetVoxel(x, y, z, new Voxel(BlockID.Rocky_Dirt, o1, o2));
				// 	}
				// 	else if (depth < 15f)
				// 	{
				// 		SetVoxel(x, y, z, new Voxel(BlockID.Stone_Sandstone, o1, o2));
				// 	}
				// 	else if (depth < 22f)
				// 	{
				// 		SetVoxel(x, y, z, new Voxel(BlockID.Stone_Limestone, 0, 0, BlockShape.Solid));
				// 	}
				// 	else if (depth < 30f)
				// 	{
				// 		SetVoxel(x, y, z, new Voxel(BlockID.Stone_Dolomite, o1, o2));
				// 	}
				// 	else if (depth < 40f)
				// 	{
				// 		SetVoxel(x, y, z, new Voxel(BlockID.Stone_Shale, o1, o2));
				// 	}
				// 	else if (depth < 52f)
				// 	{
				// 		SetVoxel(x, y, z, new Voxel(BlockID.Stone_Slate, 0, 0, BlockShape.Solid));
				// 	}
				// 	else //if (depth < 65f)
				// 	{
				// 		SetVoxel(x, y, z, new Voxel(BlockID.Stone_Basalt, o1, o2));
				// 	}
				// }

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
				if (!GetVoxel(x, y, z).IsEmpty())
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
		if (GetVoxel(x, y, z).BlockType == 1)
		{

			float sand = noiseSand[x, y, z];
			float silt = noiseSilt[x, y, z];
			float clay = noiseClay[x, y, z];

			Color soilMix = new Color(sand, silt, clay);
			Color.RGBToHSV(soilMix, out float hue, out float sat, out float val);
			if (sat < 0.02f)
			{
				SetVoxel(x, y, z, new Voxel("loam")); // even mixture (loam)
			}
			else
			{
				if (hue < soilNorm.x) // red (sand)
				{
					SetVoxel(x, y, z, new Voxel("sand"));
				}
				else if (hue < soilNorm.x + soilNorm.y) // green (silt)
				{
					SetVoxel(x, y, z, new Voxel("silt"));
				}
				else // blue (clay)
				{
					SetVoxel(x, y, z, new Voxel("clay"));
				}
			}
		}

	}
	private void AddGrass(int x, int y, int z)
	{
		Voxel v = GetVoxel(x, y, z);
		if (!v.IsEmpty() && GetVoxel(x, y + 1, z).IsEmpty())
		{
			v.Grassy = true;
			SetVoxel(x, y, z, v);
		}
		// if (exposedToSky[x, y, z])
		// {
		// 	Voxel v = GetVoxel(x, y, z);
		// 	v.Grassy = true;
		// 	SetVoxel(x, y, z, v);
		// }
	}

	private void CarveCaves(int x, int y, int z)
	{
		if (y < 2 || GetVoxel(x, y, z).IsEmpty()) return;
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
				SetVoxel(x, y, z, new Voxel("air"));
			}
		}
		else
		{
			if (noise > CaveThreshold)
			//if (height > 0.5f)
			{
				SetVoxel(x, y, z, new Voxel("air"));
			}
		}

	}

	#endregion

}
