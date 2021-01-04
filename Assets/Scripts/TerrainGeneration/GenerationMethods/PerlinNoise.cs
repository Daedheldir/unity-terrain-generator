using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[System.Serializable]
public class PerlinNoise : IGenerationMethod
{
	private GenerationSettings settings;
	private System.Random prng;

	public PerlinNoise
		(
		GenerationSettings settings, int seed
		)
	{
		this.settings = settings;
		prng = new System.Random(seed);
	}

	public float[,] CreateHeightMap()
	{
		float[,] map = new float[settings.chunkSize, settings.chunkSize];

		Vector2[] octaveOffsets = new Vector2[settings.octaves];

		for (int i = 0; i < settings.octaves; ++i)
		{
			float offsetX = prng.Next(-100000, 100000);
			float offsetZ = prng.Next(-100000, 100000);
			octaveOffsets[i] = new Vector2(offsetX, offsetZ);
		}

		float maxValue = float.MinValue;
		float minValue = float.MaxValue;

		float halfX = settings.chunkSize / 2f;
		float halfZ = settings.chunkSize / 2f;
		for (int z = 0; z < settings.chunkSize; ++z)
		{
			for (int x = 0; x < settings.chunkSize; ++x)
			{
				float amplitude = 1;
				float frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < settings.octaves; ++i)
				{
					float sampleZ = (z - halfZ) / settings.scale * frequency + octaveOffsets[i].y;
					float sampleX = (x - halfX) / settings.scale * frequency + octaveOffsets[i].x;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);

					noiseHeight += perlinValue * amplitude;

					amplitude *= settings.persistance;
					frequency *= settings.smoothing;
				}

				if (noiseHeight > maxValue)
					maxValue = noiseHeight;
				else if (noiseHeight < minValue)
					minValue = noiseHeight;

				map[x, z] = noiseHeight;
			}
		}

		//normalizing values to be between 0-1
		map = dh.Math.NormalizeMap(map, minValue, maxValue);

		return map;
	}

	public float EvaluateHeight(Vector3 point, Vector2[] octaveOffsets)
	{

		float amplitude = 1;
		float frequency = 1;
		float noiseHeight = 0;

		for (int i = 0; i < settings.octaves; ++i)
		{
			float sampleZ = (point.z) / settings.scale * frequency + octaveOffsets[i].y;
			float sampleX = (point.x) / settings.scale * frequency + octaveOffsets[i].x;

			float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);

			noiseHeight += perlinValue * amplitude;

			amplitude *= settings.persistance;
			frequency *= settings.smoothing;
		}

		return noiseHeight;
	}

	public float EvaluateHeight(float x, float y, float z, Vector2[] octaveOffsets)
	{

		float amplitude = 1;
		float frequency = 1;
		float noiseHeight = 0;

		for (int i = 0; i < settings.octaves; ++i)
		{
			float sampleZ = (z) / settings.scale * frequency + octaveOffsets[i].y;
			float sampleX = (x) / settings.scale * frequency + octaveOffsets[i].x;

			float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);

			noiseHeight += perlinValue * amplitude;

			amplitude *= settings.persistance;
			frequency *= settings.smoothing;
		}

		return noiseHeight;
	}

	public float EvaluateHeight(Vector3 point)
	{
		throw new System.NotImplementedException();
	}
}