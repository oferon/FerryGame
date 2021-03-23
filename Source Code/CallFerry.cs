using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallFerry : MonoBehaviour {


	private GameManager _manager; 


	// Use this for initialization
	void Start () 
	{
		_manager = GetComponentInParent <GameManager> ();

	}




	private void OnTriggerEnter(Collider col)
	{
		_manager.MoveFerry();

	}

}
