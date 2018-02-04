using UnityEngine;
using System.Collections;

public class MarkInvisibleOnStart : MonoBehaviour {

	// Use this for initialization
    protected void Start() 
    {
        gameObject.GetComponent<Renderer>().enabled = false;
	}
	
}
