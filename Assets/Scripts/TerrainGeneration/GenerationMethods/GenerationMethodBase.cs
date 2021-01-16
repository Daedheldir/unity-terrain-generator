using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerationMethodBase
{
	protected GenerationSettings settings;
	protected System.Random prng;
	protected Vector2 generationOffset;

	protected GenerationMethodBase(GenerationSettings settings, Vector2 generationOffset, int seed)
	{
		this.settings = settings;
		this.generationOffset = generationOffset;
		prng = new System.Random(seed);
	}
}