using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PerlinVoronoiHybrid : GenerationMethod
{
	private static Mutex mapWeightPairsMu = new Mutex();

	public float perlinScale = 1f;
	public float perlinWeight = 1f;
	public float persistance = 0.5f;
	public int octaves = 3;

	public float voronoiMinHeight = 0.1f;
	public float voronoiScale = 1f;
	public float voronoiWeight = 1f;
	private float[,] voronoiPerlinMask;
	public float voronoiMaskScale = 0.01f;
	public float voronoiMaskWeight = 1f;
	public int voronoiOctaves = 3;
	public float voronoiPersistance = 0.5f;
	public float voronoiSmoothing = 0.5f;

	public int numberOfVoronoiPoints = 50;

	public PerlinVoronoiHybrid
		(
		int mapSizeX, int seed,
		float perlinScale, float perlinWeight, int octaves, float persistance, float smoothingFactor,
		int voronoiPoints, float voronoiMinHeight, float voronoiScale, float voronoiWeight, float voronoiMaskScale, float voronoiMaskWeight, int voronoiOctaves, float voronoiPersistance, float voronoiSmoothing
		) {

		//
		this.mapSize = mapSizeX;
		this.seed = seed;

		this.perlinScale = perlinScale;
		this.perlinWeight = perlinWeight;
		this.octaves = octaves;
		this.persistance = persistance;
		this.smoothingFactor = smoothingFactor;

		this.numberOfVoronoiPoints = voronoiPoints;
		this.voronoiMinHeight = voronoiMinHeight;
		this.voronoiOctaves = voronoiOctaves;
		this.voronoiScale = voronoiScale;
		this.voronoiWeight = voronoiWeight;
		this.voronoiMaskScale = voronoiMaskScale;
		this.voronoiMaskWeight = voronoiMaskWeight;
		this.voronoiPersistance = voronoiPersistance;
		this.voronoiSmoothing = voronoiSmoothing;

		prng = new System.Random(seed);
	}

	/// <summary>
	/// Creates height map with values in range [0,1]
	/// </summary>
	/// <returns>
	/// </returns>
	public override float[,] CreateHeightMap() {
		float[,] perlinMap = CreatePerlinNoise();
		float[,] voronoiMap = CreateVoronoiGraph();

		float maxValue = float.MinValue;
		float minValue = float.MaxValue;

		//merging all height maps
		float[,] heightMap = new float[mapSize + 1, mapSize + 1];

		for (int z = 0; z < mapSize + 1; ++z) {
			for (int x = 0; x < mapSize + 1; ++x) {

				//adding Perlin
				float noiseHeight = perlinMap[x, z] * perlinWeight;

				//adding Voronoi
				float voronoiValue = voronoiMap[x, z] * (Mathf.Pow(voronoiPerlinMask[x, z], voronoiMaskWeight));
				noiseHeight += voronoiValue * voronoiWeight;

				if (noiseHeight > maxValue)
					maxValue = noiseHeight;
				else if (noiseHeight < minValue)
					minValue = noiseHeight;

				heightMap[x, z] += noiseHeight;
			}
		}

		//heightMap = NormalizeMap(heightMap, minValue, maxValue);
		return heightMap;
	}

	private float[,] CreatePerlinNoise() {
		float[,] map = new float[mapSize + 1, mapSize + 1];
		float[,] firstOctave = new float[mapSize + 1, mapSize + 1];

		Vector2[] octaveOffsets = new Vector2[octaves];

		for (int i = 0; i < octaves; ++i) {
			float offsetX = prng.Next(-100000, 100000);
			float offsetZ = prng.Next(-100000, 100000);
			octaveOffsets[i] = new Vector2(offsetX, offsetZ);
		}

		float maxValue = float.MinValue;
		float minValue = float.MaxValue;

		float halfX = mapSize / 2f;
		float halfZ = mapSize / 2f;
		for (int z = 0; z < mapSize + 1; ++z) {
			for (int x = 0; x < mapSize + 1; ++x) {
				float amplitude = 1;
				float frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < octaves; ++i) {
					float sampleZ = (z - halfZ) / perlinScale * frequency + octaveOffsets[i].y;
					float sampleX = (x - halfX) / perlinScale * frequency + octaveOffsets[i].x;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);

					noiseHeight += perlinValue * amplitude;

					if (i == 0)
						firstOctave[x, z] = noiseHeight;

					amplitude *= persistance;
					frequency *= smoothingFactor;
				}

				if (noiseHeight > maxValue)
					maxValue = noiseHeight;
				else if (noiseHeight < minValue)
					minValue = noiseHeight;

				map[x, z] = noiseHeight;
			}
		}

		//normalizing values to be between 0-1
		map = NormalizeMap(map, minValue, maxValue);

		//wait for access to resources
		voronoiPerlinMask = firstOctave;
		return map;
	}

	private float[,] CreateVoronoiGraph() {
		float[,] map = new float[mapSize + 1, mapSize + 1];

		//creating random points
		List<List<Vector2>> points = new List<List<Vector2>>();
		float frequency = 1f;

		//clamping voronoiSmoothing
		if (voronoiSmoothing <= 0f)
			voronoiSmoothing = 0.00001f;

		for (int j = 0; j < voronoiOctaves; ++j) {
			points.Add(new List<Vector2>());    //add a new octave points list
			for (int i = 0; i < numberOfVoronoiPoints * frequency; ++i) {
				points[j].Add(new Vector2(prng.Next(0, mapSize), prng.Next(0, mapSize)));
			}

			//after each octave, next octave should have more points so i has more details
			frequency /= voronoiSmoothing;
		}

		float maxValue = float.MinValue;
		float minValue = float.MaxValue;

		for (int z = 0; z < mapSize + 1; ++z) {
			for (int x = 0; x < mapSize + 1; ++x) {
				float minDistance = float.MaxValue;
				float maxDistance = float.MinValue;

				float amplitude = 1f;
				float noiseHeight = 0f;

				Vector2 mapPoint = new Vector2(x, z);

				//iterate through all octaves
				for (int j = 0; j < voronoiOctaves; ++j) {

					//find closest point to current map cell
					for (int i = 0; i < points[j].Count; ++i) {
						float currentDistance = Vector2.Distance(mapPoint, points[j][i]);

						if (currentDistance < minDistance) {
							minDistance = currentDistance;
						}
						if (currentDistance > maxDistance) {
							maxDistance = currentDistance;
						}
					}

					//change height depending on distance from closest point
					noiseHeight += CosineInterpolate(0f, 1f, minDistance / maxDistance) * amplitude;

					//clamp the height where voronoi will appear
					noiseHeight = Mathf.Max(0, noiseHeight - voronoiMinHeight);

					//updating frequency and amplitude for next octave
					amplitude *= voronoiPersistance;
				}
				noiseHeight *= voronoiScale;
				if (noiseHeight > maxValue)
					maxValue = noiseHeight;
				else if (noiseHeight < minValue)
					minValue = noiseHeight;

				map[x, z] = noiseHeight;
			}
		}

		//normalizing values to be between 0-1
		map = NormalizeMap(map, minValue, maxValue);

		//wait for access to resources
		return map;
	}

	private float CosineInterpolate(float min, float max, float ang) {
		float angle;

		angle = (1f - Mathf.Cos(ang * Mathf.PI)) / 2f;
		return (min * (1f - angle) + max * angle);
	}
}