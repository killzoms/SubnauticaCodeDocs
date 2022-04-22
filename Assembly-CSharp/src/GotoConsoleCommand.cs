using System;
using System.Collections;
using System.Text;
using UnityEngine;

namespace AssemblyCSharp
{
    public class GotoConsoleCommand : MonoBehaviour
    {
        public static GotoConsoleCommand main;

        [AssertNotNull]
        public TeleportCommandData data;

        private bool continueSpam;

        private void Awake()
        {
            main = this;
            DevConsole.RegisterConsoleCommand(this, "goto");
            DevConsole.RegisterConsoleCommand(this, "gotospam");
            DevConsole.RegisterConsoleCommand(this, "gotostop");
        }

        private void OnConsoleCommand_gotospam(NotificationCenter.Notification n)
        {
            continueSpam = true;
            StartCoroutine(GotoSpam());
        }

        private void OnConsoleCommand_gotostop(NotificationCenter.Notification n)
        {
            continueSpam = false;
        }

        private IEnumerator GotoSpam()
        {
            int choice1 = global::UnityEngine.Random.Range(0, data.locations.Length);
            int choice2 = global::UnityEngine.Random.Range(0, data.locations.Length);
            while (continueSpam)
            {
                yield return new WaitForSeconds(global::UnityEngine.Random.value * 3f);
                TeleportPosition teleportPosition = data.locations[choice1];
                int num = choice2;
                choice2 = choice1;
                choice1 = num;
                ErrorMessage.AddDebug("Jumping to position: " + teleportPosition.position);
                Player.main.SetPosition(teleportPosition.position);
                Player.main.OnPlayerPositionCheat();
            }
        }

        private void OnConsoleCommand_goto(NotificationCenter.Notification n)
        {
            if (n.data != null && n.data.Count == 1)
            {
                string b = (string)n.data[0];
                TeleportPosition[] locations = data.locations;
                foreach (TeleportPosition teleportPosition in locations)
                {
                    if (string.Equals(teleportPosition.name, b, StringComparison.OrdinalIgnoreCase))
                    {
                        ErrorMessage.AddDebug("Jumping to position: " + teleportPosition.position);
                        Player.main.SetPosition(teleportPosition.position);
                        Player.main.OnPlayerPositionCheat();
                        break;
                    }
                }
                return;
            }
            StringBuilder stringBuilder = new StringBuilder();
            for (int j = 0; j < data.locations.Length; j++)
            {
                TeleportPosition teleportPosition2 = data.locations[j];
                stringBuilder.Append(teleportPosition2.name);
                stringBuilder.Append(", ");
                if (j % 5 == 0)
                {
                    stringBuilder.AppendLine();
                }
            }
            ErrorMessage.AddDebug(stringBuilder.ToString());
        }
    }
}
