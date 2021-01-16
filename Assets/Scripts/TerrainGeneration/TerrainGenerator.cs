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

	public Vector2 generationOffset = new Vector2(0, 0);

	[Range(0, 6)]
	public int LOD;

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

		if (mapHeightMultiplier <= 0)
			mapHeightMultiplier = 0.0001f;
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
			//if method isnt active skip it
			if (!generationSettings[i].isActive)
				continue;

			float[,] tempMap = generationMethods[i].CreateHeightMap();

			for (int z = 0; z < map.GetLength(0); ++z)
			{
				for (int x = 0; x < map.GetLength(1); ++x)
				{
					map[z, x] += tempMap[z, x] * generationSettings[i].weight;
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
					generationMethod = new SpatialSubdivision(generationSettings[i], generationOffset, seed);
					break;

				case GenerationSettings.GenerationMethodType.PerlinNoise:
					generationMethod = new PerlinNoise(generationSettings[i], generationOffset, seed);
					break;

				case GenerationSettings.GenerationMethodType.Voronoi:
					generationMethod = new VoronoiDiagrams(generationSettings[i], generationOffset, seed);
					break;

				case GenerationSettings.GenerationMethodType.Sine:
					generationMethod = new Sine(generationSettings[i], generationOffset, seed);
					break;
			}

			generationMethods[i] = generationMethod;
		}
	}
}