using System.Collections;
using System.IO;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class WorldBootMenu : MonoBehaviour
    {
        public PAXTerrainController host;

        private bool waitingOnLoad;

        public LargeWorldStreamer streamer;

        private CSVEntitySpawner spawner;

        private void OnEnable()
        {
            global::UWE.Utils.lockCursor = false;
            spawner = GetComponent<CSVEntitySpawner>();
        }

        private void OnDisable()
        {
            global::UWE.Utils.lockCursor = true;
            PlayerPrefs.Save();
        }

        private void Start()
        {
            waitingOnLoad = true;
            Debug.Log("WorldBootMenu: Lightmapped prefabs all loaded - starting game frame " + Time.frameCount);
            waitingOnLoad = false;
        }

        private void OnGUI()
        {
            if (!base.isActiveAndEnabled || waitingOnLoad)
            {
                return;
            }
            global::UWE.Utils.lockCursor = false;
            int num = 200;
            GUILayout.BeginArea(new Rect(Screen.width / 2 - num / 2, 10f, num, 400f));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Choose a starting world:");
            char c = '1';
            string[] directories = Directory.GetDirectories(SNUtils.unmanagedDataDir);
            foreach (string text in directories)
            {
                if (LargeWorld.IsValidWorldDir(text))
                {
                    bool flag = (c == '1' && Input.GetKey(KeyCode.JoystickButton0)) || (c == '2' && Input.GetKey(KeyCode.JoystickButton1)) || (c == '3' && Input.GetKey(KeyCode.JoystickButton2));
                    if (QuickLaunchHelper.IsQuickLaunching() && c == '1')
                    {
                        flag = true;
                    }
                    if (GUILayout.Button(c + " :: " + Path.GetFileName(text)) || (Event.current.isKey && Event.current.character == c) || flag)
                    {
                        waitingOnLoad = true;
                        host.dataDirPath = text;
                        SaveLoadManager.main.SetCurrentSlot("test");
                        CoroutineHost.StartCoroutine(LoadSlot());
                    }
                    c = (char)(c + 1);
                }
            }
            GUILayout.Label($"Looking in: {SNUtils.unmanagedDataDir}");
            streamer.debugBiomeMaterials = PlayerPrefsUtils.PrefsToggle(streamer.debugBiomeMaterials, "UWE.Editor.DebugBiomeMaterials", "Debug Biome Materials");
            streamer.debugDisableAllEnts = PlayerPrefsUtils.PrefsToggle(streamer.debugDisableAllEnts, "UWE.Editor.DebugDisableAllEnts", "Debug Disable All Entities");
            if ((Event.current.control && !PlatformUtils.isConsolePlatform) || Input.GetKey(KeyCode.JoystickButton4))
            {
                streamer.debugSkipEntityLoad = true;
            }
            else
            {
                streamer.debugSkipEntityLoad = PlayerPrefsUtils.PrefsToggle(streamer.debugSkipEntityLoad, "uwe.sn.skipEntityLoad", "Do Not Load Entities");
            }
            streamer.overrideDisableGrass = PlayerPrefsUtils.PrefsToggle(streamer.overrideDisableGrass, "uwe.sn.overrideDisableGrass", "Override Disable Grass");
            EntitySlot.debugSlots = PlayerPrefsUtils.PrefsToggle(EntitySlot.debugSlots, "uwe.sn.debugEntitySlots", "Debug Entity Slots");
            StayAtLeashPosition.debugShortLeash = PlayerPrefsUtils.PrefsToggle(StayAtLeashPosition.debugShortLeash, "uwe.sn.debugShortLeash", "Debug Short Leash");
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        public IEnumerator LoadSlot()
        {
            yield return SaveLoadManager.main.LoadAsync();
            host.OnBootMenuDone();
        }
    }
}
