using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GenerationSettings
{
	public enum GenerationMethodType
	{
		SpatialSubdivision,
		PerlinNoise,
		RidgedPerlinNoise,
		Voronoi,
		Sine
	}

	public GenerationSettings(GenerationMethodType methodType, int octaves, float scale, float weight, float persistance, float smoothing, int chunkSize)
	{
		this.methodType = methodType;

		this.octaves = octaves;
		this.scale = scale;
		this.weight = weight;
		this.persistance = persistance;
		this.smoothing = smoothing;

		this.ChunkSize = chunkSize;
	}

	public bool isActive = true;
	public bool useFirstOctaveAsMask = false;

	public GenerationMethodType methodType;

	public int octaves;
	public float scale;
	public float weight;
	public float persistance;

	[Range(0.01f, 1)]
	public float smoothing;

	private int chunkSize = 241;

	public int ChunkSize { get => chunkSize; set => chunkSize = value; }
}