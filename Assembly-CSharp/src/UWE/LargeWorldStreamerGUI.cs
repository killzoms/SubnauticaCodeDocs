using UnityEngine;

namespace AssemblyCSharp.UWE
{
    public class LargeWorldStreamerGUI : MonoBehaviour
    {
        public GUIStyle textStyle;

        public bool showBatchGrid;

        public bool showSuperGrid;

        private LargeWorldStreamer streamer;

        private void Awake()
        {
            streamer = GetComponent<LargeWorldStreamer>();
        }

        private void OnGUI()
        {
            if (!streamer)
            {
                GUILayout.Label("No octree streamer active!", textStyle);
                return;
            }
            if (!streamer.IsReady())
            {
                GUILayout.Label("Streamer not inited yet", textStyle);
                return;
            }
            GUILayout.Label("Last load frame: " + streamer.lastLoadFrame, textStyle);
            GUILayout.Label("Last unload frame: " + streamer.lastUnloadFrame, textStyle);
            GUILayout.Label("Loaded/budget");
            GUILayout.HorizontalScrollbar(streamer.loadedMBsOut, 0.5f, 0f, streamer.budgetMBsOut);
            GUILayout.Label("loaded/budget = " + streamer.loadedMBsOut + " / " + streamer.budgetMBsOut);
            _ = GetComponent<Voxeland>().data;
        }
    }
}
