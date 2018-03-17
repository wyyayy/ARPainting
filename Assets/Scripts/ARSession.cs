using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.iOS;

namespace ARSDK
{
    public class ARSession : MonoBehaviour
    {
        [Header("AR Config Options")]
        public UnityARAlignment StartAlignment = UnityARAlignment.UnityARAlignmentGravity;
        public UnityARPlaneDetection PlaneDetection = UnityARPlaneDetection.Horizontal;
        public bool GetPointCloud = true;
        public bool EnableLightEstimation = true;

        private UnityARSessionNativeInterface _session;

        protected Camera _camera;

        // Use this for initialization
        void Start()
        {
            this.Bind(out _camera);

            _session = UnityARSessionNativeInterface.GetARSessionNativeInterface();

            Application.targetFrameRate = 60;
            ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration();
            config.planeDetection = PlaneDetection;
            config.alignment = StartAlignment;
            config.getPointCloudData = GetPointCloud;
            config.enableLightEstimation = EnableLightEstimation;

            if (config.IsSupported)
            {
                _session.RunWithConfig(config);
            }
            else
            {
                Debug.LogWarning("Session type not supported!");
            }

        }

        // Update is called once per frame
        void Update()
        {
            Debug.Assert(_camera != null);

            // JUST WORKS!
            Matrix4x4 matrix = _session.GetCameraPose();
            _camera.transform.localPosition = UnityARMatrixOps.GetPosition(matrix);
            _camera.transform.localRotation = UnityARMatrixOps.GetRotation(matrix);

            ///...need update every frame?
            _camera.projectionMatrix = _session.GetCameraProjection();
        }
    }

}
