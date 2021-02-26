using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GenerationMethodBase : IGenerationMethod
{
	protected GenerationSettings settings;
	protected System.Random prng;
	private Vector2[] randomOffsets;

	protected GenerationMethodBase(GenerationSettings settings, int seed)
	{
		this.settings = settings;
		prng = new System.Random(seed);

		//creating octave offsets
		randomOffsets = new Vector2[settings.octaves];

		for (int i = 0; i < settings.octaves; ++i)
		{
			float offsetX = prng.Next(-100000, 100000);
			float offsetZ = prng.Next(-100000, 100000);
			randomOffsets[i] = new Vector2(offsetX, offsetZ);
		}
	}

	public virtual float[,] CreateHeightMap(Vector2 generationOffset)
	{
		Vector2[] octaveOffsets = new Vector2[settings.octaves];
		for (int i = 0; i < settings.octaves; ++i)
		{
			octaveOffsets[i] = randomOffsets[i] + generationOffset;
		}

		float[,] map = new float[settings.ChunkSize, settings.ChunkSize];
		float[,] mask = new float[settings.ChunkSize, settings.ChunkSize];

		float maxValue = float.MinValue;
		float minValue = float.MaxValue;

		//creating first octave
		for (int z = 0; z < settings.ChunkSize; ++z)
		{
			for (int x = 0; x < settings.ChunkSize; ++x)
			{
				float value = EvaluateHeight(new Vector2(x, z), octaveOffsets, 0, 1);

				map[x, z] = mask[x, z] = value;
			}
		}

		//creating rest of octaves
		for (int z = 0; z < settings.ChunkSize; ++z)
		{
			for (int x = 0; x < settings.ChunkSize; ++x)
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
		//map = dh.Math.NormalizeMap(map, minValue, maxValue);
		//wait for access to resources
		return map;
	}

	protected Vector2 EvaluateSamplePoint(float x, float z, Vector2 octaveOffset, float frequency)
	{
		Vector2 sample = new Vector2();
		sample.y = ((z + octaveOffset.y - settings.ChunkSize / 2f) / settings.Scale) * frequency;
		sample.x = ((x + octaveOffset.x - settings.ChunkSize / 2f) / settings.Scale) * frequency;

		return sample;
	}

	public abstract float EvaluateHeight(Vector2 point);

	public abstract float EvaluateHeight(Vector2 point, Vector2[] octaveOffsets, int startingIndex, int endingIndex, float maskValue = 0);
}