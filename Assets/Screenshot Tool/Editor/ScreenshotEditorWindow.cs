using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpalatoBros.ScreenshotTool
{
    public class ScreenshotEditorWindow : EditorWindow
    {
		[MenuItem("Window/Spalato Bros/Screenshot Tool")]
		public static void ShowWindow()
		{
			GetWindow<ScreenshotEditorWindow>("Screenshot Tool");
		}

		private void OnGUI()
		{
			if (GUILayout.Button("Open screenshots folder"))
			{
				EditorUtility.RevealInFinder(Application.persistentDataPath);
			}

			if (!ScreenshotController.Instance)
			{
				return;
			}

			ScreenshotsToFile();
			ScreenshotsToTexture();
			RenderCamerasToTexture();
		}

		private void ScreenshotsToFile()
		{
			GUILayout.BeginHorizontal();

			GUILayout.Label("Screenshot To File");

			if (GUILayout.Button("1x"))
			{
				ScreenshotController.Instance.TakeRawScreenshot(1);
			}

			if (GUILayout.Button("2x"))
			{
				ScreenshotController.Instance.TakeRawScreenshot(2);
			}

			if (GUILayout.Button("4x"))
			{
				ScreenshotController.Instance.TakeRawScreenshot(4);
			}

			GUILayout.EndHorizontal();
		}

		private void ScreenshotsToTexture()
		{
			GUILayout.BeginHorizontal();

			GUILayout.Label("Screenshot As Texture");

			if (GUILayout.Button("1x"))
			{
				ScreenshotController.Instance.StartCoroutine(ScreenshotController.Instance.TakeRawScreenshotAsTexture(1));
			}

			if (GUILayout.Button("2x"))
			{
				ScreenshotController.Instance.StartCoroutine(ScreenshotController.Instance.TakeRawScreenshotAsTexture(2));
			}

			if (GUILayout.Button("4x"))
			{
				ScreenshotController.Instance.StartCoroutine(ScreenshotController.Instance.TakeRawScreenshotAsTexture(4));
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Save Screenshot As Texture");

			if (GUILayout.Button("png"))
			{
				ScreenshotController.Instance.SaveScreenshotAsTextureToFile("png");
			}

			if (GUILayout.Button("jpg"))
			{
				ScreenshotController.Instance.SaveScreenshotAsTextureToFile("jpg");
			}

			if (GUILayout.Button("tga"))
			{
				ScreenshotController.Instance.SaveScreenshotAsTextureToFile("tga");
			}

			if (GUILayout.Button("exr"))
			{
				ScreenshotController.Instance.SaveScreenshotAsTextureToFile("exr");
			}

			GUILayout.EndHorizontal();
		}

		private void RenderCamerasToTexture()
		{
			GUILayout.BeginHorizontal();

			GUILayout.Label("Render all cameras to textures");

			if (GUILayout.Button("Current Resolution"))
			{
				ScreenshotController.Instance.RenderAllCameras();
			}

			if (GUILayout.Button("1920 x 1080"))
			{
				ScreenshotController.Instance.RenderAllCamerasAtFullHD();
			}

			if (GUILayout.Button("3840 x 2160"))
			{
				ScreenshotController.Instance.RenderAllCamerasAtUltraHD();
			}

			if (GUILayout.Button($"Custom: {ScreenshotController.Instance.CustomCameraResolution.x} x {ScreenshotController.Instance.CustomCameraResolution.y}"))
			{
				ScreenshotController.Instance.RenderAllCamerasAtCustomResolution();
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Save camera renders to file");

			if (GUILayout.Button("png"))
			{
				ScreenshotController.Instance.SaveCameraRendersToFile("png");
			}

			if (GUILayout.Button("jpg"))
			{
				ScreenshotController.Instance.SaveCameraRendersToFile("jpg");
			}

			if (GUILayout.Button("tga"))
			{
				ScreenshotController.Instance.SaveCameraRendersToFile("tga");
			}

			if (GUILayout.Button("exr"))
			{
				ScreenshotController.Instance.SaveCameraRendersToFile("exr");
			}

			GUILayout.EndHorizontal();
		}
	}
}
