using UnityEditor;
using UnityEngine;

namespace SpalatoBros.ScreenshotTool
{
    [CustomEditor(typeof(ScreenshotController))]
    public class ScreenshotControllerEditor : Editor
    {
		private ScreenshotController sc;

		private void OnEnable()
		{
			sc = (ScreenshotController)target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
		}
    }
}
