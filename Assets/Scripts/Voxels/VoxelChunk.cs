using NUnit.Framework.Internal;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
//using Unity.Mathematics;
using Unity.Profiling;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using VInspector;
//using static Perlin;
//using static UnityEditor.PlayerSettings;
// static UnityEditor.Searcher.SearcherWindow.Alignment;
using static VoxelHelper;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;


// __   _______  _____ _       ___ _  _ _   _ _  _ _  __
// \ \ / / _ \ \/ / __| |     / __| || | | | | \| | |/ /
//  \ V / (_) >  <| _|| |__  | (__| __ | |_| | .` | ' < 
//   \_/ \___/_/\_\___|____|  \___|_||_|\___/|_|\_|_|\_\


public class VoxelChunk : MonoBehaviour, IInitializable
{

	public Vector3Int ChunkPos;
	public static Vector3Int Size3D = new Vector3Int(16, 32, 16);
	private MeshingMethod meshingMethod = MeshingMethod.CPU;

	// voxel data
	private Voxel[,,] Voxels;// = new Voxel[1, 1, 1];
	private List<Vector3Int> activeVoxels = new List<Vector3Int>();
	private Dictionary<Vector3Int, BlockEntityActor> BlockEntities;


	// status
	bool IInitializable.Initialized { get { return initialized; } }
	private bool initialized = false;
	public bool Loaded = false;
	private bool meshDirty = true;

	// references
	private static VoxelWorld world;
	private VoxelGenerator generator;
	private MeshFilter meshFilter;
	private Mesh mesh;
	private MeshCollider meshCollider;
	private MeshRenderer meshRenderer;
	[SerializeField] private GameObject blockBreakVFXPrefab;
	[SerializeField] private GameObject blockHitVFXPrefab;
	[SerializeField] private ChunkFoliage foliage;
	// neighbors
	private VoxelChunk neighborNX;
	private VoxelChunk neighborPX;
	private VoxelChunk neighborNY;
	private VoxelChunk neighborPY;
	private VoxelChunk neighborNZ;
	private VoxelChunk neighborPZ;

	// data storage for meshing function
	private List<Vector3> vertices = new List<Vector3>();
	private List<Vector3> normals = new List<Vector3>();
	private List<Vector4> uvs = new List<Vector4>();
	private List<int> triangles = new List<int>();
	private List<Color> colors = new List<Color>();
	private List<Vector3> tempVertices = new List<Vector3>();
	private List<Vector3> tempNormals = new List<Vector3>();
	private List<Vector4> tempUvs = new List<Vector4>();
	private List<int> tempTriangles = new List<int>();
	private List<Color> tempColors = new List<Color>();
	private int t = 0;
	Dictionary<Vector3Int, Voxel> neighborCache;
	private List<Vector3> foliageGrassCoords = new List<Vector3>();

	// profiling
	readonly ProfilerMarker meshMarker = new ProfilerMarker("Chunk Mesher");
	readonly ProfilerMarker voxelMarker = new ProfilerMarker("Mesh for Single Voxel");
	private ProfilerRecorder meshRecorder;


	#region INITIALIZATION

	public void Initialize()
	{
		world = World();
		meshingMethod = world.MeshingMethod;
		meshFilter = GetComponent<MeshFilter>();
		meshCollider = GetComponent<MeshCollider>();
		meshRenderer = GetComponent<MeshRenderer>();
		foliage = GetComponentInChildren<ChunkFoliage>();

		meshRenderer.material = world.ChunkMaterials[(int)meshingMethod];

		meshFilter.sharedMesh = new Mesh();
		meshCollider.enabled = false;
		meshRenderer.enabled = false;

		Voxels = new Voxel[Size3D.x, Size3D.y, Size3D.z];
		BlockEntities = new Dictionary<Vector3Int, BlockEntityActor>();


		initialized = true;

	}
	public void GenerateVoxels()
	{
		generator = GetComponent<VoxelGenerator>();
		Voxels = new Voxel[Size3D.x, Size3D.y, Size3D.z];
		int margin = 2;
		generator.Generate(Size3D + (2 * margin * Vector3Int.one), SnapToGrid(transform.position));
		Voxels = generator.GetGeneratedVoxels(margin);
	}

