using UnityEngine;
using System.Collections;

public class MarkDestroyOnStart : MonoBehaviour {

	// Use this for initialization
    protected void Start()
    {
        GameObject.Destroy(transform.gameObject);
	}
	
}
