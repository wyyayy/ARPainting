using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.XR.iOS
{
    public class ARVideo : MonoBehaviour
    {
        public Material ClearMaterial;

        private CommandBuffer _commandBuffer;
        private Texture2D _videoTextureY;
        private Texture2D _videoTextureCbCr;
		private Matrix4x4 _displayTransform;

		private bool _bCommandBufferInited;

		private UnityARSessionNativeInterface m_Session;

		/// Update near/far 
		private Camera _targetCamera;
		private float _currentNearZ;
		private float _currentFarZ;

		/// Grab video pixels
		private bool bTexturesInitialized;
		private int currentFrameIndex;
		private byte[] m_textureYBytes;
		private byte[] m_textureUVBytes;
		private byte[] m_textureYBytes2;
		private byte[] m_textureUVBytes2;
		private GCHandle m_pinnedYArray;
		private GCHandle m_pinnedUVArray;

		public void Start()
		{
			UnityARSessionNativeInterface.ARFrameUpdatedEvent += UpdateFrame;
			_bCommandBufferInited = false;

			_targetCamera = GetComponent<Camera> ();
			_updateCameraClipPlanes ();
		}

		void _updateCameraClipPlanes()
		{
			_currentNearZ = _targetCamera.nearClipPlane;
			_currentFarZ = _targetCamera.farClipPlane;

			UnityARSessionNativeInterface.GetARSessionNativeInterface ().SetCameraClipPlanes (_currentNearZ, _currentFarZ);
		}

		void UpdateFrame(UnityARCamera camera)
		{
			if (_currentNearZ != _targetCamera.nearClipPlane || _currentFarZ != _targetCamera.farClipPlane) 
			{
				_updateCameraClipPlanes ();
			}

			if (!bTexturesInitialized) 
			{
				InitializeTextures (camera);
			}

			_displayTransform = new Matrix4x4();
			_displayTransform.SetColumn(0, camera.displayTransform.column0);
			_displayTransform.SetColumn(1, camera.displayTransform.column1);
			_displayTransform.SetColumn(2, camera.displayTransform.column2);
			_displayTransform.SetColumn(3, camera.displayTransform.column3);		
		}

		void InitializeTextures(UnityARCamera camera)
		{
			int numYBytes = camera.videoParams.yWidth * camera.videoParams.yHeight;
			int numUVBytes = camera.videoParams.yWidth * camera.videoParams.yHeight / 2; //quarter resolution, but two bytes per pixel
			
			m_textureYBytes = new byte[numYBytes];
			m_textureUVBytes = new byte[numUVBytes];
			m_textureYBytes2 = new byte[numYBytes];
			m_textureUVBytes2 = new byte[numUVBytes];
			m_pinnedYArray = GCHandle.Alloc (m_textureYBytes);
			m_pinnedUVArray = GCHandle.Alloc (m_textureUVBytes);
			bTexturesInitialized = true;
		}

		void InitializeCommandBuffer()
		{
			_commandBuffer = new CommandBuffer(); 
			_commandBuffer.Blit(null, BuiltinRenderTextureType.CurrentActive, ClearMaterial);
			GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
			_bCommandBufferInited = true;

		}


		IntPtr PinByteArray(ref GCHandle handle, byte[] array)
		{
			handle.Free ();
			handle = GCHandle.Alloc (array, GCHandleType.Pinned);
			return handle.AddrOfPinnedObject ();
		}

		byte [] ByteArrayForFrame(int frame,  byte[] array0,  byte[] array1)
		{
			return frame == 1 ? array1 : array0;
		}

		byte [] YByteArrayForFrame(int frame)
		{
			return ByteArrayForFrame (frame, m_textureYBytes, m_textureYBytes2);
		}

		byte [] UVByteArrayForFrame(int frame)
		{
			return ByteArrayForFrame (frame, m_textureUVBytes, m_textureUVBytes2);
		}

		void OnDestroy()
		{
			GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
			UnityARSessionNativeInterface.ARFrameUpdatedEvent -= UpdateFrame;
			_bCommandBufferInited = false;

			m_Session.SetVideoPixelBuffer(IntPtr.Zero, IntPtr.Zero);

			m_pinnedYArray.Free ();
			m_pinnedUVArray.Free ();			
		}

#if !UNITY_EDITOR

        public void OnPreRender()
        {
			ARTextureHandles handles = UnityARSessionNativeInterface.GetARSessionNativeInterface ().GetARVideoTextureHandles();
            if (handles.textureY == System.IntPtr.Zero || handles.textureCbCr == System.IntPtr.Zero)
            {
                return;
            }

            if (!bCommandBufferInitialized) {
                InitializeCommandBuffer ();
            }

            Resolution currentResolution = Screen.currentResolution;

            // Texture Y
            if (_videoTextureY == null) {
              _videoTextureY = Texture2D.CreateExternalTexture(currentResolution.width, currentResolution.height,
                  TextureFormat.R8, false, false, (System.IntPtr)handles.textureY);
              _videoTextureY.filterMode = FilterMode.Bilinear;
              _videoTextureY.wrapMode = TextureWrapMode.Repeat;
              m_ClearMaterial.SetTexture("_textureY", _videoTextureY);
            }

            // Texture CbCr
            if (_videoTextureCbCr == null) {
              _videoTextureCbCr = Texture2D.CreateExternalTexture(currentResolution.width, currentResolution.height,
                  TextureFormat.RG16, false, false, (System.IntPtr)handles.textureCbCr);
              _videoTextureCbCr.filterMode = FilterMode.Bilinear;
              _videoTextureCbCr.wrapMode = TextureWrapMode.Repeat;
              m_ClearMaterial.SetTexture("_textureCbCr", _videoTextureCbCr);
            }

            _videoTextureY.UpdateExternalTexture(handles.textureY);
            _videoTextureCbCr.UpdateExternalTexture(handles.textureCbCr);

			m_ClearMaterial.SetMatrix("_DisplayTransform", _displayTransform);
        }
#else

		public void SetYTexure(Texture2D YTex)
		{
			_videoTextureY = YTex;
		}

		public void SetUVTexure(Texture2D UVTex)
		{
			_videoTextureCbCr = UVTex;
		}

		public void OnPreRender()
		{

			if (!_bCommandBufferInited) {
				InitializeCommandBuffer ();
			}

			ClearMaterial.SetTexture("_textureY", _videoTextureY);
			ClearMaterial.SetTexture("_textureCbCr", _videoTextureCbCr);

			ClearMaterial.SetMatrix("_DisplayTransform", _displayTransform);
		}
 
#endif
    }
}
