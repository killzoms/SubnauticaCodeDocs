using UnityEngine;

namespace AssemblyCSharp
{
    public class VFXConsoleCommands : MonoBehaviour
    {
        private void Awake()
        {
            DevConsole.RegisterConsoleCommand(this, "vfx");
        }

        private void OnConsoleCommand_vfx(NotificationCenter.Notification n)
        {
            bool flag = ((string)n.data[0]).Contains("cyclopssmoke");
            if (n.data.Count > 1)
            {
                float result = 0f;
                if (float.TryParse((string)n.data[1], out result) && flag)
                {
                    UpdateCyclopsSmokeScreenFX(result);
                }
            }
        }

        private void UpdateCyclopsSmokeScreenFX(float intensityScalar)
        {
            intensityScalar = Mathf.Clamp(intensityScalar, 0f, 1f);
            CyclopsSmokeScreenFXController component = MainCamera.camera.GetComponent<CyclopsSmokeScreenFXController>();
            if (component != null)
            {
                component.intensity = intensityScalar;
                ErrorMessage.AddDebug("Setting CyclopsSmokeScreenFXController to " + intensityScalar + ".");
            }
        }
    }
}
