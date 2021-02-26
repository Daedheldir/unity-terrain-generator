using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cosine : GenerationMethodBase
{
	public Cosine(GenerationSettings generationSettings, int seed) : base(generationSettings, seed)
	{
	}

	public override float EvaluateHeight(Vector2 point, Vector2[] octaveOffsets, int startingIndex, int endingIndex, float maskValue = 0)
	{
		Vector2 sample;

		//if starting index is 0 use frequency of 1
		float amplitude = (startingIndex > 0) ? (1 * (settings.persistance * startingIndex)) : 1;
		float frequency = (startingIndex > 0) ? (1 / (settings.smoothing * startingIndex)) : 1;

		float noiseHeight = 0;

		for (int i = startingIndex; i < endingIndex; ++i)
		{
			sample = EvaluateSamplePoint(point.x, point.y, octaveOffsets[i], frequency);

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
		float arg = (Mathf.PerlinNoise(point.x, point.y) * 2f - 1);
		float val = 1 - Mathf.Abs(Mathf.Cos(arg));
		return val * 2;
	}
}