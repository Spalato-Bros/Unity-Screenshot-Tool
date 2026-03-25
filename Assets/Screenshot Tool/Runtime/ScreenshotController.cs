using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpalatoBros.ScreenshotTool
{
    public class ScreenshotController : MonoBehaviour
    {
		public static ScreenshotController Instance { get; private set; }

		[Header("Raw screen capture settings")]
		[SerializeField] private ScreenshotConfig rawScreenshotConfig1x;
		[SerializeField] private ScreenshotConfig rawScreenshotConfig2x;
		[SerializeField] private ScreenshotConfig rawScreenshotConfig4x;

		private string screenshotsPath;
		public string ScreenshotsPath => screenshotsPath;

		[Header("Raw screen capture as texture settings")]
		[SerializeField] private ScreenshotConfig rawScreenshotAsTextureConfig1x;
		[SerializeField] private ScreenshotConfig rawScreenshotAsTextureConfig2x;
		[SerializeField] private ScreenshotConfig rawScreenshotAsTextureConfig4x;
		[Space]
		[SerializeField] private Texture2D rawScreenshotTex;
		[SerializeField] private Texture2D screenshotTex;
		[Tooltip("Use the Compress method when creating the resulting texture to reduce file size.")]
		public bool compressScreenshotAsTexture;
		public bool compressScreenshotAsTextureHighQuality;
		private bool isSaving;
		public bool SavingScreenshotAsTexture => isSaving;
		private byte[] gammaLUT;
		private byte[] linearLUT;

		[Header("Individual camera rendering")]
		[SerializeField] private ScreenshotConfig renderIndividualCamerasConfig1x;
		[SerializeField] private ScreenshotConfig renderIndividualCamerasConfigFullHD;
		[SerializeField] private ScreenshotConfig renderIndividualCamerasConfigUltraHD;
		[SerializeField] private ScreenshotConfig renderIndividualCamerasConfigCustom;
		[SerializeField] private ScreenshotConfig saveCameraRendersConfig1x;

		[SerializeField] private List<Camera> cameraList;

		[SerializeField] private List<Texture2D> renderedCameraTextures;

		[SerializeField] private Vector2Int customCameraResolution = new(1920, 1080);
		public Vector2Int CustomCameraResolution => customCameraResolution;

		[SerializeField] private RenderTextureFormat defaultRenderTextureFormat = RenderTextureFormat.ARGB32;
		[SerializeField] private TextureFormat defaultTextureFormat = TextureFormat.ARGB32;
		[SerializeField] private bool linearTexture;

		// Events.
		public event Action<string> OnScreenshotTaken;
		public event Action<Texture2D> OnRawScreenshotAsTextureTaken;
		public event Action<Texture2D> OnScreenshotAsTextureTaken;
		public event Action<string> OnScreenshotAsTextureSaved;
		public event Action<Exception> OnSaveScreenshotAsTextureError;
		public event Action<List<Texture2D>> OnCamerasRenderedToTexture;

		private void Awake()
		{
			Instance = this;

			AddListeners();

			// For converting textures that are too bright or dark to compensate.
			CreateLinearLUT();
			CreateGammaLUT();

			// Prepare individual camera list.
			cameraList = new();
			renderedCameraTextures = new();

			// For saving files to directory.
			screenshotsPath = Path.Combine(Application.persistentDataPath, "Screenshots");
			EnsureScreenshotDirectoryExists();
		}

		private void AddListeners()
		{
			rawScreenshotConfig1x.screenshotAction.performed += TakeRawScreenshotAction1xPerformed;
			rawScreenshotConfig1x.screenshotAction.Enable();

			rawScreenshotConfig2x.screenshotAction.performed += TakeRawScreenshotAction2xPerformed;
			rawScreenshotConfig2x.screenshotAction.Enable();

			rawScreenshotConfig4x.screenshotAction.performed += TakeRawScreenshotAction4xPerformed;
			rawScreenshotConfig4x.screenshotAction.Enable();

			rawScreenshotAsTextureConfig1x.screenshotAction.performed += TakeRawScreenshotAsTextureAction1xPerformed;
			rawScreenshotAsTextureConfig1x.screenshotAction.Enable();

			rawScreenshotAsTextureConfig2x.screenshotAction.performed += TakeRawScreenshotAsTextureAction2xPerformed;
			rawScreenshotAsTextureConfig2x.screenshotAction.Enable();

			rawScreenshotAsTextureConfig4x.screenshotAction.performed += TakeRawScreenshotAsTextureAction4xPerformed;
			rawScreenshotAsTextureConfig4x.screenshotAction.Enable();

			renderIndividualCamerasConfig1x.screenshotAction.performed += RenderIndividualCamera1xPerformed;
			renderIndividualCamerasConfig1x.screenshotAction.Enable();

			renderIndividualCamerasConfigFullHD.screenshotAction.performed += RenderIndividualCameraFullHDPerformed;
			renderIndividualCamerasConfigFullHD.screenshotAction.Enable();

			renderIndividualCamerasConfigUltraHD.screenshotAction.performed += RenderIndividualCameraUltraHDPerformed;
			renderIndividualCamerasConfigUltraHD.screenshotAction.Enable();

			renderIndividualCamerasConfigCustom.screenshotAction.performed += RenderIndividualCameraCustomPerformed;
			renderIndividualCamerasConfigCustom.screenshotAction.Enable();

			saveCameraRendersConfig1x.screenshotAction.performed += SaveCameraRenders1xPerformed;
			saveCameraRendersConfig1x.screenshotAction.Enable();
		}

		private void EnsureScreenshotDirectoryExists()
		{
			if (Directory.Exists(screenshotsPath)) return;
			Directory.CreateDirectory(screenshotsPath);
		}

		#region Raw screenshot events
		private void TakeRawScreenshotAction1xPerformed(InputAction.CallbackContext context)
		{
			TakeRawScreenshot(1);
		}

		private void TakeRawScreenshotAction2xPerformed(InputAction.CallbackContext context)
		{
			TakeRawScreenshot(2);
		}

		private void TakeRawScreenshotAction4xPerformed(InputAction.CallbackContext context)
		{
			TakeRawScreenshot(4);
		}

		public void TakeRawScreenshot(int superSize)
		{
			EnsureScreenshotDirectoryExists();

			string fileName = GetFileName("Screenshot", Screen.width, Screen.height, superSize, "png");
			string fullPath = Path.Combine(screenshotsPath, fileName);
			ScreenCapture.CaptureScreenshot(fullPath, superSize);
			OnScreenshotTaken?.Invoke(fileName);

			Debug.Log($"Raw screenshot captured: ({Screen.width * superSize}x{Screen.height * superSize}): {fileName}");
		}
		#endregion

		#region Raw screenshots to texture events
		private void TakeRawScreenshotAsTextureAction1xPerformed(InputAction.CallbackContext context)
		{
			StartCoroutine(TakeRawScreenshotAsTexture(1));
		}

		private void TakeRawScreenshotAsTextureAction2xPerformed(InputAction.CallbackContext context)
		{
			StartCoroutine(TakeRawScreenshotAsTexture(2));
		}

		private void TakeRawScreenshotAsTextureAction4xPerformed(InputAction.CallbackContext context)
		{
			StartCoroutine(TakeRawScreenshotAsTexture(4));
		}

		public IEnumerator TakeRawScreenshotAsTexture(int superSize)
		{
			if (rawScreenshotTex)
				Destroy(rawScreenshotTex);

			if (screenshotTex)
				Destroy(screenshotTex);

			yield return new WaitForEndOfFrame();

			rawScreenshotTex = ScreenCapture.CaptureScreenshotAsTexture(superSize);
			rawScreenshotTex.name = GetFileName("Screenshot", Screen.width, Screen.height, superSize, string.Empty);
			OnRawScreenshotAsTextureTaken?.Invoke(rawScreenshotTex);

			screenshotTex = new(rawScreenshotTex.width, rawScreenshotTex.height, TextureFormat.RGBA32, false);
			screenshotTex.SetPixels32(rawScreenshotTex.GetPixels32());

			if (compressScreenshotAsTexture)
				screenshotTex.Compress(compressScreenshotAsTextureHighQuality);

			screenshotTex.Apply();
			screenshotTex.name = rawScreenshotTex.name;
			OnScreenshotAsTextureTaken?.Invoke(screenshotTex);
			Debug.Log($"Screenshot as texture taken: {Screen.width * superSize}x{Screen.height * superSize}");
		}

		public async void SaveScreenshotAsTextureToFile(string fileExtension)
		{
			if (!screenshotTex) return;
			if (isSaving) return;
			isSaving = true;

			string fullPath = Path.Combine(screenshotsPath, screenshotTex.name + $".{fileExtension}");

			await SaveToFile(screenshotTex, fullPath, fileExtension, OnScreenshotAsTextureSaved);
		}
		#endregion

		#region Render camera events
		private void RenderIndividualCamera1xPerformed(InputAction.CallbackContext context)
		{
			RenderAllCameras();
		}

		private void RenderIndividualCameraFullHDPerformed(InputAction.CallbackContext context)
		{
			RenderAllCamerasAtFullHD();
		}

		private void RenderIndividualCameraUltraHDPerformed(InputAction.CallbackContext context)
		{
			RenderAllCamerasAtUltraHD();
		}

		private void RenderIndividualCameraCustomPerformed(InputAction.CallbackContext context)
		{
			RenderAllCamerasAtCustomResolution();
		}

		private void SaveCameraRenders1xPerformed(InputAction.CallbackContext context)
		{
			SaveCameraRendersToFile("png");
		}

		public void AddCamera(Camera newCam)
		{
			if (cameraList.Contains(newCam)) return;
			cameraList.Add(newCam);
			UpdateCameraList();
		}

		public void RemoveCamera(Camera cam)
		{
			if (!cameraList.Contains(cam)) return;
			cameraList.Remove(cam);
			UpdateCameraList();
		}

		private void UpdateCameraList()
		{
			for (int i = cameraList.Count - 1; i > 0; --i)
			{
				if (cameraList[i]) continue;
				cameraList.RemoveAt(i);
			}
		}

		[ContextMenu("Render All Cameras")]
		public void RenderAllCameras()
		{
			StartCoroutine(RenderCameras());
		}

		private IEnumerator RenderCameras()
		{
			ClearAllRenders();

			yield return new WaitForEndOfFrame();

			UpdateCameraList();

			for (int i = 0; i < cameraList.Count; i++)
				RenderCameraAtOwnResolution(cameraList[i]);

			OnCamerasRenderedToTexture?.Invoke(renderedCameraTextures);
		}

		public void RenderAllCamerasAtFullHD()
		{
			StartCoroutine(RenderCamerasFullHD());
		}

		private IEnumerator RenderCamerasFullHD()
		{
			ClearAllRenders();

			yield return new WaitForEndOfFrame();

			UpdateCameraList();

			for (int i = 0; i < cameraList.Count; i++)
				RenderCamera(cameraList[i], 1920, 1080);

			OnCamerasRenderedToTexture?.Invoke(renderedCameraTextures);
		}

		public void RenderAllCamerasAtUltraHD()
		{
			StartCoroutine(RenderCamerasUltraHD());
		}

		private IEnumerator RenderCamerasUltraHD()
		{
			ClearAllRenders();

			yield return new WaitForEndOfFrame();

			UpdateCameraList();

			for (int i = 0; i < cameraList.Count; i++)
				RenderCamera(cameraList[i], 3840, 2160);

			OnCamerasRenderedToTexture?.Invoke(renderedCameraTextures);
		}

		public void RenderAllCamerasAtCustomResolution()
		{
			StartCoroutine(RenderCamerasCustomResolution());
		}

		private IEnumerator RenderCamerasCustomResolution()
		{
			ClearAllRenders();

			yield return new WaitForEndOfFrame();

			UpdateCameraList();

			for (int i = 0; i < cameraList.Count; i++)
				RenderCamera(cameraList[i], customCameraResolution.x, customCameraResolution.y);

			OnCamerasRenderedToTexture?.Invoke(renderedCameraTextures);
		}

		private void ClearAllRenders()
		{
			for (int i = 0; i < renderedCameraTextures.Count; i++)
			{
				if (!renderedCameraTextures[i]) continue;
				Destroy(renderedCameraTextures[i]);
			}

			if (renderedCameraTextures.Count > 0)
				renderedCameraTextures.Clear();
		}

		private void RenderCameraAtOwnResolution(Camera cam)
		{
			int width = cam.scaledPixelWidth;
			int height = cam.scaledPixelHeight;

			RenderCamera(cam, width, height);
		}

		public void RenderCamera(Camera cam, int width, int height)
		{
			RenderTexture originalRT = cam.targetTexture;
			Rect originalRect = cam.rect;

			RenderTexture rt = new(width, height, 24, defaultRenderTextureFormat);
			rt.Create();

			// Normalize so it fills the RT
			cam.rect = new Rect(0f, 0f, 1f, 1f);
			cam.targetTexture = rt;

			cam.Render();

			RenderTexture currentRT = RenderTexture.active;
			RenderTexture.active = rt;

			Texture2D tex = new(width, height, defaultTextureFormat, linearTexture);
			tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			tex.Apply();

			tex.name = $"Camera_Render_{cam.transform.name}_{GetFileName(string.Empty, width, height, 1)}";

			if (!renderedCameraTextures.Contains(tex))
				renderedCameraTextures.Add(tex);

			// Restore
			RenderTexture.active = currentRT;
			cam.targetTexture = originalRT;
			cam.rect = originalRect;

			Destroy(rt);
		}

		public async void SaveCameraRendersToFile(string fileExtension = "png")
		{
			if (isSaving) return;
			isSaving = true;

			for (int i = 0; i < renderedCameraTextures.Count; i++)
			{
				if (!renderedCameraTextures[i]) continue;
				
				if (!renderedCameraTextures[i].isReadable)
				{
					Debug.LogWarning($"Rendered camera texture: {renderedCameraTextures[i].name} ({i}) is nor readable, skipping save.");
					continue;
				}

				if (renderedCameraTextures[i].name.EndsWith($".{fileExtension}"))
					renderedCameraTextures[i].name = renderedCameraTextures[i].name.Substring(0, renderedCameraTextures[i].name.Length - 4);

				string fullPath = Path.Combine(screenshotsPath, renderedCameraTextures[i].name + $".{fileExtension}");

				await SaveToFile(renderedCameraTextures[i], fullPath, fileExtension, null);
			}

			isSaving = false;
		}
		#endregion

		#region HDR/Render Texture conversions
		private void CreateGammaLUT()
		{
			gammaLUT = new byte[256];

			for (int i = 0; i < 256; i++)
			{
				float v = i / 255f;
				v = Mathf.LinearToGammaSpace(v);
				gammaLUT[i] = (byte)(v * 255f);
			}
		}

		private void CreateLinearLUT()
		{
			linearLUT = new byte[256];

			for (int i = 0; i < 256; i++)
			{
				float v = i / 255f;
				v = Mathf.GammaToLinearSpace(v);
				linearLUT[i] = (byte)(v * 255f);
			}
		}
		#endregion

		#region Helper Methods
		public string GetFileName(string prefix, int width, int height, int superSize, string fileExtension = "png")
		{
			if (string.IsNullOrEmpty(fileExtension))
				return $"{prefix}_{width * superSize}x{height * superSize}_{DateTime.Now:dd-MM-yyy_HH-mm-ss-fff}";

			return $"{prefix}_{width * superSize}x{height * superSize}_{DateTime.Now:dd-MM-yyy_HH-mm-ss-fff}.{fileExtension}";
		}

		public async Task SaveToFile(Texture2D tex, string fullPath, string fileExtension, Action<string> callback = null)
		{
			byte[] bytes;

			switch (fileExtension)
			{
				case "png": bytes = tex.EncodeToPNG(); break;
				case "jpg": bytes = tex.EncodeToJPG(); break;
				case "tga": bytes = tex.EncodeToTGA(); break;
				case "exr": bytes = tex.EncodeToEXR(); break;
				default: bytes = tex.EncodeToPNG(); break;
			}

			using FileStream fs = new(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);

			try
			{
				await fs.WriteAsync(bytes, 0, bytes.Length);
				Debug.Log($"Screenshot file saved: {fullPath}");
				callback?.Invoke(tex.name);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				OnSaveScreenshotAsTextureError?.Invoke(e);
			}
		}
		#endregion
	}

	[Serializable]
	public class ScreenshotConfig
	{
		public InputAction screenshotAction;
	}
}
