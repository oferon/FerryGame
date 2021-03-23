using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GotCoin : MonoBehaviour {

	public AudioSource snd;
	private GameManager _manager; 
	// Use this for initialization
	void Start () 
	{
		_manager = GetComponentInParent <GameManager> ();

	}


	private void OnTriggerEnter(Collider col)
	{
		Debug.Log("Please use this module legally by purchasing this 3rd party asset [Fancy Animated Coins] at the Unity Asset Store");
		snd.Play();
		this.gameObject.SetActive (false);
		_manager.ScoreUp();


	}
}
