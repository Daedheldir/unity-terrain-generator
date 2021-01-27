using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sine : GenerationMethodBase
{
	public Sine(GenerationSettings generationSettings, Vector2 generationOffset, int seed) : base(generationSettings, generationOffset, seed)
	{
	}

	public override float EvaluateHeight(Vector2 point, Vector2[] octaveOffsets, int startingIndex, int endingIndex, float maskValue = 0)
	{
		Vector2 sample = new Vector2();
		float halfChunkSize = settings.ChunkSize / 2f;

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

	public override float EvaluateHeight(Vector2 point)
	{
		float sinArg = (1f - Mathf.PerlinNoise(point.x, point.y) * 2f);
		float val = 1 - Mathf.Abs(Mathf.Sin(sinArg));
		return val;
	}
}