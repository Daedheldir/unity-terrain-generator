using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
	public override void OnInspectorGUI() {
		TerrainGenerator terrainGenerator = (TerrainGenerator)target;

		if (DrawDefaultInspector()) {
			if (terrainGenerator.autoUpdateMap) {
				terrainGenerator.GenerateMap();
			}
		}

		if (GUILayout.Button("Generate")) {
			terrainGenerator.GenerateMap();
		}
	}
}