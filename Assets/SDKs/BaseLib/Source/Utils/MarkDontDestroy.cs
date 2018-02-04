using UnityEngine;
using System.Collections;

public class MarkDontDestroy : MonoBehaviour {

	// Use this for initialization
    protected void Start()
    {
		Object.DontDestroyOnLoad(transform);
	}
	
	// Update is called once per frame
    protected void Update()
    {
        Destroy(this);
	}
}