	public VoxelChunk LoadChunk()
	{
		gameObject.SetActive(true);
		meshRenderer.enabled = true;
		meshFilter.sharedMesh = new Mesh();
		Remesh();
		meshCollider.enabled = true;
		foreach (KeyValuePair<Vector3Int, BlockEntityActor> entity in BlockEntities)
		{
			entity.Value.LoadEntity();
		}
		foliage.Load();
		Loaded = true;
		return this;
	}

	public VoxelChunk UnloadChunk()
	{
		Loaded = false;
		meshCollider.enabled = false;
		meshRenderer.enabled = false;
		meshFilter.sharedMesh.Clear();
		foreach (KeyValuePair<Vector3Int, BlockEntityActor> entity in BlockEntities)
		{
			entity.Value.UnloadEntity();
		}
		gameObject.SetActive(false);
		foliage.Unload();
		return this;
	}
	public void FillVoxelData(Voxel[,,] newVoxels)
	{
		Voxels = newVoxels;
		//Debug.Log($"voxel at 5,5,5 = ({Voxels[5, 5, 5].BlockType})");
		//activeVoxels = new List<Vector3Int>();
		//Loop3D(FillActiveVoxelsLoop);
	}

	public void FindNeighbors()
	{
		if (neighborNX == null)
			neighborNX = world.GetChunk(ChunkPos + Vector3Int.left);
		if (neighborPX == null)
			neighborPX = world.GetChunk(ChunkPos + Vector3Int.right);
		if (neighborNY == null)
			neighborNY = world.GetChunk(ChunkPos + Vector3Int.down);
		if (neighborPY == null)
			neighborPY = world.GetChunk(ChunkPos + Vector3Int.up);
		if (neighborNZ == null)
			neighborNZ = world.GetChunk(ChunkPos + Vector3Int.back);
		if (neighborPZ == null)
			neighborPZ = world.GetChunk(ChunkPos + Vector3Int.forward);
	}

	private void SetNeighborsDirty(Vector3Int localVoxelPos)
	{
		Vector3Int dirtyNeighbors = CheckPosOnEdge(localVoxelPos, Size3D);
		if (dirtyNeighbors.x == -1 && neighborNX != null)
			neighborNX.SetDirty();
		if (dirtyNeighbors.x == 1 && neighborPX != null)
			neighborPX.SetDirty();
		if (dirtyNeighbors.y == -1 && neighborNY != null)
			neighborNY.SetDirty();
		if (dirtyNeighbors.y == -1 && neighborPY != null)
			neighborPY.SetDirty();
		if (dirtyNeighbors.z == -1 && neighborNZ != null)
			neighborNZ.SetDirty();
		if (dirtyNeighbors.z == 1 && neighborPZ != null)
			neighborPZ.SetDirty();
	}

	private void Loop3D(Action<int, int, int> loopFunction)
	{
		for (int z = 0; z < Size3D.z; z++)
		{
			for (int y = 0; y < Size3D.y; y++)
			{
				for (int x = 0; x < Size3D.x; x++)
				{
					loopFunction(x, y, z);
				}
			}
		}
	}

	#endregion


	#region UPDATE
	private void FixedUpdate()
	{
		if (meshDirty && Loaded)
		{
			Remesh();
			meshDirty = false;
		}
	}

