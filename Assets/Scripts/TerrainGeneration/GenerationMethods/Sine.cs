using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sine : GenerationMethodBase, IGenerationMethod
{
	public Sine(GenerationSettings generationSettings, Vector2 generationOffset, int seed) : base(generationSettings, generationOffset, seed)
	{
	}

	public float[,] CreateHeightMap()
	{
		float[,] map = new float[settings.chunkSize, settings.chunkSize];
		float[,] mask = new float[settings.chunkSize, settings.chunkSize];

		Vector2[] octaveOffsets = new Vector2[settings.octaves];

		for (int i = 0; i < settings.octaves; ++i)
		{
			float offsetX = generationOffset.x + prng.Next(-100000, 100000);
			float offsetZ = generationOffset.y + prng.Next(-100000, 100000);
			octaveOffsets[i] = new Vector2(offsetX, offsetZ);
		}

		float maxValue = float.MinValue;
		float minValue = float.MaxValue;

		//creating first octave
		for (int z = 0; z < settings.chunkSize; ++z)
		{
			for (int x = 0; x < settings.chunkSize; ++x)
			{
				float value = EvaluateHeight(new Vector2(x, z), octaveOffsets, 0, 1);

				map[x,z] = mask[x, z] = value;
			}
		}

		//creating rest of octaves
		for (int z = 0; z < settings.chunkSize; ++z)
		{
			for (int x = 0; x < settings.chunkSize; ++x)
			{
				float value = EvaluateHeight(new Vector2(x, z), octaveOffsets, 1, octaveOffsets.Length, mask[x, z]);

				map[x, z] += value;

				if (map[x, z] > maxValue)
					maxValue = map[x, z];
				else if (map[x, z] < minValue)
					minValue = map[x, z];
			}
		}

		//normalizing values to be between 0-1
		map = dh.Math.NormalizeMap(map, minValue, maxValue);

		return map;
	}

	public float EvaluateHeight(Vector2 point, Vector2[] octaveOffsets, int startingIndex, int endingIndex, float maskValue = 0)
	{
		Vector2 sample = new Vector2();
		float halfChunkSize = settings.chunkSize / 2f;

		//if starting index is 0 use frequency of 1
		float amplitude = (startingIndex > 0) ? (1 * (settings.persistance * startingIndex)) : 1;
		float frequency = (startingIndex > 0) ? (1 / (settings.smoothing * startingIndex)) : 1;

		float noiseHeight = 0;

		for (int i = startingIndex; i < endingIndex; ++i)
		{
			sample.y = ((point.y - halfChunkSize) / settings.scale) * frequency + octaveOffsets[i].y;
			sample.x = ((point.x - halfChunkSize) / settings.scale) * frequency + octaveOffsets[i].x;

			float val = EvaluateHeight(sample);

			//if its first octave and use it as mask
			if (i != 0 && settings.useFirstOctaveAsMask)
			{
				val *= maskValue;
			}

			val *= amplitude;

			noiseHeight += val;

			amplitude *= settings.persistance;
			frequency /= settings.smoothing;
		}

		return noiseHeight;
	}

	public float EvaluateHeight(Vector2 point)
	{
		float sinArg = (1f - Mathf.PerlinNoise(point.x, point.y) * 2f);
		float val = 1 - Mathf.Abs(Mathf.Sin(sinArg));
		return val * val;
	}
}