using System;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrainGenerator : MonoBehaviour
{
	public static float maxViewDist = 4000;
	public const float debugViewDist = 1000;
	public Transform viewer;

	public static Vector3 viewerPosition;
	private int chunkSize;
	private int visibleChunks;

	private Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();

	private static TerrainGenerator terrainGenerator;

	public static TerrainGenerator TerrainGenerator
	{
		get
		{
			if (terrainGenerator == null)
				terrainGenerator = FindObjectOfType<TerrainGenerator>();
			return terrainGenerator;
		}
		set => terrainGenerator = value;
	}

	public class TerrainChunk
	{
		private GameObject meshObject;
		private MeshRenderer meshRenderer;
		private MeshFilter meshFilter;

		private GameObject waterPlane;
		private MeshRenderer waterRenderer;

		public Vector2 position;

		private Bounds bounds;

		private ChunkData chunkData;

		private LODInfo[] LODLevels;
		private MeshLOD[] meshLODs;
		private bool chunkDataReceived = false;
		private int prevLOD = -1;

		public TerrainChunk(Vector2 coord, int size, Transform parent, LODInfo[] LODLevels)
		{
			this.LODLevels = LODLevels;
			position = coord * size;

			//generate data for chunk
			TerrainGenerator.RequestChunkData(OnChunkDataReceived, position);

			Vector3 position3D = new Vector3(position.x, 0, position.y);

			//creating gameobject
			meshObject = new GameObject("Terrain Chunk [" + coord.x + ", " + coord.y + "]");

			meshLODs = new MeshLOD[LODLevels.Length];

			for (int i = 0; i < meshLODs.Length; ++i)
			{
				meshLODs[i] = new MeshLOD(LODLevels[i].lod);
			}

			//set mesh object static
			meshObject.isStatic = true;

			//positioning chunk
			meshObject.transform.parent = parent;
			meshObject.transform.localScale = Vector3.one;
			meshObject.transform.localPosition = position3D;

			//creating boundary for check
			bounds = new Bounds(position3D, new Vector3(size, 0, size));

			//adding components
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshRenderer = meshObject.AddComponent<MeshRenderer>();

			//set material
			meshRenderer.material = TerrainGenerator.terrainMaterial;
		}

		private void OnChunkDataReceived(ChunkData chunkData)
		{
			this.chunkData = chunkData;

			this.chunkDataReceived = true;

			bool waterVisible = false;

			for (int z = 0; z < chunkData.heightMap.GetLength(0); ++z)
			{
				for (int x = 0; x < chunkData.heightMap.GetLength(1); ++x)
				{
					//if any point of land is lower than sea level
					if ((chunkData.heightMap[x, z] * TerrainGenerator.mapHeightMultiplier) + meshObject.transform.localPosition.y <= TerrainGenerator.seaLevel)
					{
						waterVisible = true;
						break;
					}
				}
			}

			//if isnt visible then return
			if (!waterVisible)
			{
				waterPlane = null;
				return;
			}

			//else create waterPlane

			//creating waterPlane
			waterPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);

			//set water as static
			waterPlane.isStatic = true;

			waterRenderer = waterPlane.GetComponent<MeshRenderer>();
			waterRenderer.material = TerrainGenerator.seaMaterial;
			waterRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

			waterPlane.transform.parent = meshObject.transform;
			waterPlane.transform.localPosition = new Vector3(0, TerrainGenerator.seaLevel, 0);
			waterPlane.transform.localRotation = Quaternion.Euler(90, 0, 0);
			waterPlane.transform.localScale = new Vector3(chunkData.heightMap.GetLength(1) - 1, chunkData.heightMap.GetLength(0) - 1, 1f);
		}

		public void UpdateTerrainChunk()
		{
			if (!chunkDataReceived)
				return;

			//if chunk isn't visible
			if (!IsVisible())
			{
				if (ShouldBeVisible())

					//if it should be visible set it to it
					SetVisible(true);
			}
			else
			{
				//else if terrain chunk is visible check if it should be hidden
				if (!ShouldBeVisible())

					//if it shouldnt be visible set it to it
					SetVisible(false);
			}

			if (IsVisible())
			{
				int lodIndex = 0;
				float viewerDstToEdge = bounds.SqrDistance(viewerPosition);

				for (int i = 0; i < LODLevels.Length - 1; ++i)
				{
					if (viewerDstToEdge > LODLevels[i].distanceThreshold * LODLevels[i].distanceThreshold)
					{
						lodIndex = i + 1;
					}
					else
					{
						break;
					}
				}

				if (lodIndex != prevLOD)
				{
					MeshLOD meshLOD = meshLODs[lodIndex];
					if (meshLOD.hasMesh)
					{
						prevLOD = lodIndex;
						meshFilter.mesh = meshLOD.mesh;
					}
					else if (!meshLOD.hasRequestedMesh)
					{
						meshLOD.RequestMesh(chunkData);
					}
				}
			}
		}

		public void SetVisible(bool visible)
		{
			//meshRenderer.forceRenderingOff = !visible;
			meshRenderer.enabled = visible;

			if (waterRenderer != null)

				//waterRenderer.forceRenderingOff = !visible;
				waterRenderer.enabled = visible;
		}

		public bool IsVisible()
		{
			return meshRenderer.enabled;
		}

		public bool ShouldBeVisible()
		{
			float viewerDstToEdge = bounds.SqrDistance(viewerPosition);

			//check if it should be visible
			bool isInViewRange;
			if (terrainGenerator.DEBUG)
				isInViewRange = viewerDstToEdge <= debugViewDist * debugViewDist;
			else
				isInViewRange = viewerDstToEdge <= maxViewDist * maxViewDist;

			return EndlessTerrainGenerator.IsVisibleToCamera(meshRenderer.transform.position) && isInViewRange;
		}
	}

	// Start is called before the first frame update
	private void Start()
	{
		TerrainGenerator = FindObjectOfType<TerrainGenerator>();
		chunkSize = TerrainGenerator.chunkSize - 1;

		maxViewDist = TerrainGenerator.LODLookupTable[TerrainGenerator.LODLookupTable.Length - 1].distanceThreshold;

		if (terrainGenerator.DEBUG)
			visibleChunks = Mathf.RoundToInt(debugViewDist / chunkSize);
		else
			visibleChunks = Mathf.RoundToInt(maxViewDist / chunkSize);
	}

	// Update is called once per frame
	private void Update()
	{
		viewerPosition = viewer.position;
		UpdateVisibleChunks();
	}

	private void UpdateVisibleChunks()
	{
		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
		int currentChunkCoordZ = Mathf.RoundToInt(viewerPosition.z / chunkSize);

		Vector3 distance = new Vector3();

		for (int yOffset = -visibleChunks; yOffset <= visibleChunks; ++yOffset)
		{
			for (int xOffset = -visibleChunks; xOffset <= visibleChunks; ++xOffset)
			{
				Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordZ + yOffset);

				if (terrainChunkDict.ContainsKey(viewedChunkCoord))
				{
					terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
				}
				else
				{
					distance.Set(viewedChunkCoord.x * chunkSize, 0, viewedChunkCoord.y * chunkSize);

					if (terrainGenerator.DEBUG)
					{
						if (Vector3.Distance(distance, viewerPosition) < debugViewDist)
							terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, this.transform, TerrainGenerator.LODLookupTable));
					}
					else
					{
						if (Vector3.Distance(distance, viewerPosition) < maxViewDist)
							terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, this.transform, TerrainGenerator.LODLookupTable));
					}
				}
			}
		}
	}

	public static bool IsVisibleToCamera(Vector3 position)
	{
		Vector3 visTest = TerrainGenerator.mainCamera.WorldToViewportPoint(position);

		if (visTest.z < -2)
		{
			return false;
		}
		else if (visTest.x < -2)
		{
			return false;
		}
		else if (visTest.x > 3)
		{
			return false;
		}
		else if (visTest.y < -2)
		{
			return false;
		}
		else if (visTest.y > 3)
		{
			return false;
		}

		return true;
	}

	private class MeshLOD
	{
		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		private int lod;

		public MeshLOD(int lod)
		{
			this.lod = lod;
		}

		private void OnMeshDataReceived(MeshData meshData)
		{
			mesh = meshData.CreateMesh();
			hasMesh = true;
		}

		public void RequestMesh(ChunkData chunkData)
		{
			hasRequestedMesh = true;
			terrainGenerator.RequestMeshData(OnMeshDataReceived, chunkData, lod);
		}
	}
}