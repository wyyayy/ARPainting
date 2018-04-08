using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.iOS;

namespace ARSDK
{
    public class ARVirtualMouse : MonoBehaviour
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
				VirtualMouseData data = _session.GetVirtualMouseData();
				Debug.Log("JoyStickData, error:" + data.success + ", size: " + data.size + ", screenX: " + data.screenX + ", screenY: " + data.screenY);				
			}
        }
    }

}

