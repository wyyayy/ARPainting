using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ARSDK;

public class AppMgr : MonoBehaviour
{
	protected UdpReceiver _udpReceiver;
    protected ARVirtualMouse _arVirtualMouse;

    // Use this for initialization
    void Start()
    {
        ///...No effect, does ARKit plugin modify the frameRate at somewhere?
        Application.targetFrameRate = 30;
        QualitySettings.vSyncCount = 0;

		this.Bind(out _udpReceiver);
		_udpReceiver.StartReceive(23456);

		_udpReceiver.OnMessage = msg=>
		{
			Debug.LogWarning("Received Msg: " + msg);
		};

        this.Bind(out _arVirtualMouse);
    }

    // Update is called once per frame
    void Update()
    {
		if(Input.GetMouseButtonDown(0))
        {
            _arVirtualMouse.enabled = !_arVirtualMouse.enabled;
        }
    }
}
