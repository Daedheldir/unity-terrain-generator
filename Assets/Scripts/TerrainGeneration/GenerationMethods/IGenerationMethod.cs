using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGenerationMethod
{
	float EvaluateHeight(Vector3 point);

	float[,] CreateHeightMap();
}