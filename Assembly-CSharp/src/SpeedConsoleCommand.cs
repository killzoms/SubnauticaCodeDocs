using UnityEngine;

namespace AssemblyCSharp
{
    public class SpeedConsoleCommand : MonoBehaviour
    {
        private void Awake()
        {
            DevConsole.RegisterConsoleCommand(this, "speed");
        }

        private void OnConsoleCommand_speed(NotificationCenter.Notification n)
        {
            string text = (string)n.data[0];
            if (text != null && text != "")
            {
                float value = float.Parse(text);
                value = Mathf.Clamp(value, 0f, 10f);
                ErrorMessage.AddDebug("Setting speed to " + value + ".");
                Time.timeScale = value;
            }
            else
            {
                ErrorMessage.AddDebug("Must specify speed value from 0 to 10.");
            }
        }
    }
}
