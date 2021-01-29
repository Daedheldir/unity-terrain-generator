using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
	public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, int LOD)
	{
		//map size
		int width = heightMap.GetLength(0);
		int height = heightMap.GetLength(1);

		//LOD
		int meshIncrement = (LOD == 0) ? 1 : (LOD * 2);
		int verticesPerLine = ((width - 1) / meshIncrement) + 1;

		//Mesh data
		MeshData meshData = new MeshData(width, height);
		int vertexIndex = 0;

		//offset to put map in the center
		float mapTopLeftX = (width - 1) / -2f;
		float mapTopLeftZ = (height - 1) / 2f;

		for (int z = 0; z < height; z += meshIncrement)
		{
			for (int x = 0; x < width; x += meshIncrement)
			{
				meshData.vertices[vertexIndex] = new Vector3((mapTopLeftX + x), heightMap[x, z] * heightMultiplier, (mapTopLeftZ - z));
				meshData.uvs[vertexIndex] = new Vector2(x / (float)width, z / (float)height);
				if (x < width - 1 && z < height - 1)
				{
					meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
					meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
				}
				vertexIndex++;
			}
		}

		return meshData;
	}
}

public class MeshData
{
	public Vector3[] vertices;
	public int[] triangles;
	public Vector2[] uvs;

	private int triangleIndex = 0;

	public MeshData(int meshWidth, int meshHeight)
	{
		vertices = new Vector3[meshWidth * meshHeight];
		uvs = new Vector2[meshWidth * meshHeight];
		triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
	}

	public void AddTriangle(int a, int b, int c)
	{
		triangles[triangleIndex] = a;
		triangles[triangleIndex + 1] = b;
		triangles[triangleIndex + 2] = c;

		triangleIndex += 3;
	}

	public Mesh CreateMesh()
	{
		Mesh mesh = new Mesh();

		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		return mesh;
	}
}