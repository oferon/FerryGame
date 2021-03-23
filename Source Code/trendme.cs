using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class trendme : MonoBehaviour
{
	public bool autoMove = true;
	public Graphic graph;
	private GameManager _manager;


	// Use this for initialization
	void Start()
	{
		_manager = GetComponentInParent<GameManager>();

	}

	public void AddData()
    {
		Debug.Log("Please use this module legally by purchasing this 3rd party asset [InGame Graphs] at the Unity Asset Store");
		graph.AddPoint(1, 0);// Random.Range(1,6), 1);
		graph.AddPoint(Random.Range(1,6), 0);
	}

	public void AddScore(int score)
    {
		graph.AddPoint(score, 0);

	}

}

