using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinVoronoiHybrid : GenerationMethod
{
	public float scale = 1f;
	public float persistance = 0.5f;
	public int octaves = 3;

	public PerlinVoronoiHybrid(int mapSizeX, int mapSizeZ, int seed, float mapCellSize, float scale, int octaves, float persistance, float smoothingFactor) {
		this.mapCellsX = mapSizeX;
		this.mapCellsZ = mapSizeZ;
		this.mapCellSize = mapCellSize;
		this.scale = scale;
		this.seed = seed;
		this.octaves = octaves;
		this.persistance = persistance;
		this.smoothingFactor = smoothingFactor;

		prng = new System.Random(seed);
	}

	public override float[,] CreateHeightMap() {
		float[,] heightMap = CreatePerlinNoise();
		return heightMap;
	}

	private float[,] CreatePerlinNoise() {
		float[,] map = new float[mapCellsX + 1, mapCellsZ + 1];

		Vector2[] octaveOffsets = new Vector2[octaves];

		for (int i = 0; i < octaves; ++i) {
			float offsetX = prng.Next(-100000, 100000);
			float offsetZ = prng.Next(-100000, 100000);
			octaveOffsets[i] = new Vector2(offsetX, offsetZ);
		}

		float maxValue = float.MinValue;
		float minValue = float.MaxValue;

		float halfX = mapCellsX / 2f;
		float halfZ = mapCellsZ / 2f;

		for (int z = 0; z < mapCellsZ + 1; ++z) {
			for (int x = 0; x < mapCellsX + 1; ++x) {
				float amplitude = 1;
				float frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < octaves; ++i) {
					float sampleZ = (z - halfZ) / scale * frequency + octaveOffsets[i].y;
					float sampleX = (x - halfX) / scale * frequency + octaveOffsets[i].x;

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
		for (int z = 0; z < mapCellsZ + 1; ++z) {
			for (int x = 0; x < mapCellsX + 1; ++x) {
				map[x, z] = Mathf.InverseLerp(minValue, maxValue, map[x, z]);
			}
		}

		return map;
	}
}