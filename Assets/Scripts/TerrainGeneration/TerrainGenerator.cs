using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
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

	private Mesh mesh;
	private Vector3[] vertices;
	private int[] triangles;

	private GenerationMethod generationMethod;

	// Start is called before the first frame update
	private void Start() {
		GenerateMap();
	}

	private void OnValidate() {
		if (mapSize < 1)
			mapSize = 1;
		else if (mapSize > 255)
			mapSize = 255;

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
	}

	public void GenerateMap() {
		mesh = GetComponent<MeshFilter>().sharedMesh;
		switch (methodType) {
			case GenerationMethodType.SpatialSubdivision: {
				generationMethod = new SpatialSubdivision(mapSize, mapSize, seed, mapCellSize, mapSmoothness);
				break;
			}
			case GenerationMethodType.PerlinVoronoiHybrid: {
				generationMethod = new PerlinVoronoiHybrid(mapSize, mapSize, seed, mapCellSize, noiseScale, octaves, persistance, mapSmoothness);
				break;
			}
		}
		if (noiseScale <= 0) {
			noiseScale = 0.0001f;
		}
		var temp = Time.realtimeSinceStartup;

		CreateMesh(generationMethod.CreateHeightMap());
		Debug.Log("Terrain generation execution time = " + (Time.realtimeSinceStartup - temp).ToString());
	}

	public void CreateMesh(float[,] heightMap) {
		vertices = new Vector3[(mapSize + 1) * (mapSize + 1)];

		for (int i = 0, z = 0; z <= mapSize; z++) {
			for (int x = 0; x <= mapSize; x++, i++) {
				vertices[i] = new Vector3(x * mapCellSize, heightMap[x, z] * mapHeightMultiplier, z * mapCellSize);
			}
		}

		triangles = new int[6 * mapSize * mapSize];

		for (int ti = 0, vi = 0, z = 0; z < mapSize; z++, vi++) {
			for (int x = 0; x < mapSize; x++, ti += 6, vi++) {
				triangles[ti] = vi;
				triangles[ti + 3] = triangles[ti + 2] = vi + 1;
				triangles[ti + 4] = triangles[ti + 1] = vi + mapSize + 1;
				triangles[ti + 5] = vi + mapSize + 2;
			}
		}

		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
	}

	public void ClearMesh() {
		mesh.Clear();
	}
}