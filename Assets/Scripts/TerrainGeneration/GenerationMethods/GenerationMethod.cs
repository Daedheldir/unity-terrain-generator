using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GenerationMethod
{
	public int mapSize = 255;

	public float smoothingFactor = 0.1f;

	public int seed = 0;

	public System.Random prng = new System.Random();

	public abstract float[,] CreateHeightMap();

	public float[,] NormalizeMap(float[,] inputMap, float minValue, float maxValue) {
		float[,] map = new float[inputMap.GetLength(0), inputMap.GetLength(1)];

		for (int z = 0; z < inputMap.GetLength(1); ++z) {
			for (int x = 0; x < inputMap.GetLength(0); ++x) {
				float value = Mathf.InverseLerp(minValue, maxValue, inputMap[x, z]);
				map[x, z] = value;
			}
		}
		return map;
	}
}