	public void BlockUpdate()
	{
		BlockUpdateFast();
		//Loop3D(BlockUpdateAction);
	}
	private void BlockUpdateLoop(int x, int y, int z)
	{
		Vector3Int pos = new Vector3Int(x, y, z);
		Voxel voxel = Voxels[x, y, z];


		if (Voxels[x, y, z].Damage > 0 && !PlayerView.usingTool)
		{
			if (BlockRandomEvent(new Vector3Int(x, y, z), 0.05f))
			{
				voxel.Damage -= 1;
				//SetBlock(LocalToWorld(new Vector3Int(x, y, z), ChunkCoord, Size3D), voxel);
				world.SetVoxel(LocalToWorld(new Vector3Int(x, y, z), ChunkPos, Size3D), voxel);
				SetDirty();
			}
		}
	}

	private void FillActiveVoxelsLoop(int x, int y, int z)
	{
		if (Voxels[x, y, z] == null) { Debug.Log("voxel is empty!"); }
		if (!Voxels[x, y, z].IsEmpty())
		{
			activeVoxels.Add(new Vector3Int(x, y, z));
		}
	}

	private void BlockUpdateFast()
	{
		if (activeVoxels.Count > 0)
		{
			//Debug.Log($"updating chunk {ChunkCoord}- there are {activeVoxels.Count} voxels to check");
			Vector3Int[] tempActiveVoxels = new Vector3Int[activeVoxels.Count];
			activeVoxels.CopyTo(tempActiveVoxels);
			foreach (Vector3Int pos in tempActiveVoxels)
			{
				if (IsPosInGridBounds(pos, Size3D))
				{
					Voxel voxel = Voxels[pos.x, pos.y, pos.z];
					switch (voxel.BlockType)
					{
						case (0):
							activeVoxels.Remove(pos);
							break;
						case (1):
							if (pos.y < Size3D.y - 1)
							{
								if (!Voxels[pos.x, pos.y + 1, pos.z].IsEmpty())
								{
									world.SetVoxel(LocalToWorld(pos, ChunkPos, Size3D), new Voxel("silt"));
									meshDirty = true;
								}

							}
							if (voxel.Damage <= 0) activeVoxels.Remove(pos);
							break;
						case (2):
							if (pos.y < Size3D.y - 1)
							{
								//Voxel upVoxel = World().LookupVoxel(LocalToWorldPos((pos + new Vector3Int(0, 1, 0))));
								Voxel upVoxel = GetVoxelLocal(pos + Vector3Int.up);
								if (upVoxel.IsEmpty())
								{
									// grow into dirt with random chance
									if (BlockRandomEvent(new Vector3Int(pos.x, pos.y, pos.z), 0.04f))
									{
										world.SetVoxel(LocalToWorld(pos, ChunkPos, Size3D), new Voxel("silt"));
										if (voxel.Damage <= 0) activeVoxels.Remove(pos);
										SetDirty();
									}
								}
								else
								{
									if (voxel.Damage <= 0) activeVoxels.Remove(pos);
								}
							}
							break;
						default:
							if (voxel.Damage <= 0) activeVoxels.Remove(pos);
							break;
					}
					if (voxel.Damage > 0 && !PlayerView.usingTool)
					{
						if (BlockRandomEvent(new Vector3Int(pos.x, pos.y, pos.z), 0.1f))
						{
							voxel.Damage -= 1;
							world.SetVoxel(LocalToWorld(new Vector3Int(pos.x, pos.y, pos.z), ChunkPos, Size3D), voxel);
							if (voxel.Damage <= 0) activeVoxels.Remove(pos);
							SetDirty();
						}
					}
				}
				else
				{
					activeVoxels.Remove(pos);
				}
			}
		}
	}

	private bool BlockRandomEvent(Vector3Int pos, float probability)
	{
		int seed = (pos.x + Size3D.x * pos.y + Size3D.x * Size3D.y * pos.z) + (1000 * ChunkPos.x + 10000 * ChunkPos.y) + (Time.frameCount % 10000);
		Random.InitState(seed);
		return Random.Range(0f, 1f) < probability;
	}
	#endregion


