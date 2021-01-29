using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoronoiDiagrams : GenerationMethodBase
{
	public int numberOfVoronoiPoints = 50;

	public VoronoiDiagrams(GenerationSettings settings, int seed) : base(settings, seed)
	{
		this.numberOfVoronoiPoints = (int)(settings.scale > 25 ? 25 : settings.scale);
	}

	public override float EvaluateHeight(Vector2 point, Vector2[] octaveOffsets, int startingIndex, int endingIndex, float maskValue = 0)
	{
		Vector2 sample;

		float noiseHeight = 0f;

		float halfChunkSize = settings.ChunkSize / 2f;

		//if starting index is 0 use frequency of 1
		float amplitude = (startingIndex > 0) ? (1 * (settings.persistance * startingIndex)) : 1;
		float frequency = (startingIndex > 0) ? (1 / (settings.smoothing * startingIndex)) : 1;

		//creating random points
		List<List<Vector2>> points = new List<List<Vector2>>();

		for (int j = 0; j < settings.octaves - startingIndex; ++j)
		{
			points.Add(new List<Vector2>());    //add a new octave points list

			for (int i = 0; i < numberOfVoronoiPoints * frequency; ++i)
			{
				points[j].Add(
					new Vector2(
						prng.Next((int)(octaveOffsets[j].x - halfChunkSize), (int)(halfChunkSize + octaveOffsets[j].x)),

						prng.Next((int)(octaveOffsets[j].y - halfChunkSize), (int)(halfChunkSize + octaveOffsets[j].y))
						)
					);
			}

			//after each octave, next octave should have more points so i has more details
			frequency /= settings.smoothing;
		}

		//iterate through all octaves
		for (int j = startingIndex; j < endingIndex; ++j)
		{
			sample = EvaluateSamplePoint(point.x, point.y, octaveOffsets[j], frequency);

			float minDistance = float.MaxValue;
			float maxDistance = float.MinValue;

			//find closest point to current map cell
			for (int i = 0; i < points[j].Count; ++i)
			{
				float currentDistance = Vector2.Distance(sample, points[j][i]);

				if (currentDistance < minDistance)
				{
					minDistance = currentDistance;
				}
				if (currentDistance > maxDistance)
				{
					maxDistance = currentDistance;
				}
			}

			//change height depending on distance from closest point
			noiseHeight += EvaluateHeight(point) * amplitude;

			//updating frequency and amplitude for next octave
			amplitude *= settings.persistance;
		}

		return noiseHeight;
	}

	public override float EvaluateHeight(Vector2 minMaxDistance)
	{
		return dh.Math.CosineInterpolate(0, 1f, minMaxDistance.x / minMaxDistance.y);
	}
}