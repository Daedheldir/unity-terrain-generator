using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ActorController : MonoBehaviour
{
	public Camera camera;
	public NavMeshAgent agent;

	// Start is called before the first frame update
	private void Start()
	{
		//agent.Warp(new Vector3(12.47f, 13.67f, 8.662781f));
	}

	// Update is called once per frame
	private void Update()
	{
		if (Input.GetMouseButton(0))
		{
			Ray ray = camera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;

			if (Physics.Raycast(ray, out hit))
			{
				agent.SetDestination(hit.point);
			}
		}
	}
}