using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class TerrainGenerator : MonoBehaviour
{
	public IGenerationMethod[] generationMethods;

	[SerializeField]
	public GenerationSettings[] generationSettings;

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

	//private members
	private Mesh mesh;

	private MeshFilter meshFilter;
	private MeshCollider meshCollider;

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
		else if (chunkSize > 241)
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
		var temp = Time.realtimeSinceStartup;

		UpdateNoiseArray(generationSettings);
		meshFilter = GetComponent<MeshFilter>();
		meshCollider = GetComponent<MeshCollider>();

		float[,] map = new float[chunkSize, chunkSize];

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int i = 0; i < generationMethods.Length; ++i)
		{
			float[,] tempMap = generationMethods[i].CreateHeightMap();

			for (int z = 0; z < map.GetLength(0); ++z)
			{ 
				for (int x = 0; x < map.GetLength(1); ++x)
				{
					map[z, x] += tempMap[z, x];
					if (map[z, x] > maxValue)
						maxValue = map[z, x];
					else if (map[z, x] < minValue)
						minValue = map[z, x];
				}
			}
		}
		for (int z = 0; z < map.GetLength(0); ++z)
		{
			for (int x = 0; x < map.GetLength(1); ++x)
			{
				map[z, x] *= mapHeightMultiplier;
			}
		}
		CreateMesh(map);


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
	}

	public void UpdateNoiseArray(GenerationSettings[] generationSettings)
	{
		generationMethods = new IGenerationMethod[generationSettings.Length];

		for (int i = 0; i < generationSettings.Length; ++i)
		{
			IGenerationMethod generationMethod = null;

			switch (generationSettings[i].methodType)
			{
				case GenerationSettings.GenerationMethodType.SpatialSubdivision:
					generationMethod = new SpatialSubdivision(generationSettings[i], seed);
					break;

				case GenerationSettings.GenerationMethodType.PerlinNoise:
					generationMethod = new PerlinNoise(generationSettings[i], seed);
					break;

				case GenerationSettings.GenerationMethodType.Voronoi:
					generationMethod = new VoronoiDiagrams(generationSettings[i], seed);
					break;
			}

			generationMethods[i] = generationMethod;
		}
	}
}