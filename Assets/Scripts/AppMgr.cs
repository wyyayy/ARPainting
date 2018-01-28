using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppMgr : MonoBehaviour {

	// Use this for initialization
	void Start () {
		///...No effect, does ARKit plugin modify the frameRate at somewhere?
		///...
		Application.targetFrameRate = 30;
		QualitySettings.vSyncCount = 0;
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
