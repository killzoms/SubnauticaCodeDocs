using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AssemblyCSharp.WorldStreaming;
using UnityEngine;
using UnityEngine.Profiling;
using UWE;

namespace AssemblyCSharp
{
    public class TerrainDebugGUI : MonoBehaviour
    {
        public interface Section
        {
            void LayoutDebugSectionGUI();
        }

        public class MoveRecord
        {
            public float moveDelta;

            public float deltaTime;
        }

        public static TerrainDebugGUI main = null;

        public static HashSet<Section> sections = new HashSet<Section>();

        public Voxeland land;

        private Vector2 listScrollPos = Vector2.zero;

        private bool darkerBG;

        private string buildInfoString;

        private DateTime buildTime;

        private string[] pages = new string[6] { "Info", "Perf", "Clip Mapping", "PhotoTour", "Other", "Notifications" };

        private int selPage;

        private List<Mesh> meshes = new List<Mesh>();

        private Vector3[] verts;

        private int[] tris;

        private float playerPositionUpdateInterval = 0.15f;

        private float playerSpeedAverageIntervalA = 1f;

        private float playerSpeedAverageIntervalB = 5f;

        private Vector3 playerPosition = Vector3.zero;

        private Vector3 playerWorldVelocity = Vector3.zero;

        private Vector3 playerLocalVelocity = Vector3.zero;

        private float playerSpeed;

        private string playerWorldVelocityString = string.Empty;

        private string playerLocalVelocityString = string.Empty;

        private string playerSpeedString = string.Empty;

        private string playerSpeedStringAvgA = string.Empty;

        private string playerSpeedStringAvgB = string.Empty;

        private static ObjectPool<MoveRecord> moveRecordPool = ObjectPoolHelper.CreatePool<MoveRecord>();

        private List<MoveRecord> recentMoves = new List<MoveRecord>();

        private List<PerformanceConsoleCommands.Stats> dcStats;

        private Material markerMaterial;

        private List<GameObject> markers;

        private HashSet<LargeWorldEntity> visEnts = new HashSet<LargeWorldEntity>();

        private void Awake()
        {
            main = this;
        }

        private void Start()
        {
            StartCoroutine(UpdateSpeedString());
        }

        private IEnumerator UpdateSpeedString()
        {
            while (true)
            {
                yield return new WaitForSeconds(playerPositionUpdateInterval);
                playerWorldVelocityString = playerWorldVelocity.ToString("F2");
                playerLocalVelocityString = playerLocalVelocity.ToString("F2");
                playerSpeedString = playerSpeed.ToString("F2");
                Mathf.Max(playerSpeedAverageIntervalA, playerSpeedAverageIntervalB);
                float num = 0f;
                float num2 = 0f;
                float num3 = 0f;
                float num4 = 0f;
                List<MoveRecord> list = new List<MoveRecord>();
                for (int num5 = recentMoves.Count - 1; num5 >= 0; num5--)
                {
                    MoveRecord moveRecord = recentMoves[num5];
                    bool flag = false;
                    if (num < playerSpeedAverageIntervalA)
                    {
                        flag = true;
                        num2 += moveRecord.moveDelta;
                        num += moveRecord.deltaTime;
                    }
                    if (num3 < playerSpeedAverageIntervalB)
                    {
                        flag = true;
                        num4 += moveRecord.moveDelta;
                        num3 += moveRecord.deltaTime;
                    }
                    if (!flag)
                    {
                        list.Add(moveRecord);
                    }
                }
                while (list.Count > 0)
                {
                    MoveRecord moveRecord2 = list[0];
                    list.RemoveAt(0);
                    recentMoves.Remove(moveRecord2);
                    moveRecordPool.Return(moveRecord2);
                }
                float num6 = global::UWE.Utils.SafeDiv(num2, num);
                float num7 = global::UWE.Utils.SafeDiv(num4, num3);
                playerSpeedStringAvgA = num6.ToString("F2");
                playerSpeedStringAvgB = num7.ToString("F2");
            }
        }

