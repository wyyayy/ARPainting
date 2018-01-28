using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppMgr : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Application.targetFrameRate = 30;
		QualitySettings.vSyncCount = 2;
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
