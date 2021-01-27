using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrainGenerator : MonoBehaviour
{
	public const float maxViewDist = 500;
	public Transform viewer;

	public static Vector2 viewerPosition;
	private int chunkSize;
	private int visibleChunks;

	private Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
	private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	public class TerrainChunk
	{
		TerrainGenerator terrainGenerator;
		private GameObject meshObject;
		private MeshData meshData;
		private Vector2 position;

		private Bounds bounds;

		public TerrainChunk(Vector2 coord, int size, Transform parent)
		{
			terrainGenerator = FindObjectOfType<TerrainGenerator>();
			position = coord * size;
			Vector3 position3D = new Vector3(position.x, 0, position.y);

			terrainGenerator.generationOffset = position;


			meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
			meshObject.transform.parent = parent;
			meshObject.transform.position = position3D;
			meshObject.transform.localScale = Vector3.one * size / 10f;

			terrainGenerator.MeshFilter = meshObject.GetComponent<MeshFilter>();
			terrainGenerator.Mesh = meshObject.GetComponent<MeshFilter>().mesh;

			terrainGenerator.GenerateMapData();
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
		chunkSize = 241 - 1;
		visibleChunks = Mathf.RoundToInt(maxViewDist / chunkSize);
	}

	// Update is called once per frame
	private void Update()
	{
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
		UpdateVisibleChunks();
	}

	private void UpdateVisibleChunks()
	{
		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

		foreach (TerrainChunk terrainChunk in terrainChunksVisibleLastUpdate)
		{
			terrainChunk.SetVisible(false);
		}

		for (int yOffset = -visibleChunks; yOffset <= visibleChunks; ++yOffset)
		{
			for (int xOffset = -visibleChunks; xOffset <= visibleChunks; ++xOffset)
			{
				Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

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