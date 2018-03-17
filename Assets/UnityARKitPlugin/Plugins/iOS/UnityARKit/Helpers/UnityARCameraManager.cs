using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class UnityARCameraManager : MonoBehaviour
{
    public Camera Camera;

    [Header("AR Config Options")]
    public UnityARAlignment StartAlignment = UnityARAlignment.UnityARAlignmentGravity;
    public UnityARPlaneDetection PlaneDetection = UnityARPlaneDetection.Horizontal;
    public bool GetPointCloud = true;
    public bool EnableLightEstimation = true;

    private UnityARSessionNativeInterface _session;


    private Material __savedClearMaterial;

    // Use this for initialization
    void Start()
    {
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

        if (Camera == null)
        {
            Camera = Camera.main;
        }
    }

    public void SetCamera(Camera newCamera)
    {
        if (Camera != null)
        {
            UnityARVideo oldARVideo = Camera.gameObject.GetComponent<UnityARVideo>();
            if (oldARVideo != null)
            {
                __savedClearMaterial = oldARVideo.m_ClearMaterial;
                Destroy(oldARVideo);
            }
        }
        SetupNewCamera(newCamera);
    }

    private void SetupNewCamera(Camera newCamera)
    {
        Camera = newCamera;

        if (Camera != null)
        {
            UnityARVideo unityARVideo = Camera.gameObject.GetComponent<UnityARVideo>();
            if (unityARVideo != null)
            {
                __savedClearMaterial = unityARVideo.m_ClearMaterial;
                Destroy(unityARVideo);
            }
            unityARVideo = Camera.gameObject.AddComponent<UnityARVideo>();
            unityARVideo.m_ClearMaterial = __savedClearMaterial;
        }
    }

    // Update is called once per frame

    void Update()
    {
        if (Camera != null)
        {
            // JUST WORKS!
            Matrix4x4 matrix = _session.GetCameraPose();
            Camera.transform.localPosition = UnityARMatrixOps.GetPosition(matrix);
            Camera.transform.localRotation = UnityARMatrixOps.GetRotation(matrix);

            Camera.projectionMatrix = _session.GetCameraProjection();
        }

    }

}
