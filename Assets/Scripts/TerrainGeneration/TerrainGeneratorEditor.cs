using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
	public override void OnInspectorGUI()
	{
		TerrainGenerator terrainGenerator = (TerrainGenerator)target;
		GUILayout.Label("Generation Settings");

		if (DrawDefaultInspector())
		{
			if (terrainGenerator.autoUpdateMap)
			{
				terrainGenerator.GeneratePreviewMap();
			}
		}

		if (GUILayout.Button("Generate"))
		{
			terrainGenerator.GeneratePreviewMap();
		}
		if (GUILayout.Button("Clear Mesh"))
		{
			terrainGenerator.ClearMap();
		}
	}
}