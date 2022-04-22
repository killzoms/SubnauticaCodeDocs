using AssemblyCSharp.WorldStreaming;
using Gendarme;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class ConsoleDebugGUI : MonoBehaviour
    {
        public enum EMode
        {
            Off,
            DebugBytePool,
            DebugArrayPools,
            DebugStreaming,
            TextureStreaming,
            DebugObjectPools,
            DebugUpdaters
        }

        private const int kMaxBuckets = 1024;

        private const int kMaxPages = 128;

        public Font fixedWidthFont;

        private GUIStyle guiSkin_Label_H0;

        private GUIStyle guiSkin_Label_H1;

        private GUIStyle guiSkin_Label_H2;

        private GUIStyle guiSkin_Label_ListItem;

        private bool guiInitialized;

        private int[] freeHeap;

        private int[] inUseHeap;

        private long[] wasteHeap;

        private int[] peakHeap;

        private int[] pageUse;

        private int[] pageFree;

        private EMode _mode;

        public static ConsoleDebugGUI instance { get; private set; }

        public EMode mode
        {
            get
            {
                return _mode;
            }
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    base.enabled = _mode != 0 && !PlatformUtils.isShippingRelease;
                }
            }
        }

        private void Awake()
        {
            instance = this;
        }

        private void OnGUI()
        {
            if (!guiInitialized)
            {
                if (fixedWidthFont == null)
                {
                    fixedWidthFont = GUI.skin.font;
                }
                GUIStyle gUIStyle = new GUIStyle(GUI.skin.label);
                gUIStyle.border = new RectOffset(0, 0, 0, 0);
                gUIStyle.contentOffset = new Vector2(0f, 0f);
                gUIStyle.margin = new RectOffset(0, 0, 0, 0);
                gUIStyle.padding = new RectOffset(0, 0, 0, 0);
                guiSkin_Label_H0 = new GUIStyle(gUIStyle);
                guiSkin_Label_H0.fontSize = 14;
                guiSkin_Label_H0.fontStyle = FontStyle.Bold;
                guiSkin_Label_H0.alignment = TextAnchor.MiddleCenter;
                guiSkin_Label_H0.padding = new RectOffset(4, 4, 4, 4);
                guiSkin_Label_H1 = new GUIStyle(gUIStyle);
                guiSkin_Label_H1.fontSize = 11;
                guiSkin_Label_H1.fontStyle = FontStyle.Bold;
                guiSkin_Label_H1.padding = new RectOffset(0, 0, 2, 4);
                guiSkin_Label_H2 = new GUIStyle(gUIStyle);
                guiSkin_Label_H2.font = fixedWidthFont;
                guiSkin_Label_H2.fontSize = 11;
                guiSkin_Label_H2.padding = new RectOffset(0, 0, 2, 2);
                guiSkin_Label_ListItem = new GUIStyle(gUIStyle);
                guiSkin_Label_ListItem.font = fixedWidthFont;
                guiSkin_Label_ListItem.fontSize = 11;
                guiSkin_Label_ListItem.contentOffset = new Vector2(4f, 0f);
                guiInitialized = true;
            }
            float num = ((mode == EMode.TextureStreaming) ? 300f : 430f);
            float height = (float)Screen.height - 16f;
            float x = (float)Screen.width - num - 16f;
            float y = 16f;
            GUI.Box(new Rect(x, y, num, height), GUIContent.none);
            GUILayout.BeginArea(new Rect(x, y, num, height));
            GUILayout.Space(4f);
            GUILayout.Label("Console Debug UI", guiSkin_Label_H0);
            GUILayout.Space(4f);
            switch (mode)
            {
                case EMode.DebugBytePool:
                    DisplayDebugCommonByteArray();
                    break;
                case EMode.DebugArrayPools:
                    DisplayDebugClipMapMeshBufferPools();
                    break;
                case EMode.DebugStreaming:
                    DisplayDebugStreaming();
                    break;
                case EMode.TextureStreaming:
                    DisplayDebugTextureStreaming();
                    break;
                case EMode.DebugObjectPools:
                    DisplayDebugObjectPools();
                    break;
                case EMode.DebugUpdaters:
                    DisplayDebugUpdaters();
                    break;
            }
            GUILayout.EndArea();
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        [SuppressMessage("Subnautica.Rules", "AvoidStringConcatenation")]
        private void DisplayDebugStreaming()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Streaming", guiSkin_Label_H1);
            if (LargeWorldStreamer.main != null)
            {
                GUILayout.Label("Batch Streaming", guiSkin_Label_H2);
                GUILayout.Label(string.Format("{0,-20}{1,-10}", "Loading", LargeWorldStreamer.main.debugNumBatchesLoading), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-20}{1,-10}", "Unloading", LargeWorldStreamer.main.debugNumBatchesUnloading), guiSkin_Label_ListItem);
                GUILayout.Label("Octree Compiler", guiSkin_Label_H2);
                GUILayout.Label(string.Format("{0,-20}{1,-10}", "ToCompile", LargeWorldStreamer.main.octCompiler.numRootsToCompile), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-20}{1,-10}", "Frozen", LargeWorldStreamer.main.octCompiler.debugFreeze ? "true" : "false"), guiSkin_Label_ListItem);
                GUILayout.Label("Cell Streaming", guiSkin_Label_H2);
                if (LargeWorldStreamer.main.cellManager != null)
                {
                    GUILayout.Label(string.Format("{0,-20}{1,-10}", "FreezeCount", LargeWorldStreamer.main.cellManager.GetFreezeCount()), guiSkin_Label_ListItem);
                    GUILayout.Label(string.Format("{0,-20}{1,-10}", "Queued", LargeWorldStreamer.main.cellManager.GetQueueLength()), guiSkin_Label_ListItem);
                }
                else
                {
                    GUILayout.Label("(Uninitialized)", guiSkin_Label_ListItem);
                }
                GUILayout.Label("Clip Streaming", guiSkin_Label_H2);
                if (LargeWorldStreamer.main.streamerV2 != null)
                {
                    GUILayout.Label(string.Format("{0,-20}{1,-10}", "Frozen", LargeWorldStreamer.main.streamerV2.IsFrozen() ? "true" : "false"), guiSkin_Label_ListItem);
                    GUILayout.Label(string.Format("{0,-20}{1,-10}", "Processing", LargeWorldStreamer.main.streamerV2.clipmapStreamer.GetQueueLength()), guiSkin_Label_ListItem);
                    GUILayout.Label(string.Format("{0,-20}{1,-10}", "Meshing", LargeWorldStreamer.main.streamerV2.clipmapStreamer.meshingThreads.GetQueueLength()), guiSkin_Label_ListItem);
                    GUILayout.Label(string.Format("{0,-20}{1,-10}", "Finalizing", LargeWorldStreamer.main.streamerV2.clipmapStreamer.buildLayersThread.GetQueueLength()), guiSkin_Label_ListItem);
                    GUILayout.Label(string.Format("{0,-20}{1,-10}", "Destroying", LargeWorldStreamer.main.streamerV2.clipmapStreamer.destroyChunksThread.GetQueueLength()), guiSkin_Label_ListItem);
                    GUILayout.Label(string.Format("{0,-20}{1,-10}", "Show/Hide", LargeWorldStreamer.main.streamerV2.clipmapStreamer.toggleChunksThread.GetQueueLength()), guiSkin_Label_ListItem);
                    GUILayout.Label(string.Format("{0,-20}{1,-10}", "Update Vis", LargeWorldStreamer.main.streamerV2.visibilityUpdater.GetQueueLength()), guiSkin_Label_ListItem);
                    GUILayout.BeginVertical(GUI.skin.box);
                    LargeWorldStreamer.main.streamerV2.DebugGUI();
                    GUILayout.EndVertical();
                }
                else
                {
                    GUILayout.Label("(Uninitialized)", guiSkin_Label_ListItem);
                }
                GUILayout.Label("Deferred Prefab Spawning", guiSkin_Label_H2);
                if (DeferredSpawner.instance != null)
                {
                    GUILayout.Label(string.Format("{0,-30}{1,-10}", "Pending Spawns", DeferredSpawner.instance.InstantiateQueueCount), guiSkin_Label_ListItem);
                }
            }
            else
            {
                GUILayout.Label("(Uninitialized)", guiSkin_Label_ListItem);
            }
            GUILayout.EndVertical();
        }

        private void DisplayDebugCommonByteArray()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("CommonByteArrayAllocator", guiSkin_Label_H1);
            DisplayArrayAllocator(" Large", CommonByteArrayAllocator.largeBlock, 2);
            DisplayArrayAllocator(" Small", CommonByteArrayAllocator.smallBlock, 1);
            GUILayout.EndVertical();
        }

        private void DisplayDebugTextureStreaming()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Texture Streaming", guiSkin_Label_H1);
            if (QualitySettings.streamingMipmapsActive)
            {
                GUILayout.Label(string.Format("{0,-32}{1,10}", "StreamedTextureCount", Texture.streamingTextureCount), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-32}{1,10}", "StreamedRendererCount", Texture.streamingRendererCount), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-32}{1,10}", "NonStreamedTextureCount", Texture.nonStreamingTextureCount), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-32}{1,10}", "StreamedLoadingCount", Texture.streamingTextureLoadingCount), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-32}{1,10}", "StreamedPendingCount", Texture.streamingTexturePendingLoadCount), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-32}{1,10}", "MaxRenderersPerFrame", QualitySettings.streamingMipmapsRenderersPerFrame), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-32}{1,10}", "MaxFileIORequests", QualitySettings.streamingMipmapsMaxFileIORequests), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-32}{1,10:0.0}MB", "StreamingBudget", QualitySettings.streamingMipmapsMemoryBudget), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-32}{1,10:0.0}MB", "CurrentTextureMemory", (float)Texture.currentTextureMemory / 1048576f), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-32}{1,10:0.0}MB", "DesiredTextureMemory", (float)Texture.desiredTextureMemory / 1048576f), guiSkin_Label_ListItem);
                GUILayout.Label(string.Format("{0,-32}{1,10:0.0}MB", "NonStreamedTextureMemory", (float)Texture.nonStreamingTextureMemory / 1048576f), guiSkin_Label_ListItem);
            }
            else
            {
                GUILayout.Label("(Disabled)", guiSkin_Label_ListItem);
            }
            GUILayout.EndVertical();
        }

        private void DisplayDebugObjectPools()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"Instantiate (Miss) {GameObjectPool.InstanceCount}");
            GUILayout.Label($"Instantiate (Hit) {GameObjectPool.HitCount}");
            GUILayout.Label($"Avg Instantiate Time(ms) {GameObjectPoolUtils.AvgInstantiateTime}");
            GUILayout.EndVertical();
        }

        private void DisplayDebugUpdaters()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            BehaviourUpdateManager behaviourUpdateManager = BehaviourUpdateManager.Instance;
            if ((bool)behaviourUpdateManager)
            {
                behaviourUpdateManager.DebugGUI();
            }
            UpdateScheduler updateScheduler = UpdateScheduler.Instance;
            if ((bool)updateScheduler)
            {
                updateScheduler.DebugGUI();
            }
            GUILayout.EndVertical();
        }

        private void DisplayDebugClipMapMeshBufferPools()
        {
            if (!(LargeWorldStreamer.main != null))
            {
                return;
            }
            ClipmapStreamer clipmapStreamer = LargeWorldStreamer.main.streamerV2.clipmapStreamer;
            if (clipmapStreamer == null)
            {
                return;
            }
            GUILayout.BeginVertical(GUI.skin.box);
            int num = 1;
            foreach (MeshBuilder item in clipmapStreamer.meshBuilderPool)
            {
                DisplayMeshBufferPools($"MeshPool{num++}", item.meshBufferPools);
            }
            GUILayout.EndVertical();
        }

        private void DisplayMeshBufferPools(string name, MeshBufferPools meshBufferPools)
        {
            GUILayout.Label(string.Format("{0,-10} {1,10} {2,10} {3,10}", name, "PeakInUse", "MaxSizeMB", "MaxInUseMB"), guiSkin_Label_H2);
            DisplayLinearHeap("Color32", meshBufferPools.c32);
            DisplayLinearHeap("Indices", meshBufferPools.ints);
            DisplayLinearHeap("Vector2", meshBufferPools.v2);
            DisplayLinearHeap("Vector3", meshBufferPools.v3);
            DisplayLinearHeap("Vector4", meshBufferPools.v4);
        }

        private void DisplayLinearHeap<T>(string name, LinearArrayHeap<T> heap)
        {
            float num = (float)(heap.ElementSize * heap.MaxSize) / 1048576f;
            float num2 = (float)(heap.ElementSize * heap.Highwater) / 1048576f;
            GUILayout.Label(string.Format("{0,-10} {1,10} {2,10:0.00} {3,10:0.00}", name, heap.PeakOutstanding, num, num2), guiSkin_Label_ListItem);
        }

        private void EnsureBuffers()
        {
            if (freeHeap == null)
            {
                freeHeap = new int[1024];
            }
            if (inUseHeap == null)
            {
                inUseHeap = new int[1024];
            }
            if (wasteHeap == null)
            {
                wasteHeap = new long[1024];
            }
            if (peakHeap == null)
            {
                peakHeap = new int[1024];
            }
            if (pageFree == null)
            {
                pageFree = new int[128];
            }
            if (pageUse == null)
            {
                pageUse = new int[128];
            }
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        [SuppressMessage("Subnautica.Rules", "AvoidStringConcatenation")]
        private void DisplayArrayPool<T>(string name, IArrayPool<T> pool, int orderOfMagnitude, int displayLimit)
        {
            EnsureBuffers();
            float num = 1f / Mathf.Pow(1024f, orderOfMagnitude);
            string text = string.Empty;
            switch (orderOfMagnitude)
            {
                case 0:
                    text = "B";
                    break;
                case 1:
                    text = "KB";
                    break;
                case 2:
                    text = "MB";
                    break;
                case 3:
                    text = "GB";
                    break;
            }
            GUILayout.Label(string.Format("{0,-13} {1,5} {2,5} {3,5} {4,8} {5,8} {6,8} {7,8}", name, "UsedN", "PeakN", "FreeN", "Used" + text, "Peak" + text, "Free" + text, "Waste" + text), guiSkin_Label_H2);
            int numBuckets = pool.numBuckets;
            if (numBuckets > 1024)
            {
                GUILayout.Label($"Exceeded Max Bucket Count {numBuckets}/{1024}");
                return;
            }
            pool.GetBucketInfo(ref freeHeap, ref inUseHeap, ref peakHeap, ref wasteHeap);
            int num2 = pool.elementSize * pool.bucketStride;
            int num3 = 0;
            int num4 = 0;
            int num5 = 0;
            float num6 = 0f;
            float num7 = 0f;
            float num8 = 0f;
            float num9 = 0f;
            for (int i = 0; i < numBuckets; i++)
            {
                int num10 = num2 * (i + 1);
                num3 += inUseHeap[i];
                num4 += freeHeap[i];
                num5 += peakHeap[i];
                num8 += (float)wasteHeap[i];
                num6 += (float)(num10 * inUseHeap[i]);
                num7 += (float)(num10 * freeHeap[i]);
                num9 += (float)(num10 * peakHeap[i]);
            }
            num8 *= num;
            num6 *= num;
            num7 *= num;
            num9 *= num;
            GUILayout.Label(string.Format("{0,-12} {1,5} {2,5} {3,5} {4,8:0.0} {5,8:0.0} {6,8:0.0} {7,8:0.0}", "Total", num3, num5, num4, num6, num9, num7, num8), guiSkin_Label_ListItem);
            int num11 = numBuckets / displayLimit + 1;
            for (int i = numBuckets - 1; i >= 0; i -= num11)
            {
                num3 = 0;
                num4 = 0;
                num5 = 0;
                num6 = 0f;
                num7 = 0f;
                num8 = 0f;
                num9 = 0f;
                int num12 = Mathf.Max(0, i - num11);
                for (int num13 = i; num13 > num12; num13--)
                {
                    int num14 = num2 * (num13 + 1);
                    num3 += inUseHeap[num13];
                    num4 += freeHeap[num13];
                    num5 += peakHeap[num13];
                    num8 += (float)wasteHeap[num13];
                    num6 += (float)(num14 * inUseHeap[num13]);
                    num7 += (float)(num14 * freeHeap[num13]);
                    num9 += (float)(num14 * peakHeap[num13]);
                }
                num8 *= num;
                num6 *= num;
                num7 *= num;
                num9 *= num;
                string text2 = ((i - num12 > 1) ? $"B[{num12 + 1}-{i}]" : $"B[{i}]");
                GUILayout.Label(string.Format("{0,-12} {1,5} {2,5} {3,5} {4,8:0.0} {5,8:0.0} {6,8:0.0} {7,8:0.0}", text2, num3, num5, num4, num6, num9, num7, num8), guiSkin_Label_ListItem);
            }
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        [SuppressMessage("Subnautica.Rules", "AvoidStringConcatenation")]
        private void DisplayArrayAllocator<T>(string name, ArrayAllocator<T> pool, int orderOfMagnitude)
        {
            GUILayout.Label(name, guiSkin_Label_H1);
            EnsureBuffers();
            int bucketCount = pool.BucketCount;
            if (bucketCount > 1024)
            {
                GUILayout.Label($"Exceeded Max Bucket Count {bucketCount}/{1024}");
                return;
            }
            pool.GetDebugInfo(freeHeap, inUseHeap, peakHeap, wasteHeap, pageUse, pageFree);
            float num = 1f / Mathf.Pow(1024f, orderOfMagnitude);
            string text = string.Empty;
            switch (orderOfMagnitude)
            {
                case 0:
                    text = "B";
                    break;
                case 1:
                    text = "KB";
                    break;
                case 2:
                    text = "MB";
                    break;
                case 3:
                    text = "GB";
                    break;
            }
            GUILayout.Label(string.Format("{0,-8} {1,8} {2,8} {3,8}", "NumPages", "InUse" + text, "Free" + text, "Max" + text), guiSkin_Label_H2);
            int num2 = 0;
            int num3 = 0;
            for (int i = 0; i < pool.PageCount; i++)
            {
                num2 += pageUse[i];
                num3 += pageFree[i];
            }
            GUILayout.Label(string.Format("{0,8} {1,8:0.00} {2,8:0.00} {3,8:0.00}", pool.PageCount, (float)(num2 * pool.ElementSize) * num, (float)(num3 * pool.ElementSize) * num, (float)(pool.PageCount * pool.PageSize * pool.ElementSize) * num), guiSkin_Label_ListItem);
            GUILayout.Space(4f);
            GUILayout.Label(string.Format("{0,-8} {1,5} {2,5} {3,5} {4,7} {5,7} {6,7} {7,7}", "Bucket", "InUse", "Peak", "Free", "InUse" + text, "Peak" + text, "Free" + text, "Waste" + text), guiSkin_Label_H2);
            int num4 = 0;
            int num5 = 0;
            int num6 = 0;
            int num7 = 0;
            int num8 = 0;
            long num9 = 0L;
            int num10 = 0;
            for (int j = 0; j < bucketCount; j++)
            {
                num4 += inUseHeap[j];
                num5 += peakHeap[j];
                num6 += freeHeap[j];
                num9 += pool.ElementSize * wasteHeap[j];
                num7 += pool.ElementSize * (inUseHeap[j] << j);
                num8 += pool.ElementSize * (freeHeap[j] << j);
                num10 += pool.ElementSize * (peakHeap[j] << j);
            }
            GUILayout.Label(string.Format("{0,-8} {1,5} {2,5} {3,5} {4,7:0.00} {5,7:0.00} {6,7:0.00} {7,7:0.00}", "Total", num4, num5, num6, (float)num7 * num, (float)num10 * num, (float)num8 * num, (float)num9 * num), guiSkin_Label_ListItem);
            for (int num11 = bucketCount - 1; num11 >= pool.MinBucketIndex; num11--)
            {
                if (peakHeap[num11] != 0)
                {
                    int num12 = 1 << num11;
                    string text2 = ((num12 > 1048576) ? $"B[{num12 / 1048576}MB]" : ((num12 <= 1024) ? $"B[{num12}]" : $"B[{num12 / 1024}KB]"));
                    GUILayout.Label(string.Format("{0,-8} {1,5} {2,5} {3,5} {4,7:0.00} {5,7:0.00} {6,7:0.00} {7,7:0.00}", text2, inUseHeap[num11], peakHeap[num11], freeHeap[num11], (float)(pool.ElementSize * (inUseHeap[num11] << num11)) * num, (float)(pool.ElementSize * (peakHeap[num11] << num11)) * num, (float)(pool.ElementSize * (freeHeap[num11] << num11)) * num, (float)(pool.ElementSize * wasteHeap[num11]) * num), guiSkin_Label_ListItem);
                }
            }
        }
    }
}