	#region MESHING
	public void SetDirty()
	{
		meshDirty = true;
	}
	private void Remesh()
	{
		switch (meshingMethod)
		{
			case MeshingMethod.CPU:
				RemeshCPU(); break;
			case MeshingMethod.CPUPlusGeoShader:
				RemeshShader(); break;
		}
	}


	private void RemeshCPU()
	{
		meshMarker.Begin();
		double startTime = Time.realtimeSinceStartupAsDouble;

		vertices = new List<Vector3>();
		normals = new List<Vector3>();
		uvs = new List<Vector4>();
		triangles = new List<int>();
		colors = new List<Color>();
		foliageGrassCoords = new List<Vector3>();
		t = 0;
		neighborCache = new Dictionary<Vector3Int, Voxel>();

		Loop3D(ComputeMeshLoop);

		// send all data for chunk into mesh
		mesh = new Mesh
		{
			vertices = vertices.ToArray(),
			triangles = triangles.ToArray(),
			normals = normals.ToArray(),
			colors = colors.ToArray()
		};
		mesh.SetUVs(0, uvs.ToArray());
		mesh.RecalculateTangents();
		meshFilter.mesh = mesh;
		meshCollider.sharedMesh = mesh;

		vertices.Clear();
		normals.Clear();
		uvs.Clear();
		triangles.Clear();
		colors.Clear();
		foliage.SetFoliage(foliageGrassCoords);
		foliageGrassCoords.Clear();
		neighborCache.Clear();

		meshMarker.End();
		float duration = Mathf.Round((float)(Time.realtimeSinceStartupAsDouble - startTime) * 10000f) / 100f;
		DebugOverlay.SetDebugValue("Mesh Time", $"{duration}ms");
	}

	private void ComputeMeshLoop(int x, int y, int z)
	{
		Voxel vox = Voxels[x, y, z];
		if (vox.IsEmpty()) return;

		// if (vox.BlockID == BlockID.Foliage_Grass)
		// {
		// 	foliageGrassCoords.Add(new Vector3(x, y, z));
		// 	return;
		// }
		//voxelMarker.Begin();

		Vector3Int localPos = new Vector3Int(x, y, z);
		Vector3Int worldPos = LocalToWorld(localPos, ChunkPos, Size3D);

		Voxel[] neighborVoxels = new Voxel[6];
		for (int n = 0; n < 6; n++)
		{
			//Vector3Int dir = OrthoDirections[n].AlignYZ(vox.UpAxis, vox.ForwardAxis).ToVector();
			Vector3Int dir = OrthoNormal.AlignYZ(OrthoDirections[n], vox.UpAxis, vox.ForwardAxis).ToVector();
			//Voxel neighbor = world.LookupVoxelWorld(LocalToWorld(new Vector3Int(x, y, z) + dir, ChunkCoord, Size3D));
			Voxel neighbor = GetCachedNeighbor(worldPos + dir);
			neighborVoxels[n] = neighbor;
		}
		BlockModel model = new BlockModel(localPos, t, vox, neighborVoxels, ref tempVertices, ref tempNormals, ref tempUvs, ref tempColors, ref tempTriangles);
		foreach (Vector3 v in model.vertices) vertices.Add(v);
		foreach (Vector3 n in model.normals) normals.Add(n);
		foreach (Vector4 uv in model.uvs) uvs.Add(uv);
		foreach (Color c in model.colors) colors.Add(c);
		foreach (int tri in model.triangles) triangles.Add(tri);


		t = model.lastT;
		//voxelMarker.End();
	}

	private Voxel GetCachedNeighbor(Vector3Int worldPos)
	{
		if (neighborCache.ContainsKey(worldPos)) return neighborCache[worldPos];
		else
		{
			Voxel neighbor = world.LookupVoxelWorld(worldPos);
			neighborCache.Add(worldPos, neighbor);
			return neighbor;
		}
	}


