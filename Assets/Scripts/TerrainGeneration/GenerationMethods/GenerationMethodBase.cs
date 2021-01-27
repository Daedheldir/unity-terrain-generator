using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GenerationMethodBase : IGenerationMethod
{
	protected GenerationSettings settings;
	protected System.Random prng;
	protected Vector2 generationOffset;

	protected GenerationMethodBase(GenerationSettings settings, Vector2 generationOffset, int seed)
	{
		this.settings = settings;
		this.generationOffset = generationOffset;
		prng = new System.Random(seed);
	}

	public virtual float[,] CreateHeightMap()
	{
		float[,] map = new float[settings.ChunkSize, settings.ChunkSize];
		float[,] mask = new float[settings.ChunkSize, settings.ChunkSize];

		//creating octave offsets
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
		map = dh.Math.NormalizeMap(map, minValue, maxValue);

		//wait for access to resources
		return map;
	}

	public abstract float EvaluateHeight(Vector2 point);

	public abstract float EvaluateHeight(Vector2 point, Vector2[] octaveOffsets, int startingIndex, int endingIndex, float maskValue = 0);
}