        private void Update()
        {
            if (!(Player.main != null))
            {
                return;
            }
            SNCameraRoot sNCameraRoot = SNCameraRoot.main;
            if (sNCameraRoot != null)
            {
                Transform aimingTransform = sNCameraRoot.GetAimingTransform();
                if (aimingTransform != null)
                {
                    Vector3 position = aimingTransform.position;
                    Vector3 numerator = position - playerPosition;
                    playerWorldVelocity = global::UWE.Utils.SafeDiv(numerator, Time.deltaTime);
                    Quaternion rotation = aimingTransform.rotation;
                    playerLocalVelocity.x = Vector3.Dot(playerWorldVelocity, rotation * Vector3.forward);
                    playerLocalVelocity.y = Vector3.Dot(playerWorldVelocity, rotation * Vector3.up);
                    playerLocalVelocity.z = Vector3.Dot(playerWorldVelocity, rotation * Vector3.right);
                    playerSpeed = playerWorldVelocity.magnitude;
                    playerPosition = position;
                    MoveRecord moveRecord = moveRecordPool.Get();
                    moveRecord.moveDelta = numerator.magnitude;
                    moveRecord.deltaTime = Time.deltaTime;
                    recentMoves.Add(moveRecord);
                }
            }
        }

        private void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton4))
            {
                selPage--;
            }
            if (Input.GetKeyDown(KeyCode.JoystickButton5))
            {
                selPage++;
            }
            selPage = selPage.Clamp(0, pages.Length - 1);
        }

        private void LayoutBuildInfo()
        {
            if (buildInfoString == null)
            {
                buildInfoString = "";
                string text = File.ReadAllText(SNUtils.BuildTimeFile).Trim();
                buildTime = DateTime.Parse(text);
                if (File.Exists(SNUtils.BuildTimeFile))
                {
                    buildInfoString = buildInfoString + "Build time: " + text + "\n";
                }
                if (File.Exists(SNUtils.plasticStatusFile))
                {
                    buildInfoString = buildInfoString + "Plastic changeset: " + SNUtils.GetPlasticChangeSetOfBuild();
                }
            }
            GUILayout.Label(buildInfoString);
            GUILayout.Label("Built " + Mathf.FloorToInt(Convert.ToSingle((DateTime.Now - buildTime).TotalMinutes)) + " minutes ago");
        }

        private void OnGUI()
        {
            int num = Screen.width / 3;
            int num2 = 20;
            GUILayout.BeginArea(new Rect(Screen.width - num - num2, num2, num, Screen.height - 2 * num2), new GUIStyle("box"));
            darkerBG = GUILayout.Toggle(darkerBG, "Darker BG");
            selPage = GUILayout.Toolbar(selPage, pages);
            selPage = selPage.Clamp(0, pages.Length - 1);
            if (darkerBG)
            {
                GUILayout.BeginHorizontal("box");
            }
            listScrollPos = GUILayout.BeginScrollView(listScrollPos);
            switch (pages[selPage])
            {
                case "Info":
                {
                    LayoutBuildInfo();
                    MonitorLauncher monitorLauncher = MonitorLauncher.main;
                    long num3 = ((monitorLauncher != null) ? monitorLauncher.GetProcessInfo() : null)?.workingSet ?? 0;
                    GUILayout.Label("WorkingSet64 = " + (float)num3 / 1024f / 1024f + " MB");
                    GUILayout.Label("GC.GetTotalMemory = " + (float)GC.GetTotalMemory(forceFullCollection: false) / 1024f / 1024f + " MB");
                    if (Debug.isDebugBuild)
                    {
                        GUILayout.Label($"Profiler.usedHeapSize = {(float)Profiler.usedHeapSizeLong / 1024f / 1024f:0.0} MB\nProfiler.GetMonoHeapSize = {(float)Profiler.GetMonoHeapSizeLong() / 1024f / 1024f:0.0} MB\nProfiler.GetMonoUsedSize = {(float)Profiler.GetMonoUsedSizeLong() / 1024f / 1024f:0.0} MB");
                    }
                    GUILayout.Label($"OctNodes: {(float)VoxelandData.OctNode.GetPoolBytesUsed() / 1024f / 1024f:0.0} / {(float)VoxelandData.OctNode.GetPoolBytesTotal() / 1024f / 1024f:0.0} MB");
                    GUILayout.Label($"Camera world pos: {MainCamera.camera.transform.position}\nCamera batch #: {((LargeWorldStreamer.main != null) ? LargeWorldStreamer.main.GetContainingBatch(MainCamera.camera.transform.position) : Int3.zero)}\nCamera world forward: {MainCamera.camera.transform.forward}\nCamera clip: {MainCamera.camera.nearClipPlane} -- {MainCamera.camera.farClipPlane}");
                    GUILayout.Label(string.Format("LD biome: {0}\nPlayer biome: {1}\nRich Presence:{2}", CalculateRawBiome(Player.main), (Player.main != null) ? Player.main.GetBiomeString() : "", PlatformUtils.main.GetServices().GetRichPresence()));
                    GUILayout.Label($"Player world velocity: {playerWorldVelocity}\nPlayer local velocity: {playerLocalVelocityString}\nSpeed: {playerSpeedString} Avg ({playerSpeedAverageIntervalA:0.0}s): {playerSpeedStringAvgA} Avg ({playerSpeedAverageIntervalB:0.0}s): {playerSpeedStringAvgB}");
                    GUILayout.Label(string.Format("Time passed: {0:0.00}  Time.time {3:0.0}\nDay/Night scalar: {1:0.00}\nDay {2:0.00}", DayNightCycle.main.timePassedAsFloat, DayNightCycle.main.GetDayScalar(), DayNightCycle.main.GetDay(), Time.time));
                    GUILayout.Label($"Sunlight scalar (surface): {DayNightCycle.main.GetLightScalar():0.000}\nSunlight scalar (local): {DayNightCycle.main.GetLocalLightScalar():0.000}");
                    if (SNUtils.IsEngineDeveloper())
                    {
                        if (GUILayout.Button("Create Test Meshes"))
                        {
                            if (verts == null)
                            {
                                int num4 = 10000;
                                verts = new Vector3[num4];
                                tris = new int[3 * num4];
                                for (int i = 0; i < tris.Length; i++)
                                {
                                    tris[i] = global::UnityEngine.Random.Range(0, num4);
                                }
                            }
                            int num5 = 100;
                            for (int j = 0; j < num5; j++)
                            {
                                meshes.Add(new Mesh());
                                meshes[j].vertices = verts;
                                meshes[j].triangles = tris;
                            }
                        }
                        if (GUILayout.Button("Clear Meshes"))
                        {
                            foreach (Mesh mesh in meshes)
                            {
                                mesh.Clear(Event.current.alt);
                            }
                        }
                        if (GUILayout.Button("Clear and Reset Meshes"))
                        {
                            foreach (Mesh mesh2 in meshes)
                            {
                                mesh2.Clear(Event.current.alt);
                                mesh2.vertices = verts;
                                mesh2.triangles = tris;
                            }
                        }
                        if (GUILayout.Button("Destroy Meshes"))
                        {
                            foreach (Mesh mesh3 in meshes)
                            {
                                global::UnityEngine.Object.Destroy(mesh3);
                            }
                        }
                    }
                    EcoManager.debugFreeze = GUILayout.Toggle(EcoManager.debugFreeze, "Freeze EcoMgr");
                    GUILayout.BeginHorizontal("box");
                    if (FrameTimeRecorder.main.IsRecording())
                    {
                        if (GUILayout.Button("Start Frame Time Recording"))
                        {
                            string path = global::UWE.Utils.GenerateNumberedFileName("FRAMETIMES-", ".ignore");
                            FrameTimeRecorder.main.Record(path);
                        }
                    }
                    else if (GUILayout.Button("Stop Frame Time Record"))
                    {
                        FrameTimeRecorder.main.Stop();
                    }
                    GUILayout.EndHorizontal();
                    foreach (Section section in sections)
                    {
                        GUILayout.BeginVertical("textarea");
                        section.LayoutDebugSectionGUI();
                        GUILayout.EndVertical();
                    }
                    break;
                }
                case "Clip Mapping":
                    land.GetComponent<WorldStreamer>().DebugGUI();
                    land.GetComponent<WorldStreamerTuner>().DebugGUI();
                    break;
                case "Perf":
                    LayoutPerfGUI();
                    break;
                case "Notifications":
                    NotificationManager.main.LayoutDebugGUI();
                    break;
                default:
                    if (LargeWorldStreamer.main != null && PlayerPrefsUtils.PrefsToggle(defaultVal: false, "UWE.Editor.LargeWorldDebug", "Large World Debug"))
                    {
                        LargeWorldStreamer.main.LayoutDebugGUI();
                    }
                    if (land != null)
                    {
                        VoxelandUtils.LayoutDebugGUI(land);
                    }
                    if (PAXTerrainController.main != null)
                    {
                        PAXTerrainController.main.LayoutDebugGUI();
                    }
                    if (!Debug.isDebugBuild && !Application.isEditor)
                    {
                        break;
                    }
                    GUILayout.BeginHorizontal("box");
                    if (!global::UnityEngine.Profiling.Profiler.enabled)
                    {
                        if (GUILayout.Button("Begin Profiling"))
                        {
                            global::UnityEngine.Profiling.Profiler.logFile = "profiling.log";
                            global::UnityEngine.Profiling.Profiler.enableBinaryLog = true;
                            global::UnityEngine.Profiling.Profiler.enabled = true;
                        }
                    }
                    else if (GUILayout.Button("End Profiling"))
                    {
                        global::UnityEngine.Profiling.Profiler.enabled = false;
                    }
                    GUILayout.EndHorizontal();
                    break;
                case "PhotoTour":
                    if (!PhotoTour.main.LayoutGUI())
                    {
                        base.enabled = false;
                    }
                    break;
            }
            GUILayout.EndScrollView();
            if (darkerBG)
            {
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private string CalculateRawBiome(Player player)
        {
            AtmosphereDirector atmosphereDirector = AtmosphereDirector.main;
            if ((bool)atmosphereDirector)
            {
                string biomeOverride = atmosphereDirector.GetBiomeOverride();
                if (!string.IsNullOrEmpty(biomeOverride))
                {
                    return biomeOverride;
                }
            }
            LargeWorld largeWorld = LargeWorld.main;
            if ((bool)largeWorld && (bool)player)
            {
                return largeWorld.GetBiome(player.transform.position);
            }
            return "<unknown>";
        }

        private void LayoutPerfGUI()
        {
            if (GUILayout.Button("Collect Est. Draw Call Stats"))
            {
                Dictionary<string, PerformanceConsoleCommands.Stats> dictionary = PerformanceConsoleCommands.CollectStats(visEnts, Event.current.alt);
                dcStats = new List<PerformanceConsoleCommands.Stats>();
                foreach (KeyValuePair<string, PerformanceConsoleCommands.Stats> item in dictionary)
                {
                    dcStats.Add(item.Value);
                }
                dcStats.Sort(PerformanceConsoleCommands.Stats.CompareByDrawCallsDesc);
            }
            if (GUILayout.Button("Fake Terrain Occlusion Cull"))
            {
                FakeTerrainOcclusionCull();
            }
            if (markers != null && GUILayout.Button("Clear Markers"))
            {
                foreach (GameObject marker in markers)
                {
                    global::UnityEngine.Object.Destroy(marker);
                }
                markers.Clear();
            }
            if (dcStats == null)
            {
                return;
            }
            for (int i = 0; i < dcStats.Count && i < 20; i++)
            {
                PerformanceConsoleCommands.Stats stats = dcStats[i];
                if (stats == null)
                {
                    continue;
                }
                GUILayout.BeginHorizontal("textarea");
                GUILayout.Label(stats.numDrawCalls + " calls, " + stats.numEntInsts + " insts, " + (float)stats.numDrawCalls * 1f / (float)stats.numEntInsts + " dc/inst for '" + stats.label + "'");
                if (stats.numEntInsts > 0)
                {
                    if (GUILayout.Button("Mark"))
                    {
                        if (markerMaterial == null)
                        {
                            markerMaterial = new Material("Debug Marker Material");
                            markerMaterial.shader = Shader.Find("UWE/Debug/Marker");
                            markerMaterial.SetColor(ShaderPropertyID._Color, Color.green);
                        }
                        if (markers == null)
                        {
                            markers = new List<GameObject>();
                        }
                        foreach (LargeWorldEntity visEnt in visEnts)
                        {
                            if (visEnt != null && visEnt.gameObject.name == stats.label)
                            {
                                GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                gameObject.GetComponent<Renderer>().sharedMaterial = markerMaterial;
                                gameObject.transform.position = visEnt.transform.position;
                                global::UnityEngine.Object.Destroy(gameObject.GetComponent<Collider>());
                                markers.Add(gameObject);
                            }
                        }
                    }
                    if (GUILayout.Button("Estimate Terrain Occluded"))
                    {
                        int num = 0;
                        int num2 = 0;
                        foreach (LargeWorldEntity visEnt2 in visEnts)
                        {
                            if (visEnt2 == null || visEnt2.gameObject.name != stats.label)
                            {
                                continue;
                            }
                            Renderer[] componentsInChildren = visEnt2.GetComponentsInChildren<Renderer>();
                            foreach (Renderer renderer in componentsInChildren)
                            {
                                if (renderer.isVisible)
                                {
                                    if (IsProbablyTerrainOccluded(renderer))
                                    {
                                        Debug.DrawLine(renderer.bounds.center, MainCamera.camera.transform.position, Color.red, 5f);
                                        num++;
                                    }
                                    else
                                    {
                                        Debug.DrawLine(renderer.bounds.center, MainCamera.camera.transform.position, Color.green, 5f);
                                    }
                                    num2++;
                                }
                            }
                        }
                        Debug.Log("number of " + stats.label + " probably occluded by terrain = " + num + "/" + num2);
                    }
                    if (GUILayout.Button("Hide All"))
                    {
                        foreach (LargeWorldEntity visEnt3 in visEnts)
                        {
                            if (visEnt3 != null && visEnt3.gameObject.name == stats.label)
                            {
                                Renderer[] componentsInChildren = visEnt3.GetComponentsInChildren<Renderer>();
                                for (int j = 0; j < componentsInChildren.Length; j++)
                                {
                                    componentsInChildren[j].enabled = false;
                                }
                            }
                        }
                        dcStats[i] = null;
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        public static bool IsProbablyTerrainOccluded(Renderer r, bool drawPassRays = false)
        {
            int num = 100;
            Bounds bounds = r.bounds;
            for (int i = 0; i < num; i++)
            {
                Vector3 vector = bounds.RandomWithin();
                Vector3 position = MainCamera.camera.transform.position;
                Vector3 normalized = (vector - position).normalized;
                float magnitude = (vector - position).magnitude;
                if (!Physics.Raycast(position, normalized, magnitude, Voxeland.GetTerrainLayerMask()))
                {
                    if (drawPassRays)
                    {
                        Debug.DrawLine(vector, position, Color.green, 5f);
                    }
                    return false;
                }
            }
            return true;
        }

        public void FakeTerrainOcclusionCull()
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
            int num = 0;
            int num2 = 0;
            global::UnityEngine.Object[] array = global::UnityEngine.Object.FindObjectsOfType(typeof(Renderer));
            for (int i = 0; i < array.Length; i++)
            {
                Renderer renderer = (Renderer)array[i];
                if (!renderer.enabled || !renderer.isVisible)
                {
                    continue;
                }
                LargeWorldEntity largeWorldEntity = renderer.gameObject.FindAncestor<LargeWorldEntity>();
                Voxeland voxeland = renderer.gameObject.FindAncestor<Voxeland>();
                string text = ((largeWorldEntity != null) ? largeWorldEntity.gameObject.name : ((voxeland != null) ? voxeland.gameObject.GetFullHierarchyPath() : null));
                if (text == null)
                {
                    continue;
                }
                dictionary2[text] = dictionary2.GetOrDefault(text, 0) + 1;
                num++;
                bool flag = false;
                if (IsProbablyTerrainOccluded(renderer))
                {
                    renderer.enabled = false;
                    dictionary[text] = dictionary.GetOrDefault(text, 0) + 1;
                    num2++;
                    flag = true;
                }
                if (Event.current.shift)
                {
                    if (flag)
                    {
                        Debug.DrawLine(renderer.bounds.center, MainCamera.camera.transform.position, Color.red, 5f);
                    }
                    else
                    {
                        Debug.DrawLine(renderer.bounds.center, MainCamera.camera.transform.position, Color.green, 5f);
                    }
                }
            }
            foreach (KeyValuePair<string, int> item in dictionary2)
            {
                int orDefault = dictionary.GetOrDefault(item.Key, 0);
                Debug.Log(item.Key + " " + orDefault + "/" + item.Value);
            }
            Debug.Log("Totals: " + num2 + "/" + num);
        }
    }
}
