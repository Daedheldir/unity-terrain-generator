using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGenerationMethod
{
	float EvaluateHeight(Vector2 point);

	float EvaluateHeight(Vector2 point, Vector2[] octaveOffsets, int startingIndex, int endingIndex, float maskValue = 0);

	float[,] CreateHeightMap();
}