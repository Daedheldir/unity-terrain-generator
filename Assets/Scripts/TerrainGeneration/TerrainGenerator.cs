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

	public bool useFirstHeightMapAsMask = true;
	public bool autoUpdateMap = false;
	public int seed = 0;
	public float mapCellSize = 10;
	public float mapHeightMultiplier = 10;

	//LOD
	public int chunkSize = 241;

	public Vector2 generationOffset = new Vector2(0, 0);

	[Range(0, 6)]
	public int LOD;

	private Mesh mesh;

	private MeshFilter meshFilter;
	private MeshCollider meshCollider;

	public MeshCollider MeshCollider { get => meshCollider; set => meshCollider = value; }
	public MeshFilter MeshFilter { get => meshFilter; set => meshFilter = value; }
	public Mesh Mesh { get => mesh; set => mesh = value; }

	// Start is called before the first frame update
	private void Start()
	{
		//GenerateMap();
	}

	private void OnValidate()
	{
		if (chunkSize < 10)
			chunkSize = 10;
		else if (chunkSize > 241)
			chunkSize = 241;

		if (mapCellSize <= 0)
			mapCellSize = 0.0001f;

		if (mapHeightMultiplier <= 0)
			mapHeightMultiplier = 0.0001f;
	}

	public TerrainData GenerateMapData()
	{
		var temp = Time.realtimeSinceStartup;

		UpdateNoiseArray(generationSettings);
		MeshFilter = GetComponent<MeshFilter>();
		MeshCollider = GetComponent<MeshCollider>();

		float[,] map = new float[chunkSize, chunkSize];
		float[,] mask = null;
		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		List<IGenerationMethod> activeGenerationMethods = new List<IGenerationMethod>();
		List<GenerationSettings> activeGenerationSettings = new List<GenerationSettings>();

		for (int i = 0; i < generationMethods.Length; ++i)
		{
			//if method isnt active skip it
			if (!generationSettings[i].isActive)
				continue;

			activeGenerationMethods.Add(generationMethods[i]);
			activeGenerationSettings.Add(generationSettings[i])
;
		}

		for (int i = 0; i < activeGenerationMethods.Count; ++i)
		{
			float[,] tempMap = activeGenerationMethods[i].CreateHeightMap();

			if (i == 0 && useFirstHeightMapAsMask)
			{
				mask = (float[,])tempMap.Clone();
			}

			for (int z = 0; z < map.GetLength(0); ++z)
			{
				for (int x = 0; x < map.GetLength(1); ++x)
				{
					map[x, z] += tempMap[x, z] * activeGenerationSettings[i].weight;

					if (map[x, z] > maxValue)
						maxValue = map[x, z];
					else if (map[x, z] < minValue)
						minValue = map[x, z];
				}
			}
		}
		for (int z = 0; z < map.GetLength(0); ++z)
		{
			for (int x = 0; x < map.GetLength(1); ++x)
			{
				if (useFirstHeightMapAsMask)
					map[x, z] *= mask[x, z];

				map[x, z] *= mapHeightMultiplier;
			}
		}
		//CreateMesh(map);

		Debug.Log("Terrain generation execution time = " + (Time.realtimeSinceStartup - temp).ToString());

		return new TerrainData(map);
	}

	public void CreateMesh(float[,] heightMap)
	{
		ClearMap();

		MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, mapHeightMultiplier, LOD);
		Mesh = meshData.CreateMesh();

		MeshFilter.mesh = Mesh;
		MeshCollider.sharedMesh = Mesh;
	}

	public void ClearMap()
	{
		if (Mesh != null)
			Mesh.Clear();
	}

	public void UpdateNoiseArray(GenerationSettings[] generationSettings)
	{
		generationMethods = new IGenerationMethod[generationSettings.Length];

		for (int i = 0; i < generationSettings.Length; ++i)
		{
			generationSettings[i].ChunkSize = chunkSize;

			IGenerationMethod generationMethod = null;

			switch (generationSettings[i].methodType)
			{
				case GenerationSettings.GenerationMethodType.SpatialSubdivision:
					generationMethod = new SpatialSubdivision(generationSettings[i], generationOffset, seed);
					break;

				case GenerationSettings.GenerationMethodType.PerlinNoise:
					generationMethod = new PerlinNoise(generationSettings[i], generationOffset, seed);
					break;

				case GenerationSettings.GenerationMethodType.RidgedPerlinNoise:
					generationMethod = new RidgedPerlinNoise(generationSettings[i], generationOffset, seed);
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

	public struct TerrainData
	{
		public float[,] heightMap;

		public TerrainData(float[,] heightMap)
		{
			this.heightMap = heightMap;
		}
	}
}