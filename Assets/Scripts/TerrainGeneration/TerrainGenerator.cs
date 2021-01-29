using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class TerrainGenerator : MonoBehaviour
{
	public Material terrainMaterial;

	public IGenerationMethod[] generationMethods;

	[SerializeField]
	public GenerationSettings[] generationSettings;

	public bool useFirstHeightMapAsMask = true;
	public bool autoUpdateMap = false;
	public int seed = 0;
	public float mapHeightMultiplier = 10;

	//LOD
	public int chunkSize = 241;

	public Vector2 generationOffset = new Vector2(0, 0);

	[Range(0, 6)]
	public int LOD;

	private MeshFilter meshFilter;

	public MeshFilter MeshFilter { get => meshFilter; set => meshFilter = value; }

	//Threading
	private Queue<ChunkThreadInfo<ChunkData>> chunkThreadInfosQueue = new Queue<ChunkThreadInfo<ChunkData>>();

	private Queue<ChunkThreadInfo<MeshData>> meshDataInfosQueue = new Queue<ChunkThreadInfo<MeshData>>();

	// Start is called before the first frame update
	private void Start()
	{
		MeshFilter = GetComponent<MeshFilter>();
		InitNoiseArray(generationSettings);
	}

	private void Update()
	{
		if (chunkThreadInfosQueue.Count > 0)
		{
			lock (chunkThreadInfosQueue)
			{
				for (int i = 0; i < chunkThreadInfosQueue.Count; ++i)
				{
					ChunkThreadInfo<ChunkData> chunkThreadInfo = chunkThreadInfosQueue.Dequeue();
					chunkThreadInfo.callback(chunkThreadInfo.parameter);
				}
			}
		}
		if (meshDataInfosQueue.Count > 0)
		{
			lock (meshDataInfosQueue)
			{
				for (int i = 0; i < meshDataInfosQueue.Count; ++i)
				{
					ChunkThreadInfo<MeshData> meshThreadInfo = meshDataInfosQueue.Dequeue();
					meshThreadInfo.callback(meshThreadInfo.parameter);
				}
			}
		}
	}

	private void OnValidate()
	{
		if (chunkSize < 10)
			chunkSize = 10;
		else if (chunkSize > 241)
			chunkSize = 241;

		if (mapHeightMultiplier <= 0)
			mapHeightMultiplier = 0.0001f;
	}

	public void GeneratePreviewMap()
	{
		var temp = Time.realtimeSinceStartup;

		InitNoiseArray(generationSettings);

		MeshFilter = GetComponent<MeshFilter>();

		ChunkDataThread(EditorOnChunkDataReceived, generationOffset);

		Update();

		Debug.Log("Terrain generation execution time = " + (Time.realtimeSinceStartup - temp).ToString());
	}

	private void EditorOnChunkDataReceived(ChunkData chunkData)
	{
		MeshDataThread(EditorOnMeshDataReceived, chunkData);
	}

	private void EditorOnMeshDataReceived(MeshData meshData)
	{
		ClearMap();

		MeshFilter.sharedMesh = meshData.CreateMesh();
	}

	public void RequestChunkData(Action<ChunkData> callback, Vector2 offset)
	{
		ThreadPool.QueueUserWorkItem(delegate
		{ ChunkDataThread(callback, offset); });
	}

	private void ChunkDataThread(Action<ChunkData> callback, Vector2 offset)
	{
		ChunkData chunkData = GenerateChunkData(offset);

		//lock queue so no other threads access it
		lock (chunkThreadInfosQueue)
		{
			chunkThreadInfosQueue.Enqueue(new ChunkThreadInfo<ChunkData>(callback, chunkData));
		}
	}

	public void RequestMeshData(Action<MeshData> callback, ChunkData chunkData)
	{
		ThreadPool.QueueUserWorkItem(delegate
		{ MeshDataThread(callback, chunkData); });
	}

	private void MeshDataThread(Action<MeshData> callback, ChunkData chunkData)
	{
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(chunkData.heightMap, mapHeightMultiplier, LOD);

		//lock queue so no other threads access it
		lock (meshDataInfosQueue)
		{
			meshDataInfosQueue.Enqueue(new ChunkThreadInfo<MeshData>(callback, meshData));
		}
	}

	public ChunkData GenerateChunkData(Vector2 offset)
	{
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
			activeGenerationSettings.Add(generationSettings[i]);
		}

		for (int i = 0; i < activeGenerationMethods.Count; ++i)
		{
			float[,] tempMap = activeGenerationMethods[i].CreateHeightMap(offset * new Vector2(1, -1));

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

		return new ChunkData(map);
	}

	public void ClearMap()
	{
		if (meshFilter != null)
			if (meshFilter.sharedMesh == null)
				meshFilter.sharedMesh = new Mesh();
			else
				meshFilter.sharedMesh.Clear();
	}

	public void InitNoiseArray(GenerationSettings[] generationSettings)
	{
		generationMethods = new IGenerationMethod[generationSettings.Length];

		for (int i = 0; i < generationSettings.Length; ++i)
		{
			generationSettings[i].ChunkSize = chunkSize;
			IGenerationMethod generationMethod = null;

			switch (generationSettings[i].methodType)
			{
				case GenerationSettings.GenerationMethodType.SpatialSubdivision:
					generationMethod = new SpatialSubdivision(generationSettings[i], seed);
					break;

				case GenerationSettings.GenerationMethodType.PerlinNoise:
					generationMethod = new PerlinNoise(generationSettings[i], seed);
					break;

				case GenerationSettings.GenerationMethodType.RidgedPerlinNoise:
					generationMethod = new RidgedPerlinNoise(generationSettings[i], seed);
					break;

				case GenerationSettings.GenerationMethodType.Voronoi:
					generationMethod = new VoronoiDiagrams(generationSettings[i], seed);
					break;

				case GenerationSettings.GenerationMethodType.Sine:
					generationMethod = new Sine(generationSettings[i], seed);
					break;
			}

			generationMethods[i] = generationMethod;
		}
	}

	private struct ChunkThreadInfo<T>
	{
		public readonly Action<T> callback;
		public readonly T parameter;

		public ChunkThreadInfo(Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}
	}
}

public struct ChunkData
{
	public readonly float[,] heightMap;

	public ChunkData(float[,] heightMap)
	{
		this.heightMap = heightMap;
	}
}