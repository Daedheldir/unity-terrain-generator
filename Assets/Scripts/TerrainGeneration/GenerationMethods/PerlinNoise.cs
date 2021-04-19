using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[System.Serializable]
public class PerlinNoise : GenerationMethodBase
{
	public PerlinNoise(GenerationSettings settings, int seed, float scaleOverride) : base(settings, seed, scaleOverride)
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

			float perlinValue = EvaluateHeight(sample);

			//if its not the first octave and use it as mask
			if (i != 0 && settings.useFirstOctaveAsMask)
			{
				perlinValue *= maskValue;
			}

			noiseHeight += perlinValue * amplitude;

			amplitude *= settings.persistance;
			frequency /= settings.smoothing;
		}

		return noiseHeight;
	}

	public override float EvaluateHeight(Vector2 point)
	{
		float val = Mathf.PerlinNoise(point.x, point.y);
		return val;
	}
}