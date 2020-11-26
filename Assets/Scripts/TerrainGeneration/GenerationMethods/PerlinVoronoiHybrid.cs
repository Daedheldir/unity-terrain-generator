using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinVoronoiHybrid : GenerationMethod
{
	public float perlinScale = 1f;
	public float perlinWeight = 1f;
	public float persistance = 0.5f;
	public int octaves = 3;

	public float voronoiScale = 1f;
	public float voronoiWeight = 1f;
	public float voronoiMaskScale = 1f;
	public float voronoiMaskWeight = 1f;

	public int numberOfVoronoiPoints = 50;

	public PerlinVoronoiHybrid
		(
		int mapSizeX, int seed, float mapCellSize,
		float perlinScale, float perlinWeight, int octaves, float persistance, float smoothingFactor,
		int voronoiPoints, float voronoiScale, float voronoiWeight, float voronoiMaskScale, float voronoiMaskWeight
		) {

		//
		this.mapSize = mapSizeX;
		this.mapCellSize = mapCellSize;

		this.perlinScale = perlinScale;
		this.perlinWeight = perlinWeight;

		this.numberOfVoronoiPoints = voronoiPoints;
		this.voronoiScale = voronoiScale;
		this.voronoiWeight = voronoiWeight;
		this.voronoiMaskScale = voronoiMaskScale;
		this.voronoiMaskWeight = voronoiMaskWeight;

		this.seed = seed;
		this.octaves = octaves;
		this.persistance = persistance;
		this.smoothingFactor = smoothingFactor;

		prng = new System.Random(seed);
	}

	public override float[,] CreateHeightMap() {
		float[,] heightMap = new float[mapSize + 1, mapSize + 1];

		float[,] voronoiMap = CreateVoronoiGraph();
		float[,] voronoiMask = CreateNoiseMask(voronoiMaskScale);
		float[,] perlinMap = CreatePerlinNoise();

		for (int z = 0; z < mapSize + 1; ++z) {
			for (int x = 0; x < mapSize + 1; ++x) {
				heightMap[x, z] = (perlinMap[x, z] * perlinWeight) + ((voronoiMap[x, z] * (voronoiMask[x, z] * voronoiMaskWeight)) * voronoiWeight);
			}
		}
		return heightMap;
	}

	private float[,] CreateNoiseMask(float scale) {
		float[,] map = new float[mapSize + 1, mapSize + 1];

		float maxValue = float.MinValue;
		float minValue = float.MaxValue;

		for (int z = 0; z < mapSize + 1; ++z) {
			for (int x = 0; x < mapSize + 1; ++x) {
				float noiseHeight = 0f;

				float sampleZ = (z) / scale;
				float sampleX = (x) / scale;

				float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);

				noiseHeight += perlinValue;

				if (noiseHeight > maxValue)
					maxValue = noiseHeight;
				else if (noiseHeight < minValue)
					minValue = noiseHeight;

				map[x, z] = noiseHeight;
			}
		}

		//normalizing values to be between 0-1
		for (int z = 0; z < mapSize + 1; ++z) {
			for (int x = 0; x < mapSize + 1; ++x) {
				map[x, z] = Mathf.InverseLerp(minValue, maxValue, map[x, z]);
			}
		}

		return map;
	}

	private float[,] CreatePerlinNoise() {
		float[,] map = new float[mapSize + 1, mapSize + 1];

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

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1;

					noiseHeight += perlinValue * amplitude;

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
		for (int z = 0; z < mapSize + 1; ++z) {
			for (int x = 0; x < mapSize + 1; ++x) {
				map[x, z] = Mathf.InverseLerp(minValue, maxValue, map[x, z]);
			}
		}

		return map;
	}

	private float[,] CreateVoronoiGraph() {
		float[,] map = new float[mapSize + 1, mapSize + 1];

		//creating random points
		Vector2[] points = new Vector2[numberOfVoronoiPoints];
		List<List<Vector2>> pointsNeighbours = new List<List<Vector2>>();
		for (int i = 0; i < numberOfVoronoiPoints; ++i) {
			points[i] = new Vector2(prng.Next(0, mapSize), prng.Next(0, mapSize));
			pointsNeighbours.Add(new List<Vector2>());
		}
		float maxValue = float.MinValue;
		float minValue = float.MaxValue;

		for (int z = 0; z < mapSize + 1; ++z) {
			for (int x = 0; x < mapSize + 1; ++x) {
				float minDistance = float.MaxValue;
				float maxDistance = float.MinValue;

				Vector2 mapPoint = new Vector2(x, z);

				//find closest point to current map cell
				for (int i = 0; i < points.Length; ++i) {
					float currentDistance = Vector2.Distance(mapPoint, points[i]);

					if (currentDistance < minDistance) {
						minDistance = currentDistance;
					}
					if (currentDistance > maxDistance) {
						maxDistance = currentDistance;
					}
				}

				//change height depending on distance from closest point

				float noiseHeight = CosineInterpolate(0, 1, minDistance / maxDistance);

				if (noiseHeight > maxValue)
					maxValue = noiseHeight;
				else if (noiseHeight < minValue)
					minValue = noiseHeight;

				map[x, z] = voronoiScale * noiseHeight;
			}
		}

		//normalizing values to be between 0-1
		for (int z = 0; z < mapSize + 1; ++z) {
			for (int x = 0; x < mapSize + 1; ++x) {
				map[x, z] = Mathf.InverseLerp(minValue, maxValue, map[x, z]);
			}
		}

		return map;
	}

	private float CosineInterpolate(float min, float max, float ang) {
		float angle;

		angle = (1f - Mathf.Cos(ang * Mathf.PI)) / 2f;
		return (min * (1f - angle) + max * angle);
	}
}