using System;
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
	public bool saveToFile = false;

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
	public float scaleMultiplier = 1f;
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

		if (scaleMultiplier <= 0)
			scaleMultiplier = 0.001f;

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
		if (saveToFile)
			HeightMapSaver.SaveToTexture(dh.Math.NormalizeMap(chunkData.heightMap), "map");

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
					DrawArrow.ForDebug(
						MeshFilter.sharedMesh.vertices[z * chunkData.gradientMap.GetLength(1) + x],
						chunkData.gradientMap[x, z] * 40,
						new Color(color, 1 - color, 1 - color),
						60f,
						2);
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

		float maxWeight = 0;

		//generate mask if its required
		//all threads should finish by now
		if (useFirstHeightMapAsMask)
		{
			mask = runningTasks[0].Result;
		}

		for (int i = 0; i < runningTasks.Length; ++i)
		{
			for (int z = 0; z < map.GetLength(0); ++z)
			{
				for (int x = 0; x < map.GetLength(1); ++x)
				{
					float[,] tempMap = runningTasks[i].Result;

					float addedVal;
					if (useFirstHeightMapAsMask && generationSettings[i].useFirstHeightmapAsMask)
					{
						addedVal = tempMap[x, z] * generationSettings[i].weight * (generationSettings[i].invertFirstHeightmapMask ? 1 - mask[x, z] : mask[x, z]);
					}
					else
						addedVal = tempMap[x, z] * generationSettings[i].weight;

					map[x, z] += generationSettings[i].subtractFromMap ? -addedVal : addedVal;

					if (map[x, z] > maxValue)
						maxValue = map[x, z];
					else if (map[x, z] < minValue)
						minValue = map[x, z];
				}
			}
			if (generationSettings[i].weight > maxWeight)
				maxWeight = generationSettings[i].weight;
		}

		//map = dh.Math.NormalizeMap(map, 0, generationMethods.Length * maxWeight);

		for (int z = 0; z < map.GetLength(0); ++z)
		{
			for (int x = 0; x < map.GetLength(1); ++x)
			{
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

			generationMethods[i] = GetGenerationMethod(generationSettings[i], seed);
		}
	}
	public static IGenerationMethod GetGenerationMethod(GenerationSettings settings, int seed)
	{
		IGenerationMethod generationMethod = null;
		switch (settings.methodType)
		{
			case GenerationSettings.GenerationMethodType.SpatialSubdivision:
				settings.methodName = "Spatial Subdivision";
				generationMethod = new SpatialSubdivision(settings, seed);
				break;

			case GenerationSettings.GenerationMethodType.PerlinNoise:
				settings.methodName = "Perlin";
				generationMethod = new PerlinNoise(settings, seed);
				break;

			case GenerationSettings.GenerationMethodType.RidgedPerlinNoise:
				settings.methodName = "Ridged";
				generationMethod = new RidgedPerlinNoise(settings, seed);
				break;

			case GenerationSettings.GenerationMethodType.Voronoi:
				settings.methodName = "Voronoi";
				generationMethod = new VoronoiDiagrams(settings, seed);
				break;

			case GenerationSettings.GenerationMethodType.Sine:
				settings.methodName = "Sine";
				generationMethod = new Sine(settings, seed);
				break;

			case GenerationSettings.GenerationMethodType.Cosine:
				settings.methodName = "Cosine";
				generationMethod = new Cosine(settings, seed);
				break;

			case GenerationSettings.GenerationMethodType.Billow:
				settings.methodName = "Billow";
				generationMethod = new Billow(settings, seed);
				break;
		}

		return generationMethod;
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

public static class DrawArrow
{
	public static void ForGizmo(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Gizmos.DrawRay(pos, direction);

		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
		Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
	}

	public static void ForGizmo(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Gizmos.color = color;
		Gizmos.DrawRay(pos, direction);

		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
		Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
	}

	public static void ForDebug(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Debug.DrawRay(pos, direction);

		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Debug.DrawRay(pos + direction, right * arrowHeadLength);
		Debug.DrawRay(pos + direction, left * arrowHeadLength);
	}

	public static void ForDebug(Vector3 pos, Vector3 direction, Color color, float duration = 1 / 60f, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Debug.DrawRay(pos, direction, color, duration);

		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Debug.DrawRay(pos + direction, right * arrowHeadLength, color, duration);
		Debug.DrawRay(pos + direction, left * arrowHeadLength, color, duration);
	}
}