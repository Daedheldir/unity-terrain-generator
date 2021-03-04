using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public static class Tests
{
	private enum tableColumns
	{
		Name,
		AvgTime,
		AvgMemory,
	}

	public static void PlayGenerationTests()
	{
		//table format
		//	Name		Avg.Time	Avg.Memory
		//	method_1
		//	method_2
		int iterations = 100;

		int[] octaveCounts = { 1, 2, 4, 8 };

		int tableColumnsCount = 2 * octaveCounts.Length + 1;
		int generationMethodsCount = Enum.GetNames(typeof(GenerationSettings.GenerationMethodType)).Length;

		string[,] tables = new string[generationMethodsCount + 1, tableColumnsCount];
		tables[0, 0] = "Method Name";

		for (int tableIndex = 1, i = 0; i < octaveCounts.Length; ++i)
		{
			tables[0, tableIndex++] = "Avg. T " + octaveCounts[i] + " octave" + (octaveCounts[i] > 1 ? "s" : "");
			tables[0, tableIndex++] = "Avg. Mem " + octaveCounts[i] + " octave" + (octaveCounts[i] > 1 ? "s" : "");
		}

		Thread thread = new Thread(new ThreadStart(delegate
		{
			Debug.Log("Starting Tests");
			for (int type = 0; type < generationMethodsCount; ++type)
			{
				for (int octavesIndex = 0; octavesIndex < octaveCounts.Length; ++octavesIndex)
				{
					int tableColumn = octavesIndex * 2 + 1;

					GenerationSettings generationSettings = new GenerationSettings(
						(GenerationSettings.GenerationMethodType)type, octaveCounts[octavesIndex], 5, 1, 0.5f, 0.5f, 241);

					IGenerationMethod generationMethod = TerrainGenerator.GetGenerationMethod(generationSettings, 0);

					float timeSum = 0;
					long memorySum = 0;

					Debug.Log("Testing method " + Enum.GetNames(typeof(GenerationSettings.GenerationMethodType))[type] + " octaves = " + octaveCounts[octavesIndex]);

					for (int i = 0; i < iterations; ++i)
					{
						//collect garbage before starting and get memory use
						var memoryUseAtStart = System.GC.GetTotalMemory(true);
						var stopwatch = System.Diagnostics.Stopwatch.StartNew();

						float[,] map = generationMethod.CreateHeightMap(new Vector2(0, 0));

						stopwatch.Stop();
						timeSum += stopwatch.ElapsedMilliseconds;

						memorySum += System.GC.GetTotalMemory(false) - memoryUseAtStart;
					}

					float avgTime = (timeSum / iterations); //time in ms
					long memoryUse = (memorySum) / iterations;

					//write name
					tables[type + 1, 0] = generationSettings.methodName;

					//write time
					tables[type + 1, tableColumn++] = Math.Round(avgTime, 1).ToString() + "ms";

					//write memory
					int counter = 0;
					float memoryUseShortened = memoryUse;
					string[] shortNotation = { "B", "KB", "MB" };
					while (memoryUseShortened > 999.0f)
					{
						memoryUseShortened /= 1024f;
						counter++;
					}
					string memoryUseInMB = Math.Round(memoryUseShortened, 1).ToString();
					tables[type + 1, tableColumn++] = memoryUseInMB + shortNotation[counter];
				}
			}

			HeightMapSaver.SaveTexTable(tables, "tests_table", "Testy - " + iterations + " iteracji");

			Debug.Log("Tests Finished");
		}));
		thread.Start();
	}
}