using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.iOS;

namespace ARSDK
{
    public class ARJoyStick : MonoBehaviour
    {
        protected UnityARSessionNativeInterface _session;
		
        // Use this for initialization
        void Start()
        {
			_session = UnityARSessionNativeInterface.GetARSessionNativeInterface ();
        }

        // Update is called once per frame
        void Update()
        {
			if(Input.GetMouseButtonDown(0))
			{
				ARJoyStickData data = _session.GetARJoyStickData();
				Debug.Log("JoyStickData, error:" + data.error + ", size: " + data.size + ", screenX: " + data.screenX + ", screenY: " + data.screenY);				
			}
        }
    }

}