	struct SourceVoxel
	{
		public Vector4 positionOS;
		public uint texIndex0; public uint texIndex1; public uint texIndex2; public uint texIndex3; public uint texIndex4; public uint texIndex5;
		public int feature0Index0; public int feature0Index1; public int feature0Index2; public int feature0Index3; public int feature0Index4; public int feature0Index5;
		public uint solidFaces;
		public uint damage;
		public uint solidNeighbors;


	}
	private GraphicsBuffer sourceVoxelBuffer;
	private MaterialPropertyBlock mpb;
	private void RemeshShader()
	{
		meshMarker.Begin();
		double startTime = Time.realtimeSinceStartupAsDouble;

		Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };

		List<Vector3> positions = new List<Vector3>();
		List<Color> colors = new List<Color>();
		List<Vector4> uvs0 = new List<Vector4>(); // rgba=texture indices 0-3
		List<Vector4> uvs1 = new List<Vector4>(); // rg=texture indices 4-5
		List<Vector4> uvs2 = new List<Vector4>(); // r=solid faces, g=damage, ba=solid neighbors

		List<SourceVoxel> sourceVoxels = new List<SourceVoxel>();
		SourceVoxel[] sourceVoxelArray = new SourceVoxel[Size3D.x * Size3D.y * Size3D.z];
		foliageGrassCoords = new List<Vector3>();
		neighborCache = new Dictionary<Vector3Int, Voxel>();

		int i = 0;

		for (int x = 0; x < Size3D.x; x++)
		{
			for (int y = 0; y < Size3D.y; y++)
			{
				for (int z = 0; z < Size3D.z; z++)
				{

					Voxel vox = Voxels[x, y, z];
					if (vox == null) continue;
					// if (vox.BlockID == BlockID.Foliage_Grass || vox.Shape == BlockShape.Foliage)
					// {
					// 	foliageGrassCoords.Add(new Vector3(x, y, z));
					// }
					if (!vox.IsEmpty())// && vox.BlockID != BlockID.Foliage_Grass && vox.Shape != BlockShape.Empty && vox.Shape != BlockShape.Foliage)
					{
						Vector3Int localPos = new Vector3Int(x, y, z);
						Vector3Int worldPos = LocalToWorld(localPos, ChunkPos, Size3D);
						bool shouldDraw = false;
						uint solidFaces = 0;
						for (int n = 0; n < 6; n++)
						{
							Vector3Int dir = OrthoNormal.AlignYZ(OrthoDirections[n], vox.UpAxis, vox.ForwardAxis).ToVector();
							Voxel neighbor = GetCachedNeighbor(worldPos + dir);
							if (neighbor == null || neighbor.IsEmpty())
							{
								shouldDraw = true;
								solidFaces |= (uint)1 << n;
							}
						}

						if (shouldDraw)
						{

							uint solidNeighbors1 = 0;
							uint solidNeighbors2 = 0;
							uint solidNeighbors0 = 0;
							for (int dx = 0; dx <= 2; dx++)
							{
								for (int dy = 0; dy <= 2; dy++)
								{
									for (int dz = 0; dz <= 2; dz++)
									{
										Vector3Int neighborLocalPos = new Vector3Int(x + dx - 1, y + dy - 1, z + dz - 1);
										Vector3Int neighborWorldPos = LocalToWorld(neighborLocalPos, ChunkPos, Size3D);
										Voxel neighbor = GetCachedNeighbor(neighborWorldPos);
										if (neighbor != null && !neighbor.IsEmpty())
										{
											int index = dx + dy * 3 + dz * 9;
											solidNeighbors0 |= (uint)1 << index;
											if (index < 14)
												solidNeighbors1 |= (uint)1 << index;
											else
												solidNeighbors2 |= (uint)1 << (index - 14);
										}
									}
								}
							}

							positions.Add(new Vector3(x, y, z));
							int[] indices = new int[6] { 1, 1, 1, 1, 1, 1 };
							BlockType type = Registry.GetBlockType(vox);
							if (type != null)
							{
								BlockTextureGroup tg = type.TextureGroup;
								if (tg != null)
								{
									int r = vox.Random;
									indices = tg.GetRandomIndices(r);
								}
							}
							//BlockRegistry.LookupBlockTextures(vox.BlockID);
							int[] grassIndices = new int[6] { -1, -1, -1, -1, -1, -1 };
							// if (type.TextureFeatures.Count > 0 && vox.Grassy)
							// {
							// 	grassIndices = type.TextureFeatures[0].GetIndices();
							// }

							SourceVoxel sourceVoxel = new SourceVoxel
							{
								positionOS = new Vector4(x, y, z, 0),
								texIndex0 = (uint)indices[0],
								texIndex1 = (uint)indices[1],
								texIndex2 = (uint)indices[2],
								texIndex3 = (uint)indices[3],
								texIndex4 = (uint)indices[4],
								texIndex5 = (uint)indices[5],
								feature0Index0 = grassIndices[0],
								feature0Index1 = grassIndices[1],
								feature0Index2 = grassIndices[2],
								feature0Index3 = grassIndices[3],
								feature0Index4 = grassIndices[4],
								feature0Index5 = grassIndices[5],
								solidFaces = (uint)solidFaces,
								damage = (uint)vox.Damage,
								solidNeighbors = solidNeighbors0
							};

							sourceVoxels.Add(sourceVoxel);
							sourceVoxelArray[i] = sourceVoxel;

							i++;
						}
					}
				}
			}
		}

