using UnityEngine;
using System.Collections;

#pragma warning disable 0109, 0108
#pragma warning disable 1692 

public class ColorPickerTester : MonoBehaviour 
{
    public Renderer renderer;
    public ColorPicker picker; 

	// Use this for initialization
	void Start () 
    {
        picker.onValueChanged.AddListener(color =>
        {
            renderer.material.color = color;
        });
		renderer.material.color = picker.CurrentColor;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
