using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

		[SerializeField] private Texture2D rawScreenshotTex;
		[SerializeField] private Texture2D screenshotTex;
		[Tooltip("Use the Compress method when creating the resulting texture to reduce file size.")]
		public bool compressScreenshotAsTexture;
		public bool compressScreenshotAsTextureHighQuality;
		private bool savingScreenshotAsTexture;
		public bool SavingScreenshotAsTexture => savingScreenshotAsTexture;
		private byte[] gammaLUT;
		private byte[] linearLUT;

		[Header("Individual camera rendering")]
		[SerializeField] private List<Camera> cameraList;

		// Events.
		public event Action<string> OnScreenshotTaken;
		public event Action<Texture2D> OnRawScreenshotAsTextureTaken;
		public event Action<Texture2D> OnScreenshotAsTextureTaken;
		public event Action<string> OnScreenshotAsTextureSaved;
		public event Action<Exception> OnSaveScreenshotAsTextureError;

		private void Awake()
		{
			Instance = this;

			AddListeners();

			// For converting textures that are too bright or dark to compensate.
			CreateLinearLUT();
			CreateGammaLUT();

			// Prepare individual camera list.
			cameraList = new();

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

			screenshotTex = new(rawScreenshotTex.width, rawScreenshotTex.height, TextureFormat.RGB24, false);
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
			if (savingScreenshotAsTexture) return;
			savingScreenshotAsTexture = true;

			string fullPath = Path.Combine(screenshotsPath, screenshotTex.name + $".{fileExtension}");

			byte[] bytes;

			switch (fileExtension)
			{
				case "png": bytes = screenshotTex.EncodeToPNG(); break;
				case "jpg": bytes = screenshotTex.EncodeToJPG(); break;
				case "tga":	bytes = screenshotTex.EncodeToTGA(); break;
				case "exr":	bytes = screenshotTex.EncodeToEXR(); break;
				default: bytes = screenshotTex.EncodeToPNG(); break;
			}

			using FileStream fs = new(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);

			try
			{
				await fs.WriteAsync(bytes, 0, bytes.Length);
				Debug.Log($"Screenshot file saved: {fullPath}");
				OnScreenshotAsTextureSaved?.Invoke(screenshotTex.name);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				OnSaveScreenshotAsTextureError?.Invoke(e);
			}
			finally
			{
				savingScreenshotAsTexture = false;
			}
		}
		#endregion

		#region Render camera events
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
		public string GetFileName(string prefix, int width, int height, int superSize, string extension = "png")
		{
			if (string.IsNullOrEmpty(extension))
				return $"{prefix}_{width * superSize}x{height * superSize}_{DateTime.Now:dd-MM-yyy_HH-mm-ss-fff}";

			return $"{prefix}_{width * superSize}x{height * superSize}_{DateTime.Now:dd-MM-yyy_HH-mm-ss-fff}.{extension}";
		}	
		#endregion
	}

	[Serializable]
	public class ScreenshotConfig
	{
		public InputAction screenshotAction;
	}
}
