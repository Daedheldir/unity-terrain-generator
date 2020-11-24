using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialSubdivision : MonoBehaviour
{
	Mesh mesh;

	Vector3[] vertices;
	int[] triangles;

	public int mapCellsX = 256;
	public int mapCellsZ = 256;

	public int mapCellSize = 10;

	public int smoothingSteps = 3;

	[Range(0f, 1f)]
	public float smoothingFactor = 0.1f;

	private float[,] genInputArr()
	{
		float[,] input = new float[2, 2];
		for (int z = 0; z < 2; ++z)
		{
			for (int x = 0; x < 2; ++x)
			{
				input[z, x] = Random.Range(-1f, 1f) ;
			}
		}

		return input;
	}

	private float[,] genNoiseMap()
	{
		float[,] noiseMap = new float[mapCellsZ, mapCellsX];
		for (int z = 0; z < mapCellsZ; ++z)
		{
			for (int x = 0; x < mapCellsX; ++x)
			{
				noiseMap[z, x] = Random.value;
			}
		}
		return noiseMap;
	}

	private float[,] genMap(float[,] previousMap, int step)
	{
		if (previousMap.Length >= (1+mapCellsX) * (1+mapCellsZ))
		{
			return previousMap;
		}

		//divide each tile in two <=> create new map two times bigger
		float[,] newMap = new float[previousMap.GetLength(0) * 2, previousMap.GetLength(1) * 2];

		for (int z = 0; z < previousMap.GetLength(0); ++z)
		{
			for (int x = 0; x < previousMap.GetLength(1); ++x)
			{
				newMap[z * 2, x * 2] = previousMap[z, x];
			}
		}

		//find values for empty points diagonally
		for (int z = 1; z < newMap.GetLength(0); z += 2)
		{
			for (int x = 1; x < newMap.GetLength(1); x += 2)
			{
					newMap[z, x] = averageDiagonal(newMap, z, x) + Random.Range(-smoothingFactor / (step + 0.1f), smoothingFactor / (step + 0.1f));
			}
		}

		//find values for empty points orthogonally
		for (int z = 0; z < newMap.GetLength(0); ++z)
		{
			for (int x = (z+1) % 2; x < newMap.GetLength(1); x += 2)
			{
				newMap[z, x] = averageOrthogonal(newMap, z, x) + Random.Range(-smoothingFactor / (step + 0.1f), smoothingFactor / (step + 0.1f));
			}
		}

		return genMap(newMap, step + 1);
	}
	private float averageOrthogonal(float[,] map, int z, int x)
	{
		//create average of nearby points
		float average = 0;

		//x underflow handling
		if (x - 1 < 0)
			average += map[z, map.GetLength(1) - 1];
		else
			average += map[z, x - 1];

		//x overflow handling
		if (x + 1 >= map.GetLength(1))
			average += map[z, 0];
		else
			average += map[z, x + 1];

		//y underflow handling
		if (z - 1 < 0)
			average += map[map.GetLength(0) - 1, x];
		else
			average += map[z - 1, x];

		//y overflow handling
		if (z + 1 >= map.GetLength(0))
			average += map[0, x];
		else
			average += map[z + 1, x];

		return average / 4f;
	}
	private float averageDiagonal(float[,] map, int y, int x)
	{
		//create average of nearby points
		float average = 0;

		average += map[y - 1, x - 1];

		//y overflow handling
		if (y + 1 >= map.GetLength(0))
		{
			average += map[0, x - 1];
			//x overflow handling
			if (x + 1 >= map.GetLength(1))
				average += map[0, 0];
			else
				average += map[0, x + 1];
		}
		else
		{
			average += map[y + 1, x - 1];
			//x overflow handling
			if (x + 1 >= map.GetLength(1))
				average += map[y + 1, 0];
			else
				average += map[y + 1, x + 1];

		}

		if (x + 1 >= map.GetLength(1))
			average += map[y - 1, 0];
		else
			average += map[y - 1, x + 1];

		return average / 4f;
	}

	private void CreateMesh(float[,] heightMap)
	{
		vertices = new Vector3[(mapCellsZ + 1) * (mapCellsX + 1)];
		WaitForSeconds wait = new WaitForSeconds(0f);
		for (int i = 0, z = 0; z <= mapCellsZ; z++)
		{
			for (int x = 0; x <= mapCellsX; x++, i++)
			{
				vertices[i] = new Vector3(x*mapCellSize, heightMap[z,x]*10, z*mapCellSize);
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
				UpdateMesh();			
			}
		}

		UpdateMesh();

	}

	private void UpdateMesh()
	{
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;

		//mesh.RecalculateNormals();
	}

	// Start is called before the first frame update
	private void Start()
	{
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

		float[,] map = genInputArr();

		float[,] newMap = genMap(map, 0);

		CreateMesh(newMap);
	}

	// Update is called once per frame
	private void Update()
	{
	}

	private void OnDrawGizmos()
	{
		if(vertices == null)
		{ return; }
		foreach(Vector3 vertex in vertices)
		{
			Gizmos.DrawSphere(vertex, .5f);
		}
	}
}