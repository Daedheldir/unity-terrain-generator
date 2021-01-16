using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoronoiDiagrams : GenerationMethodBase, IGenerationMethod
{
	public int numberOfVoronoiPoints = 50;

	private List<List<Vector2>> points = new List<List<Vector2>>();

	public VoronoiDiagrams(GenerationSettings settings, Vector2 generationOffset, int seed) : base(settings, generationOffset, seed)
	{
		this.numberOfVoronoiPoints = (int)settings.scale;
	}

	public float[,] CreateHeightMap()
	{
		return CreateVoronoiGraph(new Vector2(generationOffset.x, generationOffset.y));
	}

	public float EvaluateHeight(Vector2 point)
	{
		throw new System.NotImplementedException();
	}

	private float[,] CreateVoronoiGraph(Vector2 offset)
	{
		float[,] map = new float[settings.chunkSize, settings.chunkSize];

		float frequency = 1f;

		//clamping voronoiSmoothing
		if (settings.smoothing <= 0f)
			settings.smoothing = 0.00001f;

		//creating random points
		for (int j = 0; j < settings.octaves; ++j)
		{
			points.Add(new List<Vector2>());    //add a new octave points list
			for (int i = 0; i < numberOfVoronoiPoints * frequency; ++i)
			{
				points[j].Add(
					new Vector2(
						prng.Next((int)offset.x, (int)(settings.chunkSize + offset.x)),
						prng.Next((int)offset.y, (int)(settings.chunkSize + offset.y))
						)
					);
			}

			//after each octave, next octave should have more points so i has more details
			frequency /= settings.smoothing;
		}

		float maxValue = float.MinValue;
		float minValue = float.MaxValue;

		for (int z = 0; z < settings.chunkSize; ++z)
		{
			for (int x = 0; x < settings.chunkSize; ++x)
			{
				float minDistance = float.MaxValue;
				float maxDistance = float.MinValue;

				float amplitude = 1f;
				float noiseHeight = 0f;

				Vector2 mapPoint = new Vector2(z, x);

				//iterate through all octaves
				for (int j = 0; j < settings.octaves; ++j)
				{
					//find closest point to current map cell
					for (int i = 0; i < points[j].Count; ++i)
					{
						float currentDistance = Vector2.Distance(mapPoint, points[j][i]);

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
					noiseHeight += dh.Math.CosineInterpolate(0, 1f, minDistance / maxDistance) * amplitude;

					//updating frequency and amplitude for next octave
					amplitude *= settings.persistance;
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

		//wait for access to resources
		return map;
	}
}