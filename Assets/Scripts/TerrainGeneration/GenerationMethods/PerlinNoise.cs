using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[System.Serializable]
public class PerlinNoise : GenerationMethodBase, IGenerationMethod
{
	public PerlinNoise(GenerationSettings settings, Vector2 generationOffset, int seed) : base(settings, generationOffset, seed)
	{
	}

	public float[,] CreateHeightMap()
	{
		float[,] map = new float[settings.chunkSize, settings.chunkSize];

		Vector2[] octaveOffsets = new Vector2[settings.octaves];

		for (int i = 0; i < settings.octaves; ++i)
		{
			float offsetX = generationOffset.x + prng.Next(-100000, 100000);
			float offsetZ = generationOffset.y + prng.Next(-100000, 100000);
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
				float noiseHeight = EvaluateHeight(x, z, octaveOffsets);
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

	public float EvaluateHeight(Vector2 point, Vector2[] octaveOffsets)
	{
		float amplitude = 1;
		float frequency = 1;
		float noiseHeight = 0;

		Vector2 sample = new Vector2();

		for (int i = 0; i < settings.octaves; ++i)
		{
			sample.y = (point.y) / settings.scale * frequency + octaveOffsets[i].y;
			sample.x = (point.x) / settings.scale * frequency + octaveOffsets[i].x;

			float perlinValue = EvaluateHeight(sample);

			noiseHeight += perlinValue * amplitude;

			amplitude *= settings.persistance;
			frequency /= settings.smoothing;
		}

		return noiseHeight;
	}

	public float EvaluateHeight(float x, float z, Vector2[] octaveOffsets)
	{
		return EvaluateHeight(new Vector2(x, z), octaveOffsets);
	}

	public float EvaluateHeight(Vector2 point)
	{
		return Mathf.PerlinNoise(point.x, point.y);
	}
}