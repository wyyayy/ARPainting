using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.iOS;

namespace ARSDK
{
    public class ARVirtualMouse : MonoBehaviour
    {
        public Transform DebugModel;

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
                Debug.Log("MousePos: " + Input.mousePosition);

				VirtualMouseData mouseData = _session.GetVirtualMouseData();
                var screenPos = new Vector3(Screen.width - mouseData.screenY, Screen.height - mouseData.screenX, 0);

				Debug.Log("ScreenPos: " + screenPos);
                Debug.Log("JoyStickData, error:" + mouseData.success + ", size: " + mouseData.size + ", screenX: " + mouseData.screenX + ", screenY: " + mouseData.screenY);

                if(DebugModel != null)
                {
                    var ray = Camera.current.ScreenPointToRay(screenPos);
                    float distance = (mouseData.size / 17.0f) / 100f;
                    var pos = ray.GetPoint(distance);
                    DebugModel.position = pos;
                }
			}
        }
    }

}

