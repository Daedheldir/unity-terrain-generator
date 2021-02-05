﻿using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class TerrainGenerator : MonoBehaviour
{
	public Camera mainCamera;

	private ChunkData chunkData;
	public bool drawGradientRays;

	public Material terrainMaterial;
	public Material seaMaterial;

	public IGenerationMethod[] generationMethods;

	[SerializeField]
	public GenerationSettings[] generationSettings;

	public bool useFirstHeightMapAsMask = true;
	public bool autoUpdateMap = false;
	public int seed = 0;
	public float mapHeightMultiplier = 10;
	public float seaLevel = 1f;

	//LOD
	public int chunkSize = 241;

	public Vector2 generationOffset = new Vector2(0, 0);

	[Range(0, 9), SerializeField]
	private int LOD;

	private int[] LODLookupTable = { 0, 1, 2, 3, 4, 5, 6, 8, 10, 12, 15, 30 };

	private MeshFilter meshFilter;

	public MeshFilter MeshFilter { get => meshFilter; set => meshFilter = value; }

	//Threading
	private Queue<ChunkThreadInfo<ChunkData>> chunkThreadInfosQueue = new Queue<ChunkThreadInfo<ChunkData>>();

	private Queue<ChunkThreadInfo<MeshData>> meshDataInfosQueue = new Queue<ChunkThreadInfo<MeshData>>();

	private static Mutex unityMutex = new Mutex();

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
		this.chunkData = chunkData;
		MeshDataThread(EditorOnMeshDataReceived, chunkData);
	}

	private void EditorOnMeshDataReceived(MeshData meshData)
	{
		ClearMap();

		MeshFilter.sharedMesh = meshData.CreateMesh();

		if (drawGradientRays)
		{
			for (int z = 0; z < chunkData.gradientMap.GetLength(0); z += 8)
			{
				for (int x = 0; x < chunkData.gradientMap.GetLength(1); x += 8)
				{
					Debug.Log(x + ", " + z);
					float color = Mathf.Clamp(Mathf.Abs(chunkData.gradientMap[x, z].normalized.y) * 10, 0, 1);
					Debug.DrawRay(
						MeshFilter.sharedMesh.vertices[z * chunkData.gradientMap.GetLength(1) + x],
						chunkData.gradientMap[x, z] * 30,
						new Color(color, 1 - color, 1 - color),
						60);
				}
			}
		}
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
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(chunkData.heightMap, mapHeightMultiplier, LODLookupTable[LOD]);

		//lock queue so no other threads access it
		lock (meshDataInfosQueue)
		{
			meshDataInfosQueue.Enqueue(new ChunkThreadInfo<MeshData>(callback, meshData));
		}
	}

	public ChunkData GenerateChunkData(Vector2 offset)
	{
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
		Task<float[,]>[] tasksList = new Task<float[,]>[activeGenerationMethods.Count];

		//generate heightmaps
		for (int i = 0; i < activeGenerationMethods.Count; ++i)
		{
			int index = i;
			tasksList[i] = Task.Factory.StartNew(delegate
			  {
				  return activeGenerationMethods[index].CreateHeightMap(offset * new Vector2(1, -1));
			  });
		}
		float[,] map = SumGeneratedHeightmaps(tasksList, activeGenerationSettings);
		return new ChunkData(map);
	}

	private float[,] SumGeneratedHeightmaps(Task<float[,]>[] runningTasks, List<GenerationSettings> generationSettings)
	{
		float[,] map = new float[chunkSize, chunkSize];
		float[,] mask = null;
		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int i = 0; i < runningTasks.Length; ++i)
		{
			for (int z = 0; z < map.GetLength(0); ++z)
			{
				for (int x = 0; x < map.GetLength(1); ++x)
				{
					float[,] tempMap = runningTasks[i].Result;
					map[x, z] += tempMap[x, z] * generationSettings[i].weight;

					if (map[x, z] > maxValue)
						maxValue = map[x, z];
					else if (map[x, z] < minValue)
						minValue = map[x, z];
				}
			}
		}

		//generate mask if its required
		//all threads should finish by now
		if (useFirstHeightMapAsMask)
		{
			mask = runningTasks[0].Result;
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

		return map;
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

	private void OnDestroy()
	{
	}
}

public struct ChunkData
{
	public readonly float[,] heightMap;
	public readonly Vector3[,] gradientMap;

	public ChunkData(float[,] heightMap)
	{
		this.heightMap = heightMap;
		int height = heightMap.GetLength(0);
		int width = heightMap.GetLength(1);

		//calculating gradient
		this.gradientMap = new Vector3[width, height];

		for (int z = 0; z < height; ++z)
		{
			for (int x = 0; x < width; ++x)
			{
				float slopeX = heightMap[x < width - 1 ? x + 1 : x, z] - heightMap[x > 0 ? x - 1 : x, z];
				float slopeZ = heightMap[x, z < height - 1 ? z + 1 : z] - heightMap[x, z > 0 ? z - 1 : z];

				if (x == 0 || x == width - 1)
					slopeX *= 2;
				if (z == 0 || z == height - 1)
					slopeZ *= 2;

				Vector3 normal = new Vector3(-slopeX * (width - 1), (width - 1), slopeZ * (height - 1));
				normal.Normalize();

				gradientMap[x, z] = normal - Vector3.up;
			}
		}
	}
}