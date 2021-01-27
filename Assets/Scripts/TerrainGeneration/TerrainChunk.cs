using UnityEngine;

public class TerrainChunk
{
	private GameObject meshObject;
	private Vector2 position;

	private Bounds bounds;

	public TerrainChunk(Vector2 coord, int size)
	{
		position = coord * size;
		Vector3 position3D = new Vector3(position.x, 0, position.y);

		meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
		meshObject.transform.position = position3D;

		meshObject.transform.localScale = Vector3.one * size / 10f;
	}
}