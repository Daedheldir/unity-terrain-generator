using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	public int mapSize = 64;
	public float mapCellSize = 10;

	[Range(1, 10)]
	public int octaves = 3;

	[Range(0, 1)]
	public float persistance = 0.5f;

	public float mapHeightMultiplier = 10;
	public float mapSmoothness = 0.1f;
	public float noiseScale = 1f;
	public float perlinWeight = 1f;

	public int voronoiPoints = 50;

	public float voronoiScale = 1f;
	public float voronoiWeight = 1f;

	[Range(0f, 1f)]
	public float voronoiMaskScale = 0.1f;

	public float voronoiMaskWeight = 1f;
	public int voronoiOctaves = 3;
	public float voronoiPersistance = 0.5f;

	[Range(0.1f, 2f)]
	public float voronoiSmoothing = 0.5f;

	private Texture2D texture;
	private Terrain terrain;

	private GenerationMethod generationMethod;

	// Start is called before the first frame update
	private void Start() {
		GenerateMap();
	}

	private void OnValidate() {
		if (mapSize < 1)
			mapSize = 1;
		else if (mapSize > 512)
			mapSize = 512;

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

		if (voronoiSmoothing <= 0f)
			voronoiSmoothing = 0.1f;
		if (voronoiSmoothing > 2f)
			voronoiSmoothing = 2f;

		if (voronoiPersistance <= -3f)
			voronoiPersistance = -3f;
		if (voronoiPersistance > 3f)
			voronoiPersistance = 3f;
	}

	public void GenerateMap() {
		terrain = GetComponent<Terrain>();

		switch (methodType) {
			case GenerationMethodType.SpatialSubdivision: {
				generationMethod = new SpatialSubdivision(mapSize, seed, mapCellSize, mapSmoothness);
				break;
			}
			case GenerationMethodType.PerlinVoronoiHybrid: {
				generationMethod = new PerlinVoronoiHybrid(mapSize, seed, mapCellSize,
					noiseScale, perlinWeight, octaves, persistance, mapSmoothness,
					voronoiPoints, voronoiScale, voronoiWeight, voronoiMaskScale, voronoiMaskWeight, voronoiOctaves, voronoiPersistance, voronoiSmoothing);
				break;
			}
		}
		if (noiseScale <= 0) {
			noiseScale = 0.0001f;
		}
		var temp = Time.realtimeSinceStartup;

		//CreateMesh(generationMethod.CreateHeightMap());
		float[,] heightMap = generationMethod.CreateHeightMap();
		//texture = CreateTexture(heightMap);
		TerrainData tData = new TerrainData();
		tData.heightmapResolution = mapSize + 1;
		tData.size = new Vector3(mapSize, mapHeightMultiplier, mapSize);
		tData.SetHeights(0, 0, heightMap);
		terrain.terrainData = tData;
		Debug.Log("Terrain generation execution time = " + (Time.realtimeSinceStartup - temp).ToString());
	}

	public Texture2D CreateTexture(float[,] heightMap) {
		int mapWidth = heightMap.GetLength(0);
		int mapHeight = heightMap.GetLength(1);

		Color[] pixelColors = new Color[mapWidth * mapHeight];

		for (int y = 0; y < mapWidth; ++y) {
			for (int x = 0; x < mapHeight; ++x) {
				pixelColors[y * mapWidth + x] = new Color(heightMap[x, y], heightMap[x, y], heightMap[x, y]);
			}
		}

		Texture2D texture = new Texture2D(mapWidth, mapHeight);
		texture.SetPixels(pixelColors);

		return texture;
	}
}