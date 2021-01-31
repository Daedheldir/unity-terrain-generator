using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrainGenerator : MonoBehaviour
{
	public const float maxViewDist = 3000;

	public Transform viewer;

	public static Vector3 viewerPosition;
	private int chunkSize;
	private int visibleChunks;

	private Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
	private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

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

		private Vector2 position;

		private Bounds bounds;

		public TerrainChunk(Vector2 coord, int size, Transform parent)
		{
			position = coord * size;

			Vector3 position3D = new Vector3(position.x, 0, position.y);

			//creating gameobject
			meshObject = new GameObject("Terrain Chunk [" + coord.x + ", " + coord.y + "]");

			//creating waterPlane
			waterPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);

			waterPlane.GetComponent<MeshRenderer>().material = TerrainGenerator.seaMaterial;
			waterPlane.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

			waterPlane.transform.parent = meshObject.transform;
			waterPlane.transform.localPosition = new Vector3(0, TerrainGenerator.seaLevel, 0);
			waterPlane.transform.localRotation = Quaternion.Euler(90, 0, 0);
			waterPlane.transform.localScale = new Vector3(size, size, 1f);

			//positioning chunk
			meshObject.transform.parent = parent;
			meshObject.transform.localScale = Vector3.one;
			meshObject.transform.localPosition = position3D;

			//creating boundary for check
			bounds = new Bounds(position3D, new Vector3(size, 0, size));

			//adding components
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshRenderer = meshObject.AddComponent<MeshRenderer>();

			//generate data for chunk
			TerrainGenerator.RequestChunkData(OnChunkDataReceived, position);
		}

		private void OnChunkDataReceived(ChunkData chunkData)
		{
			TerrainGenerator.RequestMeshData(OnMeshDataReceived, chunkData);
		}

		private void OnMeshDataReceived(MeshData meshData)
		{
			//assing mesh data to mesh
			meshFilter.mesh = meshData.CreateMesh();

			//set material
			meshRenderer.material = TerrainGenerator.terrainMaterial;
		}

		public void UpdateTerrainChunk()
		{
			float viewerDstToEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

			bool isVisible = viewerDstToEdge <= maxViewDist;
			SetVisible(isVisible);
		}

		public void SetVisible(bool visible)
		{
			meshObject.SetActive(visible);
		}

		public bool IsVisible()
		{
			return meshObject.activeSelf;
		}
	}

	// Start is called before the first frame update
	private void Start()
	{
		TerrainGenerator = FindObjectOfType<TerrainGenerator>();
		chunkSize = TerrainGenerator.chunkSize - 1;
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

		//hide all chunks that were visible
		foreach (TerrainChunk terrainChunk in terrainChunksVisibleLastUpdate)
		{
			terrainChunk.SetVisible(false);
		}

		for (int yOffset = -visibleChunks; yOffset <= visibleChunks; ++yOffset)
		{
			for (int xOffset = -visibleChunks; xOffset <= visibleChunks; ++xOffset)
			{
				Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordZ + yOffset);

				if (terrainChunkDict.ContainsKey(viewedChunkCoord))
				{
					terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
					if (terrainChunkDict[viewedChunkCoord].IsVisible())
					{ terrainChunksVisibleLastUpdate.Add(terrainChunkDict[viewedChunkCoord]); }
				}
				else
				{
					terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, this.transform));
				}
			}
		}
	}
}