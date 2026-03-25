using UnityEngine;

namespace SpalatoBros.ScreenshotTool
{
    public class AddCameraToScreenshotController : MonoBehaviour
    {
        [SerializeField] private Camera cam;

        private void Start()
        {
            ScreenshotController.Instance.AddCamera(cam);
        }
    }
}
