using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TerrainGenerator : MonoBehaviour
{
	private Mesh mesh;
	private Vector3[] vertices;
	private int[] triangles;

	private GenerationMethod generationMethod;
	public int mapCellsX = 64;
	public int mapCellsZ = 64;
	public int mapCellSize = 10;
	public int mapHeightMax = 100;
	public float mapSmoothness = 0.1f;

	// Start is called before the first frame update
	private void Start()
	{
		mesh = GetComponent<MeshFilter>().mesh;
		generationMethod = new SpatialSubdivision(mapCellsX, mapCellsZ, mapCellSize, 3, mapSmoothness);

		var temp = Time.realtimeSinceStartup;

		CreateMesh(generationMethod.CreateHeightMap());
		Debug.Log("Terrain generation execution time = " + (Time.realtimeSinceStartup - temp).ToString());
	}

	// Update is called once per frame
	private void Update()
	{
	}

	public void CreateMesh(float[,] heightMap)
	{
		vertices = new Vector3[(mapCellsZ + 1) * (mapCellsX + 1)];

		for (int i = 0, z = 0; z <= mapCellsZ; z++)
		{
			for (int x = 0; x <= mapCellsX; x++, i++)
			{
				vertices[i] = new Vector3(x * mapCellSize, heightMap[z, x] * mapHeightMax, z * mapCellSize);
			}
		}

		triangles = new int[6 * mapCellsX * mapCellsZ];

		for (int ti = 0, vi = 0, z = 0; z < mapCellsZ; z++, vi++)
		{
			for (int x = 0; x < mapCellsX; x++, ti += 6, vi++)
			{
				triangles[ti] = vi;
				triangles[ti + 3] = triangles[ti + 2] = vi + 1;
				triangles[ti + 4] = triangles[ti + 1] = vi + mapCellsX + 1;
				triangles[ti + 5] = vi + mapCellsX + 2;
			}
		}

		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
	}

}