		if (sourceVoxels.Count == 0)
		{
			mesh.Clear();
			meshFilter.mesh = mesh;
			meshCollider.sharedMesh = mesh;
			foliage.SetFoliage(foliageGrassCoords);
			foliageGrassCoords.Clear();
			neighborCache.Clear();
			meshMarker.End();
			return;
		}
		sourceVoxelBuffer?.Release();
		sourceVoxelBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, sourceVoxels.Count, sizeof(float) * 4 + sizeof(uint) * 15);
		sourceVoxelBuffer.SetData(sourceVoxels);

		mpb ??= new MaterialPropertyBlock();
		meshRenderer.GetPropertyBlock(mpb);
		mpb.SetBuffer("_VoxelBuffer", sourceVoxelBuffer);
		meshRenderer.SetPropertyBlock(mpb);
		//meshRenderer.sharedMaterial.SetBuffer("_VoxelBuffer", sourceVoxelBuffer);

		mesh.SetVertices(positions);
		mesh.SetUVs(0, uvs0);
		mesh.SetUVs(1, uvs1);
		mesh.SetUVs(2, uvs2);
		mesh.SetColors(colors);
		mesh.SetIndices(Enumerable.Range(0, positions.Count).ToArray(), MeshTopology.Points, 0);
		meshFilter.mesh = mesh;

		foliage.SetFoliage(foliageGrassCoords);
		foliageGrassCoords.Clear();
		neighborCache.Clear();

		meshMarker.End();
		float duration = Mathf.Round((float)(Time.realtimeSinceStartupAsDouble - startTime) * 10000f) / 100f;
		DebugOverlay.SetDebugValue("Mesh Time", $"{duration}ms");

	}

	private bool GetVoxelFaceVisible(Vector3Int pos, Vector3Int faceDirection)
	{
		Voxel thisVoxel = world.LookupVoxelWorld(LocalToWorld(pos, ChunkPos, Size3D));
		Voxel neighborVoxel = world.LookupVoxelWorld(LocalToWorld(pos + faceDirection, ChunkPos, Size3D));
		return neighborVoxel.IsEmpty() && !thisVoxel.IsEmpty();
	}
	#endregion


	#region ACCESS VOXELS
	public Voxel GetVoxel(Vector3Int worldPos)
	{
		Vector3Int localPos = WorldToLocal(worldPos, ChunkPos, Size3D);
		Voxel result;
		try
		{
			result = GetVoxelLocal(localPos);
		}
		catch (IndexOutOfRangeException)
		{
			result = LookupWorldVoxel(localPos);
		}
		return result;
		//return Voxels[localPos.x, localPos.y, localPos.z];
	}
	public Voxel GetVoxelLocal(Vector3Int localPos)
	{
		return Voxels[localPos.x, localPos.y, localPos.z];
	}

	public Voxel LookupWorldVoxel(Vector3Int localPos)
	{
		return world.LookupVoxelWorld(LocalToWorldPos(localPos));
	}

	public Vector3Int LocalToWorldPos(Vector3Int localPos)
	{
		Vector3Int worldPos = new Vector3Int(localPos.x + ChunkPos.x * Size3D.x, localPos.y, localPos.z + ChunkPos.y * Size3D.z);
		return worldPos;
	}
	public Vector3Int WorldToLocalPos(Vector3Int worldPos)
	{
		Vector3Int localPos = worldPos - new Vector3Int(ChunkPos.x * Size3D.x, 0, ChunkPos.y * Size3D.z);
		return localPos;
	}
	#endregion


	#region MODIFY VOXELS

	public void SetVoxel(Vector3Int worldPos, Voxel voxelData)
	{
		Vector3Int localPos = WorldToLocal(worldPos, ChunkPos, Size3D);
		Voxel currentVoxel = GetVoxel(localPos);
		if (IsPosInGridBounds(localPos, Size3D))
		{
			if (currentVoxel.IsEmpty() || true)
			{
				Voxels[localPos.x, localPos.y, localPos.z] = voxelData;

				meshDirty = true;

				SetNeighborsDirty(localPos);

			}
		}
		activeVoxels.Add(localPos);
		activeVoxels.Add(localPos + Vector3Int.left);
		activeVoxels.Add(localPos + Vector3Int.right);
		activeVoxels.Add(localPos + Vector3Int.down);
		activeVoxels.Add(localPos + Vector3Int.up);
		activeVoxels.Add(localPos + Vector3Int.back);
		activeVoxels.Add(localPos + Vector3Int.forward);
	}
	public void AddBlockEntity(BlockEntityActor entity, Vector3Int worldPos, Voxel voxel)
	{
		if (!BlockEntities.ContainsKey(worldPos))
		{
			entity.VoxelData = voxel;
			entity.VoxelPosition = worldPos;
			entity.SetPosition();
			entity.LoadEntity();
			BlockEntities.Add(worldPos, entity);
		}
	}

	private const int DAMAGE_THRESH = 12;
	public void DamageVoxel(Vector3Int worldPos, VoxelHitInfo hitInfo, byte damage)
	{
		Voxel voxel = GetVoxel(worldPos);
		BlockType blockType = Registry.GetBlockType(voxel);

		VFX().SpawnVFX(VFXType.BLOCK_DMG, hitInfo.hitPos, hitInfo.hitNormal, 1);

		voxel.Damage += damage;
		int toughness = blockType.Toughness;
		if (voxel.Damage >= toughness)
		{
			// if (data.IsBlockEntity)
			// {
			// 	if (BlockEntities.ContainsKey(worldPos))
			// 	{
			// 		Destroy(BlockEntities[worldPos].gameObject);
			// 		BlockEntities.Remove(worldPos);
			// 	}
			// }
			VFX().SpawnVFX(VFXType.BLOCK_BREAK, worldPos, Vector3.zero, 0);

			voxel = new Voxel("air");
			voxel.Shape = 0;
		}
		world.SetVoxel(worldPos, voxel); // set via world
										 //SetVoxel(worldPos, voxel); // set directly
	}

	#endregion

}





