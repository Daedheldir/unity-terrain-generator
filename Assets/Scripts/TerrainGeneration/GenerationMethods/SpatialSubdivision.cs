using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialSubdivision : GenerationMethod
{
	public SpatialSubdivision(int mapSizeX, int mapSizeZ, int seed, float mapCellSize, float smoothingFactor)
	{
		this.mapCellsX = mapSizeX;
		this.mapCellsZ = mapSizeZ;
		this.seed = seed;
		this.mapCellSize = mapCellSize;
		this.smoothingFactor = smoothingFactor;

		prng = new System.Random(seed);
	}

	private float[,] genInputArr()
	{
		float[,] input = new float[2, 2];
		for (int z = 0; z < 2; ++z)
		{
			for (int x = 0; x < 2; ++x)
			{
				input[z, x] = prng.Next(-100000, 100000)/200000;
			}
		}

		return input;
	}

	private float[,] genNoiseMap()
	{
		float[,] noiseMap = new float[mapCellsZ, mapCellsX];
		for (int z = 0; z < mapCellsZ; ++z)
		{
			for (int x = 0; x < mapCellsX; ++x)
			{
				noiseMap[z, x] = (float)prng.NextDouble();
			}
		}
		return noiseMap;
	}

	private float[,] genMap(float[,] previousMap, int step)
	{
		if (previousMap.Length >= (1 + mapCellsX) * (1 + mapCellsZ))
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
		int loopCounter = 0;
		//find values for empty points diagonally
		for (int z = 1; z < newMap.GetLength(0); z += 2)
		{
			for (int x = 1; x < newMap.GetLength(1); x += 2)
			{ 
				newMap[z, x] = averageDiagonal(newMap, z, x) + Random.Range((-smoothingFactor) / (step + 0.1f), (smoothingFactor) / (step + 0.1f));
				loopCounter++;
			}
		}
		Debug.Log("Loop counter after first loop " + loopCounter);
		loopCounter = 0;
		//find values for empty points orthogonally
		for (int z = 0; z < newMap.GetLength(0); ++z)
		{
			for (int x = (z + 1) % 2; x < newMap.GetLength(1); x += 2)
			{
				newMap[z, x] = averageOrthogonal(newMap, z, x) + Random.Range((-smoothingFactor) / (step + 0.1f), (smoothingFactor) / (step + 0.1f));
				loopCounter++;
			}
		}
		Debug.Log("Loop counter after second loop " + loopCounter);


		return genMap(newMap, step + 1);
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

	private float averageDiagonal(float[,] map, int y, int x)
	{
		//create average of nearby points
		float average = 0;

		average += map[y - 1, x - 1];

		//y overflow handling
		if (y + 1 >= map.GetLength(0))
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
			average += map[y + 1, x - 1];

			//x overflow handling
			if (x + 1 >= map.GetLength(1))
				average += map[y + 1, 0];
			else
				average += map[y + 1, x + 1];
		}

		if (x + 1 >= map.GetLength(1))
			average += map[y - 1, 0];
		else
			average += map[y - 1, x + 1];

		return average / 4f;
	}

	private float[,] normalizeMap(float [,] map)
	{
		float maxVal = float.MinValue;
		float minVal = float.MaxValue;

		foreach (float val in map)
		{
			if (val > maxVal)
				maxVal = val;
			else if (val < minVal)
				minVal = val;
		}

		for(int z = 0; z < map.GetLength(0); ++z)
		{
			for (int x = 0; x < map.GetLength(1); ++x)
			{
				map[z,x] = (map[z, x] - minVal);
			}
		}

		return map;
	}

	public override float[,] CreateHeightMap()
	{
		return (genMap(genInputArr(), 0));
	}
}