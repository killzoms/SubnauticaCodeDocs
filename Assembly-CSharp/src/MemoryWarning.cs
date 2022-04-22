using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class MemoryWarning : MonoBehaviour
    {
        private const string warningText = "<size=24><color=#D00000>WARNING\n</color></size><size=18><color=#FFFFFF>The game is about to run out of memory and crash. Please save now and restart the game.\n</color></size><size=10><color=#FFFFFF>This issue has nothing to do with your machine. We are working on improving the situation. In the meantime please save often. Apologies for the inconvenience.</color></size>";

        private bool warningShowing;

        private static long warningThresholdBytes => Convert.ToInt64(SystemInfo.systemMemorySize) * 1024 * 1024 * 3 / 4;

        private static long warningHysteresis => warningThresholdBytes - 209715200;

        private void Start()
        {
            InvokeRepeating("CheckMemory", 1f, 1f);
        }

        private void CheckMemory()
        {
            uGUI_PopupMessage warning = Hint.main.warning;
            if (!warning)
            {
                return;
            }
            MonitorLauncher main = MonitorLauncher.main;
            if (!main)
            {
                return;
            }
            ProcessInfo processInfo = main.GetProcessInfo();
            if (processInfo == null)
            {
                return;
            }
            if (warningShowing)
            {
                if (processInfo.workingSet < warningHysteresis)
                {
                    warning.Hide();
                    warningShowing = false;
                }
            }
            else if (processInfo.workingSet > warningThresholdBytes)
            {
                warning.SetBackgroundColor(new Color32(127, 0, 0, 127));
                warning.SetText("<size=24><color=#D00000>WARNING\n</color></size><size=18><color=#FFFFFF>The game is about to run out of memory and crash. Please save now and restart the game.\n</color></size><size=10><color=#FFFFFF>This issue has nothing to do with your machine. We are working on improving the situation. In the meantime please save often. Apologies for the inconvenience.</color></size>", TextAnchor.MiddleCenter);
                warning.Show(-1f);
                warningShowing = true;
            }
        }
    }
}
