using System;
using System.Collections;
using UnityEngine;

namespace AssemblyCSharp
{
    public class PDAConsoleCommands : MonoBehaviour
    {
        private void Awake()
        {
            DevConsole.RegisterConsoleCommand(this, "ency", caseSensitiveArgs: true);
        }

        private void OnConsoleCommand_ency(NotificationCenter.Notification n)
        {
            Hashtable data = n.data;
            if (data != null && data.Count > 0)
            {
                string text = (string)data[0];
                if (string.Equals(text, "all", StringComparison.OrdinalIgnoreCase))
                {
                    bool verbose = false;
                    if (data.Count > 1)
                    {
                        verbose = string.Equals((string)data[1], "verbose", StringComparison.OrdinalIgnoreCase);
                    }
                    PDAEncyclopedia.AddAllEntries(verbose);
                }
                else if (PDAEncyclopedia.HasEntryData(text))
                {
                    PDAEncyclopedia.Add(text, verbose: true);
                }
                else
                {
                    ErrorMessage.AddDebug("No entry found in Encyclopedia for key '" + text + "'.\nEntry keys are case-sensitive!");
                }
            }
            else
            {
                ErrorMessage.AddDebug("Usage: ency id/all [verbose]");
            }
        }

        private void OnConsoleCommand_notificationadd(NotificationCenter.Notification n)
        {
            Hashtable data = n.data;
            if (data != null && data.Count >= 2)
            {
                string text = (string)data[1];
                if (!string.IsNullOrEmpty(text) && global::UWE.Utils.TryParseEnum<NotificationManager.Group>((string)data[0], out var result))
                {
                    float duration = 5f;
                    if (data.Count > 2 && float.TryParse((string)data[2], out var result2))
                    {
                        duration = result2;
                    }
                    NotificationManager.main.Add(result, text, duration);
                    return;
                }
            }
            ErrorMessage.AddDebug("Usage: notificationadd NotificationGroup NotificationKey [duration]");
        }

        private void OnConsoleCommand_notificationremove(NotificationCenter.Notification n)
        {
            Hashtable data = n.data;
            if (data != null && data.Count >= 2)
            {
                string text = (string)data[1];
                if (!string.IsNullOrEmpty(text) && global::UWE.Utils.TryParseEnum<NotificationManager.Group>((string)data[0], out var result))
                {
                    NotificationManager.main.Remove(result, text);
                    return;
                }
            }
            ErrorMessage.AddDebug("Usage: removenotification NotificationGroup NotificationKey");
        }

        private void OnConsoleCommand_notificationlist(NotificationCenter.Notification n)
        {
            string message = NotificationManager.main.ToString();
            ErrorMessage.AddDebug(message);
            Debug.Log(message);
        }
    }
}
