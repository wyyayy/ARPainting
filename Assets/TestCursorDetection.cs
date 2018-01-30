using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCursorDetection : MonoBehaviour 
{
	public Texture2D Texture;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(Input.GetKeyDown(KeyCode.D))
		{
			var pixels = Texture.GetPixels32();
			Debug.Log(pixels.Length);			
			Debug.Log(Texture.width * Texture.height);

			var pos = _detect(pixels);
			Debug.Log(pos);
		}		
	}

	Vector2 _detect(Color32[] pixels)
	{
		return new Vector2(0, 0);
	}
}
