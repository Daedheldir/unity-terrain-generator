using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpatialSubdivision : GenerationMethodBase
{
	private float minValue = float.MaxValue;
	private float maxValue = float.MinValue;

	public SpatialSubdivision(GenerationSettings settings, int seed, float scaleOverride) : base(settings, seed, scaleOverride)
	{
	}

	private float[,] genInputArr()
	{
		float[,] input = new float[2, 2];
		for (int z = 0; z < 2; ++z)
		{
			for (int x = 0; x < 2; ++x)
			{
				input[z, x] = (float)prng.NextDouble() * settings.scale * settings.ChunkSize;
			}
		}

		return input;
	}

	private void UpdateMinMaxValues(float value)
	{
		if (value > maxValue)
			maxValue = value;
		else if (value < minValue)
			minValue = value;
	}

	private float[,] genMap(float[,] previousMap, int step, float amplitude)
	{
		if (previousMap.Length >= (settings.ChunkSize) * (settings.ChunkSize))
		{
			return previousMap;
		}

		//divide each tile in two <=> create new map two times bigger
		float[,] newMap = new float[previousMap.GetLength(0) * 2, previousMap.GetLength(1) * 2];

		for (int z = 0; z < previousMap.GetLength(0); ++z)
		{
			for (int x = 0; x < previousMap.GetLength(1); ++x)
			{
				newMap[z * 2, x * 2] = previousMap[z, x];
			}
		}

		//average all new values
		//find values for empty points diagonally
		for (int z = 1; z < newMap.GetLength(0); z += 2)
		{
			for (int x = 1; x < newMap.GetLength(1); x += 2)
			{
				newMap[z, x] = averageDiagonal(newMap, z, x);

				//update min and max values
				UpdateMinMaxValues(newMap[z, x]);
			}
		}

		//find values for empty points orthogonally
		for (int z = 0; z < newMap.GetLength(0); ++z)
		{
			for (int x = (z + 1) % 2; x < newMap.GetLength(1); x += 2)
			{
				newMap[z, x] = averageOrthogonal(newMap, z, x);

				//update min and max values
				UpdateMinMaxValues(newMap[z, x]);
			}
		}

		//find new values
		for (int z = 0; z < newMap.GetLength(0); z += 2)
		{
			for (int x = 1; x < newMap.GetLength(1); x += 2)
			{
				newMap[z, x] += (float)prng.NextDouble() * amplitude * settings.scale * settings.ChunkSize;

				//update min and max values
				UpdateMinMaxValues(newMap[z, x]);
			}
		}

		for (int z = 1; z < newMap.GetLength(0); z += 2)
		{
			for (int x = 0; x < newMap.GetLength(1); ++x)
			{
				newMap[z, x] += (float)prng.NextDouble() * amplitude * settings.scale * settings.ChunkSize;

				//update min and max values
				UpdateMinMaxValues(newMap[z, x]);
			}
		}

		//Debug.Log("Loop counter after second loop " + loopCounter);
		amplitude *= settings.persistance;
		return genMap(newMap, step + 1, amplitude);
	}

	private float averageOrthogonal(float[,] map, int z, int x)
	{
		//create average of nearby points
		float average = 0;

		//x underflow handling
		if (x - 1 < 0)
			average += map[z, map.GetLength(1) - 1];
		else
			average += map[z, x - 1];

		//x overflow handling
		if (x + 1 >= map.GetLength(1))
			average += map[z, 0];
		else
			average += map[z, x + 1];

		//y underflow handling
		if (z - 1 < 0)
			average += map[map.GetLength(0) - 1, x];
		else
			average += map[z - 1, x];

		//y overflow handling
		if (z + 1 >= map.GetLength(0))
			average += map[0, x];
		else
			average += map[z + 1, x];

		return average / 4f;
	}

	private float averageDiagonal(float[,] map, int z, int x)
	{
		//create average of nearby points
		float average = 0;

		average += map[z - 1, x - 1];

		//y overflow handling
		if (z + 1 >= map.GetLength(0))
		{
			average += map[0, x - 1];

			//x overflow handling
			if (x + 1 >= map.GetLength(1))
				average += map[0, 0];
			else
				average += map[0, x + 1];
		}
		else
		{
			average += map[z + 1, x - 1];

			//x overflow handling
			if (x + 1 >= map.GetLength(1))
				average += map[z + 1, 0];
			else
				average += map[z + 1, x + 1];
		}

		if (x + 1 >= map.GetLength(1))
			average += map[z - 1, 0];
		else
			average += map[z - 1, x + 1];

		return average / 4f;
	}

	public override float[,] CreateHeightMap(Vector2 generationOffset)
	{
		float amplitude = 1f / settings.smoothing + 0.01f;
		return dh.Math.NormalizeWithEasing(genMap(genInputArr(), 1, amplitude), minValue, maxValue);
	}

	public override float EvaluateHeight(Vector2 point)
	{
		throw new System.NotImplementedException();
	}

	public override float EvaluateHeight(Vector2 point, Vector2[] octaveOffsets, int startingIndex, int endingIndex, float maskValue = 0)
	{
		throw new System.NotImplementedException();
	}
}