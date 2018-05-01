using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMousePosition : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetMouseButton(0))
		{
			Debug.Log(Input.mousePosition);
			Debug.Log("Width: " + Screen.width);
			Debug.Log("Height: " + Screen.height);
		}
	}
}
