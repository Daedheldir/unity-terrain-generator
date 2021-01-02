using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class TerrainGenerator : MonoBehaviour
{
	public enum GenerationMethodType
	{
		SpatialSubdivision,
		PerlinVoronoiHybrid
	}

	public GenerationMethodType methodType = GenerationMethodType.SpatialSubdivision;

	public bool autoUpdateMap = false;
	public int seed = 0;
	public float mapCellSize = 10;
	public float mapHeightMultiplier = 10;

	//LOD
	public int chunkSize = 240;

	[Range(0, 6)]
	public int LOD;

	//perlin data
	[Range(1, 10)]
	public int octaves = 3;

	[Range(0, 1)]
	public float persistance = 0.5f;

	public float mapSmoothness = 0.1f;
	public float noiseScale = 1f;
	public float perlinWeight = 1f;

	//voronoi Data
	public int voronoiPoints = 50;

	public float voronoiMinHeight = 0.1f;

	public float voronoiScale = 1f;
	public float voronoiWeight = 1f;

	[Range(0f, 1f)]
	public float voronoiMaskScale = 0.1f;

	public float voronoiMaskWeight = 1f;
	public int voronoiOctaves = 3;
	public float voronoiPersistance = 0.5f;

	[Range(0.1f, 2f)]
	public float voronoiSmoothing = 0.5f;

	//terrain tiles data
	public bool enableTiles;

	public GameObject terrainTilePrefab;

	//private members
	private Mesh mesh;

	private MeshFilter meshFilter;
	private MeshCollider meshCollider;

	private List<GameObject> terrainTiles = new List<GameObject>();

	private GenerationMethod generationMethod;

	// Start is called before the first frame update
	private void Start()
	{
		meshFilter = GetComponent<MeshFilter>();
		meshCollider = GetComponent<MeshCollider>();

		GenerateMap();
	}

	private void OnValidate()
	{
		if (chunkSize < 10)
			chunkSize = 10;
		else if (chunkSize > 240)
			chunkSize = 240;

		if (mapCellSize <= 0)
			mapCellSize = 0.0001f;

		if (octaves < 1)
			octaves = 1;

		if (mapHeightMultiplier <= 0)
			mapHeightMultiplier = 0.0001f;

		if (mapSmoothness < 1)
			mapSmoothness = 1f;

		if (noiseScale <= 0)
			noiseScale = 0.0001f;

		if (voronoiOctaves > 30f / voronoiPoints)
			voronoiOctaves = Mathf.Min((int)((30f / voronoiPoints) > 1 ? (30f / voronoiPoints) : 1), 4);
		if (voronoiOctaves <= 0)
			voronoiOctaves = 1;

		if (voronoiSmoothing <= 0f)
			voronoiSmoothing = 0.1f;
		if (voronoiSmoothing > 2f)
			voronoiSmoothing = 2f;

		if (voronoiPersistance <= -3f)
			voronoiPersistance = -3f;
		if (voronoiPersistance > 3f)
			voronoiPersistance = 3f;
	}

	public void GenerateMap()
	{
		meshFilter = GetComponent<MeshFilter>();
		meshCollider = GetComponent<MeshCollider>();

		//choosing terrain generation method
		switch (methodType)
		{
			case GenerationMethodType.SpatialSubdivision:
			{
				generationMethod = new SpatialSubdivision(chunkSize, seed, mapSmoothness);
				break;
			}
			case GenerationMethodType.PerlinVoronoiHybrid:
			{
				generationMethod = new PerlinVoronoiHybrid(chunkSize, seed,
					noiseScale, perlinWeight, octaves, persistance, mapSmoothness,
					voronoiPoints, voronoiMinHeight, voronoiScale, voronoiWeight, voronoiMaskScale, voronoiMaskWeight, voronoiOctaves, voronoiPersistance, voronoiSmoothing);
				break;
			}
		}
		var temp = Time.realtimeSinceStartup;

		//assigning components
		if (!enableTiles)
		{
			CreateMesh(generationMethod.CreateHeightMap());
		}
		else
		{
			CreateTiles(generationMethod.CreateHeightMap());
		}
		Debug.Log("Terrain generation execution time = " + (Time.realtimeSinceStartup - temp).ToString());
	}

	public void CreateMesh(float[,] heightMap)
	{
		ClearMap();

		MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, mapHeightMultiplier, LOD);
		mesh = meshData.CreateMesh();

		meshFilter.sharedMesh = mesh;
		meshCollider.sharedMesh = mesh;
	}

	public void ClearMap()
	{
		if (mesh != null)
			mesh.Clear();

		//clear the tile map
		if (terrainTiles != null)
		{
			foreach (GameObject tile in terrainTiles)
			{
				GameObject.DestroyImmediate(tile);
			}
			terrainTiles.Clear();
		}
	}

	public void CreateTiles(float[,] heightMap)
	{
		ClearMap();

		for (int z = 0; z <= chunkSize; z++)
		{
			for (int x = 0; x <= chunkSize; x++)
			{
				Vector3 position = new Vector3(x * 5 * mapCellSize, (heightMap[x, z] * mapHeightMultiplier) * 5, z * 5 * mapCellSize);
				string tileName = "Tile[" + x + "," + z + "]";

				terrainTiles.Add(GameObject.Instantiate(terrainTilePrefab, position, Quaternion.Euler(0, 0, 0), this.transform));
				terrainTiles[z * (chunkSize + 1) + x].name = tileName;
				terrainTiles[z * (chunkSize + 1) + x].transform.localScale = new Vector3(mapCellSize, heightMap[x, z] * mapHeightMultiplier, mapCellSize);
			}
		}
	}
}