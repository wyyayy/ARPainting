using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

using UnityEngine.XR.iOS;

namespace ARSDK
{
    public class ARVideo : MonoBehaviour
    {
        public Material ClearMaterial;

        private CommandBuffer _commandBuffer;

        private Texture2D _videoTextureY;
        private Texture2D _videoTextureCbCr;

		private Matrix4x4 _displayTransform;

		private UnityARSessionNativeInterface _session;

		/// Update near/far 
		private Camera _targetCamera;
		private float _currentNearZ;
		private float _currentFarZ;

		/// Grab video pixels
		private bool _initialized;

		private byte[] _textureYBytes;
		private byte[] _textureUVBytes;
		private GCHandle _pinnedYArray;
		private GCHandle _pinnedUVArray;

		public void Start()
		{
			_session = UnityARSessionNativeInterface.GetARSessionNativeInterface ();
			UnityARSessionNativeInterface.ARFrameUpdatedEvent += UpdateFrame;
			_targetCamera = GetComponent<Camera> ();
			_updateCameraClipPlanes ();
		}

		void _updateCameraClipPlanes()
		{
			_currentNearZ = _targetCamera.nearClipPlane;
			_currentFarZ = _targetCamera.farClipPlane;

			_session.SetCameraClipPlanes (_currentNearZ, _currentFarZ);
		}

		void UpdateFrame(UnityARCamera camera)
		{
			if (_currentNearZ != _targetCamera.nearClipPlane || _currentFarZ != _targetCamera.farClipPlane) 
			{
				_updateCameraClipPlanes();
			}

			if (!_initialized) 
			{
				_initialize(camera);
			}

			_videoTextureY.LoadRawTextureData(_textureYBytes);
			_videoTextureY.Apply ();
			_videoTextureCbCr.LoadRawTextureData(_textureUVBytes);
			_videoTextureCbCr.Apply ();			
			
			_displayTransform = new Matrix4x4();
			_displayTransform.SetColumn(0, camera.displayTransform.column0);
			_displayTransform.SetColumn(1, camera.displayTransform.column1);
			_displayTransform.SetColumn(2, camera.displayTransform.column2);
			_displayTransform.SetColumn(3, camera.displayTransform.column3);

			ClearMaterial.SetMatrix("_DisplayTransform", _displayTransform);

		}

		void _initialize(UnityARCamera camera)
		{
			int numYBytes = camera.videoParams.yWidth * camera.videoParams.yHeight;
			int numUVBytes = camera.videoParams.yWidth * camera.videoParams.yHeight / 2; //quarter resolution, but two bytes per pixel
			
			_textureYBytes = new byte[numYBytes];
			_textureUVBytes = new byte[numUVBytes];
			_pinnedYArray = GCHandle.Alloc (_textureYBytes);
			_pinnedUVArray = GCHandle.Alloc (_textureUVBytes);
			_initialized = true;

			IntPtr yBytes = _pinByteArray(ref _pinnedYArray, _textureYBytes);
			IntPtr uvBytes = _pinByteArray(ref _pinnedUVArray, _textureUVBytes);
			_session.SetVideoPixelBuffer (yBytes, uvBytes);

			int yWidth = camera.videoParams.yWidth;
			int yHeight = camera.videoParams.yHeight;
			int uvWidth = yWidth / 2;
			int uvHeight = yHeight / 2;

			_videoTextureY = new Texture2D (yWidth, yHeight, TextureFormat.R8, false, true);
			_videoTextureCbCr = new Texture2D (uvWidth, uvHeight, TextureFormat.RG16, false, true);

			ClearMaterial.SetTexture("_textureY", _videoTextureY);
			ClearMaterial.SetTexture("_textureCbCr", _videoTextureCbCr);

			InitializeCommandBuffer ();
		}

		void InitializeCommandBuffer()
		{
			_commandBuffer = new CommandBuffer(); 

			/// Source is Y and CbCr texture
			_commandBuffer.Blit(null, BuiltinRenderTextureType.CurrentActive, ClearMaterial);

			GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);

		}

		IntPtr _pinByteArray(ref GCHandle handle, byte[] array)
		{
			handle.Free ();
			handle = GCHandle.Alloc (array, GCHandleType.Pinned);
			return handle.AddrOfPinnedObject ();
		}

		void OnDestroy()
		{
			GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
			UnityARSessionNativeInterface.ARFrameUpdatedEvent -= UpdateFrame;

			_pinnedYArray.Free();
			_pinnedUVArray.Free();

			_session.SetVideoPixelBuffer(IntPtr.Zero, IntPtr.Zero);

			if(null != _commandBuffer) _commandBuffer.Dispose();
		}

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
		}
 
    }
}
