using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoronoiDiagrams : GenerationMethodBase
{
	public int numberOfVoronoiPoints = 5;
	private bool pointsCreated = false;
	private List<List<Vector2>> points;

	public VoronoiDiagrams(GenerationSettings settings, int seed, float scaleOverride) : base(settings, seed, scaleOverride)
	{
		this.numberOfVoronoiPoints = (int)(settings.Scale > 50 ? 50 : settings.Scale);
	}

	public override float EvaluateHeight(Vector2 point, Vector2[] octaveOffsets, int startingIndex, int endingIndex, float maskValue = 0)
	{
		float noiseHeight = 0f;

		float halfChunkSize = settings.ChunkSize / 2f;

		//if starting index is 0 use frequency of 1
		float amplitude = (startingIndex > 0) ? (1 * (settings.persistance * startingIndex)) : 1;
		float frequency = (startingIndex > 0) ? (1 / (settings.smoothing * startingIndex)) : 1;

		//creating random points
		if (!pointsCreated)
		{
			points = new List<List<Vector2>>();

			for (int j = 0; j < settings.octaves - startingIndex; ++j)
			{
				points.Add(new List<Vector2>());    //add a new octave points list
				float numberOfPoints = numberOfVoronoiPoints * frequency < 512 ? numberOfVoronoiPoints * frequency : 128;
				for (int i = 0; i < numberOfPoints; ++i)
				{
					points[j].Add(
						new Vector2(
							prng.Next((int)(-settings.ChunkSize), (int)(settings.ChunkSize)),

							prng.Next((int)(-settings.ChunkSize), (int)(settings.ChunkSize))
							)
						);
				}

				//after each octave, next octave should have more points so i has more details
				frequency /= settings.smoothing;
			}

			pointsCreated = true;
		}

		//iterate through all octaves
		for (int j = startingIndex; j < endingIndex; ++j)
		{
			float minDistance = float.MaxValue;
			float maxDistance = float.MinValue;

			//find closest point to current map cell
			for (int i = 0; i < points[j].Count; ++i)
			{
				float currentDistance = 0;
				currentDistance = Vector2.Distance(point, points[j][i]);
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
			noiseHeight += EvaluateHeight(new Vector2(minDistance, maxDistance)) * amplitude;

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