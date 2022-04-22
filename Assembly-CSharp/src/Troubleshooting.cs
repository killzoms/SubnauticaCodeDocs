using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class Troubleshooting : MonoBehaviour
    {
        public Text systemSpecs;

        private void Start()
        {
            updateSystemSpecs();
        }

        private void updateSystemSpecs()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("<color=#FFA300FF>" + Language.main.Get("CPU") + ": </color>{0}\n", SystemInfo.processorType);
            stringBuilder.AppendFormat("<color=#FFA300FF>" + Language.main.Get("RAM") + ": </color>{0} " + Language.main.Get("megabytes") + "\n", SystemInfo.systemMemorySize);
            stringBuilder.AppendFormat("<color=#FFA300FF>" + Language.main.Get("GPU") + ": </color>{0}\n", SystemInfo.graphicsDeviceName);
            stringBuilder.AppendFormat("<color=#FFA300FF>" + Language.main.Get("GPURAM") + ": </color>{0} " + Language.main.Get("megabytes") + "\n", SystemInfo.graphicsMemorySize);
            stringBuilder.AppendFormat("<color=#FFA300FF>" + Language.main.Get("OS") + ": </color>{0}\n", SystemInfo.operatingSystem);
            stringBuilder.AppendFormat("<color=#FFA300FF>" + Language.main.Get("Threads") + ": </color>{0} " + Language.main.Get("LogicalProcessors") + "\n", SystemInfo.processorCount);
            stringBuilder.AppendFormat("<color=#FFA300FF>" + Language.main.Get("Version") + ": </color>{0}\n", SNUtils.GetPlasticChangeSetOfBuild());
            stringBuilder.AppendFormat("<color=#FFA300FF>" + Language.main.Get("BuildDate") + ": </color>{0}\n", SNUtils.GetDateTimeOfBuild());
            systemSpecs.text = stringBuilder.ToString();
        }

        public void openTroubleshootingGuide()
        {
            Application.OpenURL("https://unknownworlds.com/subnautica/steam-troubleshooting-guide");
        }

        public void openBugReportingForums()
        {
            Application.OpenURL("https://forums.unknownworlds.com/categories/subnautica-bug-reporting");
        }
    }
}
