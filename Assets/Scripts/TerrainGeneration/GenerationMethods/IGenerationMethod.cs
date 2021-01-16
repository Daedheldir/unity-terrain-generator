using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGenerationMethod
{
	float EvaluateHeight(Vector2 point);

	float[,] CreateHeightMap();
}