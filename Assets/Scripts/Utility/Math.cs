using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dh
{
	public static class Math
	{
		/// <summary>
		/// Used when max and min values are known
		/// </summary>
		/// <param name="inputMap"></param>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		/// <returns></returns>

		public static float[,] NormalizeMap(float[,] inputMap, float minValue, float maxValue)
		{
			float[,] map = new float[inputMap.GetLength(0), inputMap.GetLength(1)];

			for (int z = 0; z < inputMap.GetLength(1); ++z)
			{
				for (int x = 0; x < inputMap.GetLength(0); ++x)
				{
					float value = Mathf.InverseLerp(minValue, maxValue, inputMap[x, z]);
					Debug.Assert(value >= 0f && value <= 1f, "Value doesn't fit within bounds! Value = " + value);
					map[x, z] = value;
				}
			}
			return map;
		}
		public static float[,] NormalizeMap(float[,] inputMap)
		{
			float maxValue = float.MinValue;
			float minValue = float.MaxValue;

			for (int z = 0; z < inputMap.GetLength(1); ++z)
			{
				for (int x = 0; x < inputMap.GetLength(0); ++x)
				{
					if (inputMap[x, z] > maxValue)
						maxValue = inputMap[x, z];
					else if (inputMap[x, z] < minValue)
						minValue = inputMap[x, z];
				}
			}

			return NormalizeMap(inputMap, minValue, maxValue);
		}

		public static float[,] NormalizeWithEasing(float[,] inputMap, float minValue, float maxValue)
		{
			float[,] map = new float[inputMap.GetLength(0), inputMap.GetLength(1)];

			for (int z = 0; z < inputMap.GetLength(1); ++z)
			{
				for (int x = 0; x < inputMap.GetLength(0); ++x)
				{
					float value = Mathf.InverseLerp(minValue, maxValue, inputMap[x, z]);
					Debug.Assert(value >= 0f && value <= 1f, "Value doesn't fit within bounds! Value = " + value);
					map[x, z] = easingFunc(value);
				}
			}
			return map;
		}
		private static float easingFunc(float input)
		{
			if (input > 1f || input < 0f)
			{
				return input;
			}
			return 6 * Mathf.Pow(input, 5) - 15 * Mathf.Pow(input, 4) + 10 * Mathf.Pow(input, 3);
		}

		public static float CosineInterpolate(float min, float max, float ang)
		{
			float angle;

			angle = (1f - Mathf.Cos(ang * Mathf.PI)) / 2f;
			return (min * (1f - angle) + max * angle);
		}
	}
}