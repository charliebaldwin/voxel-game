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
using Unity.Mathematics;
using Unity.Profiling;
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
	private Voxel[,,] Voxels = new Voxel[1, 1, 1];
	private List<Vector3Int> activeVoxels = new List<Vector3Int>();
	private Dictionary<Vector3Int, BlockEntityActor> BlockEntities;


	// status
	bool IInitializable.Initialized { get { return initialized; } }
	private bool initialized = false;
	public bool Loaded = false;
	private bool meshDirty = true;

	// references
	private static VoxelWorld world;
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

		UnloadChunk();

		initialized = true;

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
		activeVoxels = new List<Vector3Int>();
		Loop3D(FillActiveVoxelsLoop);
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
		BlockID voxelID = voxel.BlockID;
		switch (voxelID)
		{
			case (BlockID.Grass):
				if (y < Size3D.y - 1)
				{
					if (Voxels[x, y + 1, z].BlockID != BlockID.Air)
					{
						world.SetVoxel(LocalToWorld(pos, ChunkPos, Size3D), new Voxel(BlockID.Dirt, voxel.Damage, voxel.Shape, voxel.UpAxis, voxel.ForwardAxis));
						meshDirty = true;
					}
				}
				break;
			case (BlockID.Dirt):
				if (y < Size3D.y - 1)
				{
					//Voxel upVoxel = World().LookupVoxel(LocalToWorldPos((pos + new Vector3Int(0, 1, 0))));
					Voxel upVoxel = GetVoxelLocal(pos + Vector3Int.up);
					if (upVoxel.BlockID == BlockID.Air)
					{
						// grow into dirt with random chance
						if (BlockRandomEvent(new int3(x, y, z), 0.04f))
						{
							world.SetVoxel(LocalToWorld(pos, ChunkPos, Size3D), new Voxel(BlockID.Grass, voxel.Damage, voxel.Shape, voxel.UpAxis, voxel.ForwardAxis));

							SetDirty();
						}
					}
				}
				break;
		}

		if (Voxels[x, y, z].Damage > 0 && !PlayerView.usingTool)
		{
			if (BlockRandomEvent(new int3(x, y, z), 0.05f))
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
		BlockID id = Voxels[x, y, z].BlockID;
		if (id != BlockID.Air)
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
					BlockID voxelID = voxel.BlockID;
					switch (voxelID)
					{
						case (BlockID.Air):
							activeVoxels.Remove(pos);
							break;
						case (BlockID.Grass):
							if (pos.y < Size3D.y - 1)
							{
								if (Voxels[pos.x, pos.y + 1, pos.z].BlockID != BlockID.Air)
								{
									world.SetVoxel(LocalToWorld(pos, ChunkPos, Size3D), new Voxel(BlockID.Dirt, voxel.Damage, voxel.Shape, voxel.UpAxis, voxel.ForwardAxis));
									meshDirty = true;
								}

							}
							if (voxel.Damage <= 0) activeVoxels.Remove(pos);
							break;
						case (BlockID.Dirt):
							if (pos.y < Size3D.y - 1)
							{
								//Voxel upVoxel = World().LookupVoxel(LocalToWorldPos((pos + new Vector3Int(0, 1, 0))));
								Voxel upVoxel = GetVoxelLocal(pos + Vector3Int.up);
								if (upVoxel.BlockID == BlockID.Air)
								{
									// grow into dirt with random chance
									if (BlockRandomEvent(new int3(pos.x, pos.y, pos.z), 0.04f))
									{
										world.SetVoxel(LocalToWorld(pos, ChunkPos, Size3D), new Voxel(BlockID.Grass, voxel.Damage, voxel.Shape, voxel.UpAxis, voxel.ForwardAxis));
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
						if (BlockRandomEvent(new int3(pos.x, pos.y, pos.z), 0.1f))
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

	private bool BlockRandomEvent(int3 pos, float probability)
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
		if (vox.BlockID == BlockID.Air || vox.BlockID == BlockID.Machine) return;

		if (vox.BlockID == BlockID.Foliage_Grass)
		{
			foliageGrassCoords.Add(new Vector3(x, y, z));
			return;
		}
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

	private void RemeshShader()
	{
		meshMarker.Begin();
		double startTime = Time.realtimeSinceStartupAsDouble;

		Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };

		List<Vector3> positions = new List<Vector3>();
		List<Color> colors = new List<Color>();
		List<Vector4> uvs0 = new List<Vector4>(); // rgba=texture indices 0-3
		List<Vector4> uvs1 = new List<Vector4>(); // rg=texture indices 4-5
		List<Vector4> uvs2 = new List<Vector4>(); // r=solid faces, g=damage, b=solid neighbors
		foliageGrassCoords = new List<Vector3>();
		neighborCache = new Dictionary<Vector3Int, Voxel>();


		for (int z = 0; z < Size3D.z; z++)
		{
			for (int y = 0; y < Size3D.y; y++)
			{
				for (int x = 0; x < Size3D.x; x++)
				{

					Voxel vox = Voxels[x, y, z];
					if (vox.BlockID == BlockID.Foliage_Grass || vox.Shape == BlockShape.Foliage)
					{
						foliageGrassCoords.Add(new Vector3(x, y, z));
					}
					else if (vox.BlockID != BlockID.Air && vox.BlockID != BlockID.Foliage_Grass && vox.Shape != BlockShape.Empty && vox.Shape != BlockShape.Foliage)
					{
						Vector3Int localPos = new Vector3Int(x, y, z);
						Vector3Int worldPos = LocalToWorld(localPos, ChunkPos, Size3D);
						bool shouldDraw = false;
						uint solidFaces = 0;
						for (int n = 0; n < 6; n++)
						{
							Vector3Int dir = OrthoNormal.AlignYZ(OrthoDirections[n], vox.UpAxis, vox.ForwardAxis).ToVector();
							Voxel neighbor = GetCachedNeighbor(worldPos + dir);
							if (neighbor.Shape == BlockShape.Foliage || neighbor.Shape == BlockShape.Empty)
							{
								shouldDraw = true;
								solidFaces |= (uint)1 << n;
							}
						}

						uint solidNeighbors = 0;
						for (int dx = 0; dx <= 2; dx++)
						{
							for (int dy = 0; dy <= 2; dy++)
							{
								for (int dz = 0; dz <= 2; dz++)
								{
									Vector3Int neighborLocalPos = new Vector3Int(x + dx - 1, y + dy - 1, z + dz - 1);
									Vector3Int neighborWorldPos = LocalToWorld(neighborLocalPos, ChunkPos, Size3D);
									Voxel neighbor = GetCachedNeighbor(neighborWorldPos);
									if (neighbor.Shape != BlockShape.Empty || neighbor.BlockID != BlockID.Air)
									{
										solidNeighbors |= (uint)1 << (dx + dy * 3 + dz * 9);
									}
								}
							}
						}
						if (shouldDraw)
						{
							positions.Add(new Vector3(x, y, z));
							List<int> indices = BlockRegistry.LookupBlockTextures(vox.BlockID);

							Vector4 uv0 = Vector4.zero;
							if (indices.Count > 0) uv0.x = indices[0];
							if (indices.Count > 1) uv0.y = indices[1];
							if (indices.Count > 2) uv0.z = indices[2];
							if (indices.Count > 3) uv0.w = indices[3];

							Vector4 uv1 = Vector4.zero;
							if (indices.Count > 4) uv1.x = indices[4];
							if (indices.Count > 5) uv1.y = indices[5];

							Vector4 uv2 = Vector4.zero;
							uv2.x = solidFaces;
							uv2.y = vox.Damage;
							uv2.z = solidNeighbors;

							uvs0.Add(uv0);
							uvs1.Add(uv1);
							uvs2.Add(uv2);
						}
					}
				}
			}
		}

		mesh.SetVertices(positions);
		mesh.SetUVs(0, uvs0);
		mesh.SetUVs(1, uvs1);
		mesh.SetUVs(2, uvs2);
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
		return neighborVoxel.BlockID == 0 && thisVoxel.BlockID > 0;
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
			if (currentVoxel.BlockID == 0 || true)
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
		BlockData data = BlockRegistry.LookupBlock(voxel.BlockID);

		VFX().SpawnVFX(VFXType.BLOCK_DMG, hitInfo.hitPos, hitInfo.hitNormal, (int)voxel.BlockID);

		voxel.Damage += damage;
		int toughness = data.Toughness;
		if (voxel.Damage >= toughness)
		{
			if (data.IsBlockEntity)
			{
				if (BlockEntities.ContainsKey(worldPos))
				{
					Destroy(BlockEntities[worldPos].gameObject);
					BlockEntities.Remove(worldPos);
				}
			}
			VFX().SpawnVFX(VFXType.BLOCK_BREAK, worldPos, Vector3.zero, (int)voxel.BlockID);

			voxel = new Voxel(BlockID.Air, 0, 0);
			voxel.Shape = 0;
		}
		world.SetVoxel(worldPos, voxel); // set via world
										 //SetVoxel(worldPos, voxel); // set directly
	}

	#endregion

}





