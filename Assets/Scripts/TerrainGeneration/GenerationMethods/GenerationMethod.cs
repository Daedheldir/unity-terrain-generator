using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GenerationMethod
{
	public int mapCellsX = 255;
	public int mapCellsZ = 255;

	public float mapCellSize = 1;

	public float smoothingFactor = 0.1f;

	public int seed = 0;

	public System.Random prng = new System.Random();

	public abstract float[,] CreateHeightMap();
}