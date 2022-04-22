using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using AssemblyCSharp.WorldStreaming;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(LargeWorldStreamer))]
    [RequireComponent(typeof(Voxeland))]
    [ExecuteInEditMode]
    public class LargeWorld : MonoBehaviour
    {
        public class Heightmap : UshortHeightmap, IVoxelGrid
        {
            public LargeWorld world;

            public static int MaskVersion;

            public Array3<bool> batch2baked;

            private Int2 cacheOrigin;

            private float[,] cache;

            public void LoadMaskThreaded(string path)
            {
                Int3 batchCount = world.streamer.batchCount;
                if (batch2baked == null || batch2baked.Dims() != batchCount)
                {
                    batch2baked = new Array3<bool>(batchCount.x, batchCount.y, batchCount.z);
                }
                batch2baked.Clear();
                if (!FileUtils.FileExists(path))
                {
                    return;
                }
                using StreamReader streamReader = FileUtils.ReadTextFile(path);
                int.Parse(streamReader.ReadLine());
                while (true)
                {
                    string text = streamReader.ReadLine();
                    if (text == null || text.Trim().Length == 0)
                    {
                        break;
                    }
                    Int3 p = Int3.Parse(text, ',');
                    batch2baked.Set(p, value: true);
                }
            }

            public void SaveMask(string path)
            {
                using StreamWriter streamWriter = FileUtils.CreateTextFile(path);
                int num = 0;
                streamWriter.WriteLine(MaskVersion);
                foreach (Int3 item in batch2baked.Indices())
                {
                    if (batch2baked.Get(item))
                    {
                        num++;
                        streamWriter.WriteLine(item);
                    }
                }
                Debug.Log("Saved " + num + " baked batches to " + path);
            }

            public bool IsMaskedOut(int x, int y, int z)
            {
                Int3 p = new Int3(x, y, z) / world.streamer.blocksPerBatch;
                if (batch2baked.CheckBounds(p))
                {
                    return batch2baked.Get(p);
                }
                return true;
            }

            public override byte GetType(int x, int y, int z)
            {
                if (IsMaskedOut(x, y, z))
                {
                    return 0;
                }
                return world.GetBlockType(new Int3(x, y, z));
            }

            public override bool GetMask(int x, int y, int z)
            {
                return !IsMaskedOut(x, y, z);
            }

            public void PrepareCache(Int2 mins, Int2 maxs)
            {
                mins -= 1;
                maxs += 1;
                Int2 @int = maxs - mins + 1;
                cacheOrigin = mins;
                cache = new float[@int.x, @int.y];
                Int2.RangeEnumerator enumerator = Int2.Range(mins, maxs).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Int2 current = enumerator.Current;
                    float height = GetHeight(current.x, current.y);
                    cache.Set(current - cacheOrigin, height);
                }
            }

            private bool IsAboveHeightmap(int x, int y, int z)
            {
                float num = SystemExtensions.Get(p: new Int2(x, z) - cacheOrigin, array: cache);
                return (float)y + 0.5f > num;
            }

            public VoxelandData.OctNode GetVoxel(int x, int y, int z)
            {
                if (!GetVoxelMask(x, y, z))
                {
                    return VoxelandData.OctNode.EmptyNode();
                }
                return new VoxelandData.OctNode(GetType(x, y, z), 0);
            }

            public bool GetVoxelMask(int x, int y, int z)
            {
                Int3 p = new Int3(x, y, z) / world.streamer.blocksPerBatch;
                if (batch2baked.Get(p))
                {
                    return false;
                }
                return !IsAboveHeightmap(x, y, z);
            }
        }

        public class BatchSignature
        {
            private Dictionary<string, byte[]> fileHashes = new Dictionary<string, byte[]>();

            private Dictionary<string, long> fileSizes = new Dictionary<string, long>();

            public int numFiles => fileHashes.Count;

            public void ProcessFile(string path)
            {
                string fileName = Path.GetFileName(path);
                if (fileHashes.ContainsKey(fileName))
                {
                    throw new Exception("Filename was fed twice: " + path);
                }
                using (Stream inputStream = FileUtils.ReadFile(path))
                {
                    byte[] value = hasher.ComputeHash(inputStream);
                    fileHashes[fileName] = value;
                }
                FileInfo fileInfo = new FileInfo(path);
                fileSizes[fileName] = fileInfo.Length;
            }

            private bool IsEqualToOneWay(BatchSignature other, bool ignoreSlots = false)
            {
                if (other.fileHashes.Count != fileHashes.Count)
                {
                    return false;
                }
                foreach (KeyValuePair<string, byte[]> fileHash in fileHashes)
                {
                    string key = fileHash.Key;
                    if (!ignoreSlots || !CellManager.IsSlotsFile(key))
                    {
                        if (!other.fileHashes.ContainsKey(fileHash.Key))
                        {
                            return false;
                        }
                        if (other.fileSizes[fileHash.Key] != fileSizes[fileHash.Key])
                        {
                            return false;
                        }
                        byte[] b = other.fileHashes[fileHash.Key];
                        if (!fileHash.Value.IsEqualTo(b))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            public bool IsEqualTo(BatchSignature other, bool ignoreSlots = false)
            {
                if (other.IsEqualToOneWay(this, ignoreSlots))
                {
                    return IsEqualToOneWay(other, ignoreSlots);
                }
                return false;
            }

            public void Write(TextWriter w)
            {
                w.Write(fileHashes.Count + "\n");
                foreach (KeyValuePair<string, byte[]> fileHash in fileHashes)
                {
                    w.Write(fileHash.Key + "\n");
                    w.Write(fileSizes[fileHash.Key] + "\n");
                    w.Write(fileHash.Value.Length + "\n");
                    for (int i = 0; i < fileHash.Value.Length; i++)
                    {
                        w.Write(fileHash.Value[i] + "\n");
                    }
                }
            }

            public void Read(TextReader r)
            {
                fileHashes.Clear();
                int num = int.Parse(r.ReadLine());
                for (int i = 0; i < num; i++)
                {
                    string key = r.ReadLine().Trim();
                    long value = long.Parse(r.ReadLine());
                    fileSizes[key] = value;
                    int num2 = int.Parse(r.ReadLine());
                    byte[] array = new byte[num2];
                    for (int j = 0; j < num2; j++)
                    {
                        array[j] = byte.Parse(r.ReadLine());
                    }
                    fileHashes[key] = array;
                }
            }
        }

        public class WorldSignature
        {
            private readonly Dictionary<Int3, BatchSignature> batchSigs = new Dictionary<Int3, BatchSignature>();

            public void Compute(string worldDir)
            {
                using (new ScopeTiming("WorldSignature.Compute(" + worldDir + ")"))
                {
                    string[] files = FileUtils.GetFiles(Path.Combine(worldDir, "BatchOctrees"));
                    foreach (string text in files)
                    {
                        if (LargeWorld.onActivity != null)
                        {
                            LargeWorld.onActivity(this, null);
                        }
                        if (TryExtractBatchNumber(text, out var batchOut))
                        {
                            if (!batchSigs.ContainsKey(batchOut))
                            {
                                batchSigs[batchOut] = new BatchSignature();
                            }
                            batchSigs[batchOut].ProcessFile(text);
                        }
                    }
                }
            }

            public void Write(TextWriter w)
            {
                w.Write(batchSigs.Count + "\n");
                foreach (KeyValuePair<Int3, BatchSignature> batchSig in batchSigs)
                {
                    w.Write(string.Concat(batchSig.Key, "\n"));
                    batchSig.Value.Write(w);
                }
            }

            public void Read(TextReader r)
            {
                batchSigs.Clear();
                int num = int.Parse(r.ReadLine());
                for (int i = 0; i < num; i++)
                {
                    Int3 @int = Int3.Parse(r.ReadLine(), ',');
                    if (batchSigs.ContainsKey(@int))
                    {
                        throw new Exception(string.Concat("Batch ", @int, " shows up multiple tiles in input stream!"));
                    }
                    BatchSignature batchSignature = new BatchSignature();
                    batchSignature.Read(r);
                    batchSigs[@int] = batchSignature;
                }
            }

            public HashSet<Int3> ComputeDifferingBatches(WorldSignature other, bool ignoreSlots = false)
            {
                HashSet<Int3> hashSet = new HashSet<Int3>();
                foreach (KeyValuePair<Int3, BatchSignature> batchSig in batchSigs)
                {
                    if (!other.batchSigs.ContainsKey(batchSig.Key))
                    {
                        hashSet.Add(batchSig.Key);
                    }
                }
                foreach (KeyValuePair<Int3, BatchSignature> batchSig2 in other.batchSigs)
                {
                    if (!batchSigs.ContainsKey(batchSig2.Key))
                    {
                        hashSet.Add(batchSig2.Key);
                    }
                }
                foreach (KeyValuePair<Int3, BatchSignature> batchSig3 in batchSigs)
                {
                    Int3 key = batchSig3.Key;
                    BatchSignature orDefault = other.batchSigs.GetOrDefault(key, null);
                    if (orDefault != null && !batchSig3.Value.IsEqualTo(orDefault, ignoreSlots))
                    {
                        hashSet.Add(key);
                    }
                }
                return hashSet;
            }
        }

        public class BiomeVoxelGrid : IVoxelGrid
        {
            private LargeWorld world;

            public BiomeVoxelGrid(LargeWorld world)
            {
                this.world = world;
            }

            public VoxelandData.OctNode GetVoxel(int x, int y, int z)
            {
                return new VoxelandData.OctNode(world.GetBlockType(new Int3(x, y, z), checkHeight: false, debug: false), 0);
            }

            public bool GetVoxelMask(int x, int y, int z)
            {
                return world.streamer.debugBiomeDithering switch
                {
                    1 => ((x + z) & 1) == 0, 
                    2 => ((x + 2 * z) & 3) == 0, 
                    _ => true, 
                };
            }
        }

        public static LargeWorld main;

        public static int MetaVersion = 39;

        public static int HeightLockedBatchesVersion = 0;

        [HideInInspector]
        public string dataDir;

        [HideInInspector]
        public string paletteDir;

        [HideInInspector]
        public string fallbackDir;

        [NonSerialized]
        public LargeWorldStreamer streamer;

        [NonSerialized]
        public Voxeland land;

        [NonSerialized]
        public bool isEditing;

        [NonSerialized]
        public bool worldMounted;

        [NonSerialized]
        public string state = "uninit";

        [NonSerialized]
        public bool editingWindow;

        public bool loadingWindow = true;

        [NonSerialized]
        public float entitySlotsDrawDistance = 100f;

        [NonSerialized]
        public GameObject helperTerrainObj;

        [NonSerialized]
        public TerrainData helperTerrainData;

        [NonSerialized]
        public Int2 helperTerrainOffset;

        private GameObject proceduralSlotsRoot;

        public Int3.Bounds batchWindow = new Int3.Bounds(Int3.zero, Int3.zero);

        private Dictionary<GameObject, Int3> lockedBox2batch = new Dictionary<GameObject, Int3>();

        public GameObject lockedBoxRoot;

        private Dictionary<GameObject, Int3> existing2batch = new Dictionary<GameObject, Int3>();

        public GameObject existingRoot;

        public SignalDatabase signalDatabase;

        private GameObject signalsRoot;

        public Int3.Bounds loadedBatchWindow = new Int3.Bounds(Int3.zero, Int3.zero);

        [NonSerialized]
        public DateTime lastSaveDT = DateTime.MinValue;

        [NonSerialized]
        public HashSet<Int3> existingBatches = new HashSet<Int3>(Int3.equalityComparer);

        [NonSerialized]
        public Heightmap heightmap;

        [NonSerialized]
        public Dictionary<Int3, BiomeProperties> biomeMapLegend;

        [NonSerialized]
        public Color32[] biomeMap;

        [NonSerialized]
        public int biomeMapWidth;

        [NonSerialized]
        public int biomeMapHeight;

        [NonSerialized]
        private int biomeDownFactor;

        public HashSet<Int2> heightLockedBatches = new HashSet<Int2>(Int2.equalityComparer);

        private GameObject _batchSelectorPrefab;

        private GameObject _bakedBatchSelectorPrefab;

        private GameObject _heightlockedHighlightPrefab;

        private static SHA256Managed hasher = new SHA256Managed();

        public Int3.Bounds blockWindow => new Int3.Bounds(batchWindow.mins * streamer.blocksPerBatch, (batchWindow.maxs + 1) * streamer.blocksPerBatch - 1);

        public Bounds wsBounds
        {
            get
            {
                Bounds result = new Bounds(Vector3.zero, Vector3.zero);
                result.SetMinMax(land.transform.TransformPoint(blockWindow.mins.ToVector3()), land.transform.TransformPoint((blockWindow.maxs + 1).ToVector3()));
                return result;
            }
        }

        public string heightmapPath => Path.Combine(fallbackDir, "heightmap.r16");

        public string legendColorsPath => Path.Combine(fallbackDir, "legendColors.png");

        public string biomesCSVPath => Path.Combine(fallbackDir, "biomes.csv");

        public string biomeMapPath => Path.Combine(fallbackDir, "biomeMap.png");

        public string heightLockedBatchesPath => Path.Combine(fallbackDir, "heightLockedBatches.txt");

        public string heightBakedBatchesPath => Path.Combine(fallbackDir, "heightBakedBatches.txt");

        public string lastSavedAgoString
        {
            get
            {
                if (lastSaveDT == DateTime.MinValue)
                {
                    return "Never!";
                }
                return Mathf.FloorToInt((float)(DateTime.Now - lastSaveDT).TotalSeconds) + " seconds ago";
            }
        }

        private GameObject batchSelectorPrefab
        {
            get
            {
                if (_batchSelectorPrefab == null)
                {
                    _batchSelectorPrefab = Resources.Load("WorldEditor/BatchSelector") as GameObject;
                }
                return _batchSelectorPrefab;
            }
        }

        private GameObject bakedBatchSelectorPrefab
        {
            get
            {
                if (_bakedBatchSelectorPrefab == null)
                {
                    _bakedBatchSelectorPrefab = Resources.Load("WorldEditor/BakedBatchSelector") as GameObject;
                }
                return _bakedBatchSelectorPrefab;
            }
        }

        private GameObject heightlockedHighlightPrefab
        {
            get
            {
                if (_heightlockedHighlightPrefab == null)
                {
                    _heightlockedHighlightPrefab = Resources.Load("WorldEditor/HeightLockedBatchHighlight") as GameObject;
                }
                return _heightlockedHighlightPrefab;
            }
        }

        public static event EventHandler onActivity;

        public void SaveEditorBatchWindowSettings()
        {
        }

        public void LoadEditorBatchWindowSettings()
        {
        }

        public bool Mounted()
        {
            return state == "mounted";
        }

        public Bounds BiomeDataBounds()
        {
            Vector3 position = land.transform.position;
            Vector3 max = position + new Vector3(biomeMapWidth, 0f, biomeMapHeight) * biomeDownFactor;
            Bounds result = default(Bounds);
            result.SetMinMax(position, max);
            return result;
        }

        public bool IsObjectSelector(GameObject obj)
        {
            if (!lockedBox2batch.ContainsKey(obj))
            {
                return existing2batch.ContainsKey(obj);
            }
            return true;
        }

        public Int3 GetBatchForSelector(GameObject obj)
        {
            if (lockedBox2batch.ContainsKey(obj))
            {
                return lockedBox2batch[obj];
            }
            if (existing2batch.ContainsKey(obj))
            {
                return existing2batch[obj];
            }
            return Int3.zero;
        }

        public List<Int3> GetExistingBatchesAsShuffledList()
        {
            List<Int3> list = new List<Int3>();
            foreach (Int3 existingBatch in existingBatches)
            {
                list.Add(existingBatch);
            }
            list.Shuffle();
            return list;
        }

        public byte GetBlockType(Int3 block, bool checkHeight = true, bool debug = true)
        {
            BiomeProperties biomeProperties = GetBiomeProperties(block.xz);
            if (biomeProperties == null)
            {
                return 0;
            }
            int num = VoxelandUtils.TopBlockForHeight(heightmap.GetHeight(block.x, block.z));
            if (checkHeight && block.y > num)
            {
                return 0;
            }
            if (streamer.debugBiomeMaterials)
            {
                return Convert.ToByte(biomeProperties.debugType);
            }
            if (block.y > num - 16)
            {
                return Convert.ToByte(biomeProperties.groundType);
            }
            return Convert.ToByte(biomeProperties.bedrockType);
        }

        public void RasterHeightmapTypes(Int3 origin, Array3<byte> dest)
        {
            dest.Clear();
            if (biomeMap == null)
            {
                return;
            }
            Int3 @int = origin;
            Int3 int2 = origin + dest.Dims() - 1;
            Int2.RangeEnumerator enumerator = Int2.Range(@int.xz, int2.xz).GetEnumerator();
            while (enumerator.MoveNext())
            {
                Int2 current = enumerator.Current;
                BiomeProperties biomeProperties = GetBiomeProperties(current);
                if (biomeProperties != null)
                {
                    int num = VoxelandUtils.TopBlockForHeight(heightmap.GetHeight(current.x, current.y));
                    for (int i = @int.y; i <= int2.y; i++)
                    {
                        Int3 int3 = new Int3(current.x, i, current.y);
                        byte b = 0;
                        SystemExtensions.Set(value: (byte)((i <= num) ? ((!streamer.debugBiomeMaterials) ? ((int3.y <= num - 16) ? Convert.ToByte(biomeProperties.bedrockType) : Convert.ToByte(biomeProperties.groundType)) : Convert.ToByte(biomeProperties.debugType)) : 0), array: dest, p: int3 - origin);
                    }
                }
            }
        }

        public Color DebugBiomeMap(Int3 block)
        {
            if (biomeMap == null)
            {
                return new Color(0f, 0f, 0f, 0f);
            }
            int num = block.z / biomeDownFactor * biomeMapWidth + block.x / biomeDownFactor;
            if (num >= biomeMap.Length || num < 0)
            {
                return new Color(0f, 0f, 0f, 0f);
            }
            return biomeMap[num];
        }

        public BiomeProperties GetBiomeProperties(Int2 blockXZ)
        {
            if (biomeMap == null)
            {
                return null;
            }
            int num = blockXZ.y / biomeDownFactor * biomeMapWidth + blockXZ.x / biomeDownFactor;
            if (num >= biomeMap.Length || num < 0)
            {
                return null;
            }
            Int3 @int = biomeMap[num].ToInt3();
            if (!biomeMapLegend.TryGetValue(@int, out var value))
            {
                Debug.LogWarningFormat("There is a color in the biomeMap that is not in the legend. Color {0}, block {1}", @int, blockXZ);
                return null;
            }
            return value;
        }

        public string GetBiome(Int3 block)
        {
            string overrideBiome = streamer.GetOverrideBiome(block);
            if (overrideBiome != null)
            {
                return overrideBiome;
            }
            return GetBiomeProperties(block.xz)?.name;
        }

        public string GetBiomeOnHeightMap(Vector3 wsPos, int yTolerance)
        {
            Int3 block = Int3.Floor(land.transform.InverseTransformPoint(wsPos));
            string result = GetBiome(block);
            int num = Mathf.FloorToInt(heightmap.GetHeight(block.x, block.z));
            if (System.Math.Abs(block.y - num) > yTolerance)
            {
                result = "";
            }
            return result;
        }

        public float GetHeightMapY(Vector3 wsPos)
        {
            Int3 @int = Int3.Floor(land.transform.InverseTransformPoint(wsPos));
            return heightmap.GetHeight(@int.x, @int.z);
        }

        public string GetBiome(Vector3 wsPos)
        {
            if (state != "mounted")
            {
                return null;
            }
            Int3 block = Int3.Floor(land.transform.InverseTransformPoint(wsPos));
            return GetBiome(block);
        }

        private void Awake()
        {
            main = this;
        }

        private void OnDestroy()
        {
            main = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (state == "mounted" && land.data != null)
            {
                Gizmos.color = ((batchWindow == loadedBatchWindow) ? Color.white : Color.yellow);
                Vector3 vector = base.transform.TransformPoint(blockWindow.mins.ToVector3());
                Vector3 vector2 = base.transform.TransformPoint(blockWindow.maxs.ToVector3());
                Vector3 center = (vector + vector2) * 0.5f;
                Vector3 size = vector2 - vector;
                Gizmos.DrawWireCube(center, size);
            }
        }

        public static bool IsValidWorldDir(string dir)
        {
            return File.Exists(Path.Combine(dir, "meta.txt"));
        }

        public static void CreateWorld(string path, string paletteDir, Int3 size)
        {
            if (Directory.Exists(path))
            {
                Debug.Log("WARNING: Overwriting (ie. deleting) world directory " + path);
                Directory.Delete(path, recursive: true);
            }
            Directory.CreateDirectory(path);
            StreamWriter streamWriter = FileUtils.CreateTextFile(Path.Combine(path, "meta.txt"));
            streamWriter.WriteLine(MetaVersion);
            streamWriter.WriteLine(paletteDir);
            streamWriter.Close();
            VoxelandData voxelandData = ScriptableObject.CreateInstance<VoxelandData>();
            voxelandData.ClearToNothing(size.x, size.y, size.z, 32);
            LargeWorldStreamer.CreateStreamableCache(treesPerBatch: new Int3(5, 5, 5), pathPrefix: path + "/", data: voxelandData);
            if (Application.isEditor)
            {
                global::UnityEngine.Object.DestroyImmediate(voxelandData);
            }
            else
            {
                global::UnityEngine.Object.Destroy(voxelandData);
            }
        }

        public static bool CheckWorld(string dir, ref string paletteDir)
        {
            if (!Directory.Exists(dir))
            {
                Debug.Log("Directory does not exist: " + dir);
                return false;
            }
            int num = -1;
            using (StreamReader streamReader = FileUtils.ReadTextFile(Path.Combine(dir, "meta.txt")))
            {
                num = int.Parse(streamReader.ReadLine());
                Debug.Log("World in " + dir + " is from version " + num);
                if (paletteDir != null)
                {
                    paletteDir = streamReader.ReadLine().Trim();
                }
            }
            return true;
        }

        public void UnmountWorld()
        {
            if (state == "mounted")
            {
                if (land.data != null)
                {
                    streamer.Deinitialize();
                    land.Rebuild();
                    global::UnityEngine.Object.DestroyImmediate(land.data);
                }
                UnloadWindow();
                UnloadHeightLockedBatches();
                if (existingRoot != null)
                {
                    global::UnityEngine.Object.DestroyImmediate(existingRoot);
                }
                DestroyHelperHeightmap();
                signalDatabase = null;
                global::UWE.Utils.DestroyWrap(signalsRoot);
                state = "uninit";
                isEditing = false;
                worldMounted = false;
            }
        }

        public bool MountWorld(string dataDir, string fallbackDir, LargeWorldStreamer streamer, Voxeland land)
        {
            return MountWorld(dataDir, fallbackDir, streamer, null, land);
        }

        public bool MountWorld(string dataDir, string fallbackDir, LargeWorldStreamer streamer, WorldStreamer streamerV2, Voxeland land)
        {
            CoroutineTask<Result> coroutineTask = MountWorldAsync(dataDir, fallbackDir, streamer, streamerV2, land);
            CoroutineUtils.PumpCoroutine(coroutineTask);
            return coroutineTask.GetResult().success;
        }

        public CoroutineTask<Result> MountWorldAsync(string dataDir, string fallbackDir, LargeWorldStreamer streamer, WorldStreamer streamerV2, Voxeland land)
        {
            TaskResult<Result> result = new TaskResult<Result>();
            return new CoroutineTask<Result>(MountWorldAsync(dataDir, fallbackDir, streamer, streamerV2, land, result), result);
        }

        private IEnumerator MountWorldAsync(string dataDir, string fallbackDir, LargeWorldStreamer streamer, WorldStreamer streamerV2, Voxeland land, IOut<Result> result)
        {
            if (state == "uninit")
            {
                if (!CheckWorld(fallbackDir, ref paletteDir))
                {
                    Debug.LogFormat(this, "CheckWorld failed. Frame {0}.", Time.frameCount);
                    result.Set(Result.Failure("CheckWorldFailure"));
                    yield break;
                }
                Debug.LogFormat(this, "LargeWorld: Loading world. Frame {0}.", Time.frameCount);
                main = this;
                this.dataDir = dataDir;
                this.land = land;
                this.streamer = streamer;
                this.fallbackDir = fallbackDir;
                if (paletteDir != "")
                {
                    this.land.paletteResourceDir = paletteDir;
                }
                Debug.LogFormat(this, "LargeWorld land '{0}'", land);
                streamer.Deinitialize();
                land.data = ScriptableObject.CreateInstance<VoxelandData>();
                Timer.Begin("Streamer initialize");
                Result value;
                try
                {
                    value = streamer.Initialize(streamerV2, land, dataDir, fallbackDir);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                    value = Result.Failure(ex.Message);
                }
                Timer.End();
                streamer.DNEType = 0;
                streamer.frozen = true;
                if (!value.success)
                {
                    result.Set(value);
                    yield break;
                }
                Timer.Begin("cellManager.ResetEntityDistributions");
                streamer.cellManager.ResetEntityDistributions();
                Timer.End();
                yield return null;
                if (Application.isPlaying)
                {
                    Timer.Begin("LoadSceneObjects");
                    streamer.LoadSceneObjects();
                    Timer.End();
                    yield return null;
                    Timer.Begin("LoadGlobalRoot");
                    yield return streamer.LoadGlobalRootAsync();
                    Timer.End();
                    yield return null;
                }
                if (land.paletteResourceDir != "")
                {
                    Debug.LogFormat(this, "loading palette for LargeWorld at '{0}'.", land.paletteResourceDir);
                    Timer.Begin("Loading palette");
                    land.LoadPalette(land.paletteResourceDir);
                    Timer.End();
                    yield return null;
                }
                else
                {
                    Debug.Log("no custom palette dir specified - leaving palette alone.");
                }
                if (!Application.isPlaying && FileUtils.FileExists(heightmapPath))
                {
                    Timer.Begin("Loading heightmap");
                    heightmap = new Heightmap();
                    heightmap.world = this;
                    yield return WorkerTask.Launch(delegate
                    {
                        heightmap.LoadWorldMachineU16Threaded(heightmapPath, land.data.GetSize());
                    });
                    Timer.End();
                    yield return WorkerTask.Launch(LoadHeightLockedBatchesThreaded);
                    FinalizeLoadHeightLockedBatches();
                    yield return WorkerTask.Launch(delegate
                    {
                        heightmap.LoadMaskThreaded(heightBakedBatchesPath);
                    });
                }
                if (FileUtils.FileExists(biomeMapPath))
                {
                    Timer.Begin("Loading biome map");
                    InitializeBiomeMap();
                    Timer.End();
                    yield return null;
                }
                land.disableAutoSerialize = true;
                land.readOnly = false;
                land.dynamicRebuilding = true;
                Debug.LogFormat(this, "LargeWorld: calling land.Rebuild frame {0}", Time.frameCount);
                Timer.Begin("Rebuilding land");
                land.Rebuild();
                Timer.End();
                yield return null;
                if ((bool)streamerV2)
                {
                    WorldStreamer.Settings settings = new WorldStreamer.Settings
                    {
                        worldPath = fallbackDir,
                        numOctrees = land.data.GetNodeCount(),
                        numOctreesPerBatch = streamer.treesPerBatch.x,
                        octreeSize = land.data.biggestNode
                    };
                    streamerV2.Start(land.types, settings);
                    streamerV2.clipmapStreamer.RegisterListener(streamer.cellManager);
                    yield return null;
                }
                state = "mounted";
                worldMounted = true;
                LoadEditorBatchWindowSettings();
                result.Set(Result.Success());
            }
            else
            {
                Debug.LogWarningFormat(this, "Can not mount world in state {0}.", state);
                result.Set(Result.Failure("WorldAlreadyMounted"));
            }
        }

        public static Dictionary<Int3, BiomeProperties> LoadBiomeMapLegend(string legendColorsPath, string biomesCSVPath)
        {
            byte[] data = File.ReadAllBytes(legendColorsPath);
            Texture2D obj = new Texture2D(1, 1, TextureFormat.ARGB32, mipChain: false, linear: true)
            {
                name = "LargeWorld.LoadBiomeMap.LegendColors"
            };
            obj.LoadImage(data);
            Color32[] pixels = obj.GetPixels32();
            List<BiomeProperties> list = CSVUtils.Load<BiomeProperties>(biomesCSVPath);
            Dictionary<Int3, BiomeProperties> dictionary = new Dictionary<Int3, BiomeProperties>(Int3.equalityComparer);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].name != list[i].name.Trim())
                {
                    Debug.LogErrorFormat("In file '{0}' has non-trimmed name: '{1}'", biomesCSVPath, list[i].name);
                }
                Int3 key = Int3.FromRGB(pixels[list.Count - i - 1]);
                dictionary[key] = list[i];
            }
            return dictionary;
        }

        public static Color32[] LoadBiomeMap(string biomeMapPath, int mipLevel, out int mapWidth, out int mapHeight)
        {
            ProfilingUtils.BeginSample("Read all bytes biome map");
            byte[] data = File.ReadAllBytes(biomeMapPath);
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("create biome map texture");
            Texture2D texture2D = new Texture2D(1, 1, TextureFormat.ARGB32, mipChain: false, linear: true);
            texture2D.name = "LargeWorld.LoadBiomeMap.BiomeMapTex";
            texture2D.LoadImage(data);
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("Create threadsafe biome map");
            Color32[] array = texture2D.GetPixels32();
            mapWidth = texture2D.width;
            mapHeight = texture2D.height;
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("Destroy biome map texture");
            global::UWE.Utils.DestroyWrap(texture2D);
            ProfilingUtils.EndSample();
            if (mipLevel > 0)
            {
                ProfilingUtils.BeginSample("Downsample biome map");
                Color32[] array2 = array;
                int num = mapWidth;
                int num2 = mapHeight;
                mapWidth = num >> mipLevel;
                mapHeight = num2 >> mipLevel;
                array = new Color32[mapWidth * mapHeight];
                for (int i = 0; i < mapWidth; i++)
                {
                    int num3 = i << mipLevel;
                    for (int j = 0; j < mapHeight; j++)
                    {
                        int num4 = j << mipLevel;
                        int num5 = num3 + num4 * num;
                        int num6 = i + j * mapWidth;
                        array[num6] = array2[num5];
                    }
                }
                ProfilingUtils.EndSample();
            }
            return array;
        }

        public void InitializeBiomeMap()
        {
            int mipLevel = (Application.isPlaying ? 2 : 0);
            biomeMap = LoadBiomeMap(biomeMapPath, mipLevel, out biomeMapWidth, out biomeMapHeight);
            biomeDownFactor = land.data.sizeX / biomeMapWidth;
            Debug.LogFormat("biome map downsample factor: {0}", biomeDownFactor);
            biomeMapLegend = LoadBiomeMapLegend(legendColorsPath, biomesCSVPath);
        }

        public void OnEntityMoved(UniqueIdentifier ent)
        {
            if (!(state != "mounted"))
            {
                streamer.cellManager.OnEntityMoved(ent);
            }
        }

        public void LoadWindow(bool progressBar = true)
        {
            if (state == "mounted")
            {
                Debug.Log("Loading window, wsBounds = " + wsBounds);
                loadingWindow = true;
                streamer.ForceUnloadAll();
                streamer.LoadBatchesForEdit(blockWindow, progressBar);
                loadingWindow = false;
                editingWindow = true;
                lastSaveDT = DateTime.Now;
                if (lockedBoxRoot != null)
                {
                    lockedBoxRoot.SetActive(value: false);
                }
                if (existingRoot != null)
                {
                    existingRoot.SetActive(value: false);
                }
                DestroyHelperHeightmap();
                land.meshMins = blockWindow.mins;
                land.meshMaxs = blockWindow.maxs;
            }
        }

        public int HeightmapLockWindow()
        {
            int count = heightLockedBatches.Count;
            Int2.RangeEnumerator enumerator = Int2.Range(batchWindow.mins.XZ(), batchWindow.maxs.XZ()).GetEnumerator();
            while (enumerator.MoveNext())
            {
                Int2 current = enumerator.Current;
                heightLockedBatches.Add(current);
            }
            SaveHeightLockedBatches();
            UnloadHeightLockedBatches();
            LoadHeightLockedBatches();
            return heightLockedBatches.Count - count;
        }

        public int HeightmapUnlockWindow()
        {
            int count = heightLockedBatches.Count;
            Int2.RangeEnumerator enumerator = Int2.Range(batchWindow.mins.XZ(), batchWindow.maxs.XZ()).GetEnumerator();
            while (enumerator.MoveNext())
            {
                Int2 current = enumerator.Current;
                if (heightLockedBatches.Contains(current))
                {
                    heightLockedBatches.Remove(current);
                }
            }
            SaveHeightLockedBatches();
            UnloadHeightLockedBatches();
            LoadHeightLockedBatches();
            return count - heightLockedBatches.Count;
        }

        public void SaveWindowCopy(string copyPathPrefix)
        {
            string pathPrefix = streamer.pathPrefix;
            string fallbackPrefix = streamer.fallbackPrefix;
            try
            {
                streamer.SetPathPrefix(copyPathPrefix, copyPathPrefix);
                PerformEditorSave(isAutosave: true);
            }
            finally
            {
                streamer.SetPathPrefix(pathPrefix, fallbackPrefix);
            }
        }

        public void UnloadWindow()
        {
            if (state == "mounted" && editingWindow)
            {
                Timer.Begin("ForceUnloadAll", 5f);
                streamer.ForceUnloadAll();
                Timer.End();
                Timer.Begin("DestroyAllChunks", 5f);
                land.DestroyAllChunks();
                Timer.End();
                editingWindow = false;
                Timer.Begin("Toggle Selectors", 5f);
                if (lockedBoxRoot != null)
                {
                    lockedBoxRoot.SetActive(value: true);
                }
                if (existingRoot != null)
                {
                    existingRoot.SetActive(value: true);
                }
                Timer.End();
                Timer.Begin("DestroyHelperHeightmap", 5f);
                DestroyHelperHeightmap();
                Timer.End();
                if (proceduralSlotsRoot != null)
                {
                    global::UWE.Utils.DestroyWrap(proceduralSlotsRoot);
                }
            }
        }

        public static bool TryExtractBatchNumber(string filename, out Int3 batchOut)
        {
            batchOut = new Int3(-1);
            if (!filename.Contains("batch"))
            {
                return false;
            }
            return global::UWE.Utils.TryParseBatchNumber(filename, out batchOut);
        }

        public void RefreshExistingBatchesForPlayMode()
        {
            existingBatches.Clear();
            string[] files = FileUtils.GetFiles(Path.Combine(fallbackDir, "CompiledOctreesCache"));
            for (int i = 0; i < files.Length; i++)
            {
                if (TryExtractBatchNumber(files[i], out var batchOut))
                {
                    existingBatches.Add(batchOut);
                }
            }
        }

        public void RefreshSignals()
        {
            global::UWE.Utils.DestroyWrap(signalsRoot);
            try
            {
                signalDatabase.Load(dataDir, fallbackDir);
                if (!Application.isPlaying)
                {
                    signalsRoot = signalDatabase.SpawnEditorPreview();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public void RefreshExistingBatches(bool skipSelectorObjects = false)
        {
            if (Application.isPlaying)
            {
                return;
            }
            existingBatches.Clear();
            if (existingRoot == null)
            {
                existingRoot = new GameObject("Existing Batches Selectors");
                existingRoot.transform.position = Vector3.zero;
                existingRoot.hideFlags = HideFlags.NotEditable;
            }
            string[] files = FileUtils.GetFiles(Path.Combine(dataDir, "BatchOctrees"));
            foreach (string text in files)
            {
                if (text.LastIndexOf("batch") == -1 || !TryExtractBatchNumber(text, out var batchOut))
                {
                    continue;
                }
                existingBatches.Add(batchOut);
                bool flag = false;
                if (heightmap != null)
                {
                    if (!heightmap.batch2baked.CheckBounds(batchOut))
                    {
                        Int3 zero = Int3.zero;
                        Int3 @int = heightmap.batch2baked.Dims() - 1;
                        Debug.LogError(string.Concat("Batch ", batchOut, " file ", text, " is out of bounds [(", zero, "), (", @int, ")]"), this);
                        continue;
                    }
                    if (heightmap.batch2baked.Get(batchOut))
                    {
                        flag = true;
                    }
                }
                if (!skipSelectorObjects)
                {
                    GameObject gameObject = global::UnityEngine.Object.Instantiate(flag ? bakedBatchSelectorPrefab : batchSelectorPrefab, streamer.GetBatchCenter(batchOut), Quaternion.identity);
                    gameObject.transform.localScale = streamer.blocksPerBatch.ToVector3() * 0.3f;
                    gameObject.transform.parent = existingRoot.transform;
                    gameObject.name = "Existing batch " + batchOut;
                    gameObject.hideFlags = HideFlags.NotEditable;
                    existing2batch[gameObject] = batchOut;
                }
            }
            Debug.Log("Found " + existingBatches.Count + " existing batches in " + dataDir);
        }

        public HashSet<Int3> GetAllAuthorAffectedBatches()
        {
            HashSet<Int3> hashSet = new HashSet<Int3>();
            foreach (Int3 existingBatch in existingBatches)
            {
                foreach (Int3 item in Int3.Range(existingBatch - 1, existingBatch + 1))
                {
                    if (streamer.CheckBatch(item))
                    {
                        hashSet.Add(item);
                    }
                }
            }
            return hashSet;
        }

        private void LoadHeightLockedBatches()
        {
            LoadHeightLockedBatchesThreaded();
            FinalizeLoadHeightLockedBatches();
        }

        private void LoadHeightLockedBatchesThreaded()
        {
            heightLockedBatches.Clear();
            if (heightmap == null || !FileUtils.FileExists(heightLockedBatchesPath))
            {
                return;
            }
            using StreamReader streamReader = FileUtils.ReadTextFile(heightLockedBatchesPath);
            int.Parse(streamReader.ReadLine());
            while (true)
            {
                string text = streamReader.ReadLine();
                if (text == null || text.Trim() == "")
                {
                    break;
                }
                Int2 item = Int2.Parse(text);
                heightLockedBatches.Add(item);
            }
        }

        private void FinalizeLoadHeightLockedBatches()
        {
            if (Application.isPlaying)
            {
                return;
            }
            if (lockedBoxRoot == null)
            {
                lockedBoxRoot = new GameObject("Locked Batches Selectors");
                lockedBoxRoot.transform.position = Vector3.zero;
                lockedBoxRoot.hideFlags = HideFlags.NotEditable;
            }
            land.data.GetSize();
            foreach (Int2 heightLockedBatch in heightLockedBatches)
            {
                Int2 @int = heightLockedBatch * streamer.blocksPerBatch.XZ() + streamer.blocksPerBatch.XZ() / 2;
                int newy = Mathf.FloorToInt(heightmap.GetHeight(@int.x, @int.y)) / streamer.blocksPerBatch.y;
                Int3 int2 = heightLockedBatch.XZToInt3(newy);
                GameObject gameObject = global::UnityEngine.Object.Instantiate(heightlockedHighlightPrefab, streamer.GetBatchCenter(int2), Quaternion.identity);
                gameObject.transform.localScale = streamer.blocksPerBatch.ToVector3() * 0.8f;
                gameObject.transform.parent = lockedBoxRoot.transform;
                gameObject.name = "LD Ready batch " + int2;
                gameObject.hideFlags = HideFlags.NotEditable;
                lockedBox2batch[gameObject] = int2;
            }
        }

        private void UnloadHeightLockedBatches()
        {
            if (!Application.isPlaying)
            {
                heightLockedBatches.Clear();
                lockedBox2batch.Clear();
                global::UnityEngine.Object.DestroyImmediate(lockedBoxRoot);
            }
        }

        private void SaveHeightLockedBatches()
        {
            if (Application.isPlaying)
            {
                return;
            }
            using StreamWriter streamWriter = FileUtils.CreateTextFile(heightLockedBatchesPath);
            streamWriter.WriteLine(HeightLockedBatchesVersion);
            List<Int2> list = new List<Int2>();
            foreach (Int2 heightLockedBatch in heightLockedBatches)
            {
                list.Add(heightLockedBatch);
            }
            list.Sort(default(Int2.CompareXY));
            foreach (Int2 item in list)
            {
                streamWriter.WriteLine(string.Concat(item));
            }
        }

        public void DestroyHelperHeightmap()
        {
            if (helperTerrainData != null)
            {
                global::UnityEngine.Object.DestroyImmediate(helperTerrainData);
            }
            if (helperTerrainObj != null)
            {
                global::UnityEngine.Object.DestroyImmediate(helperTerrainObj);
            }
        }

        public void PushHelperTerrainToHeightmap()
        {
            if (helperTerrainData == null || heightmap == null)
            {
                return;
            }
            Timer.Begin("PushHelperTerrainToHeightmap");
            TerrainData terrainData = helperTerrainData;
            bool flag = false;
            Int2.RangeEnumerator enumerator = Int2.Range(blockWindow.mins.xz, blockWindow.maxs.xz).GetEnumerator();
            while (enumerator.MoveNext())
            {
                Int2 current = enumerator.Current;
                ushort num = Convert.ToUInt16(terrainData.GetHeight(current.x - helperTerrainOffset.x, current.y - helperTerrainOffset.y) * 65535f / (float)land.data.sizeY);
                if (heightmap.GetHeightRaw(current) != num)
                {
                    flag = true;
                    heightmap.SetHeightRaw(current, num);
                }
            }
            if (flag)
            {
                streamer.octCompiler.OnHeightmapChanged();
            }
            Timer.End();
        }

        public void CreateHelperHeightmap()
        {
            DestroyHelperHeightmap();
            Int2 xz = land.data.GetSize().xz;
            helperTerrainOffset = Int2.zero;
            if (editingWindow)
            {
                xz = blockWindow.size.xz;
                helperTerrainOffset = blockWindow.mins.xz;
            }
            float[,] array = new float[xz.y, xz.x];
            using (Progress progress = new Progress("Computing heigths", array.Length, 50000))
            {
                Int2.RangeEnumerator enumerator = array.Indices().GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Int2 current = enumerator.Current;
                    if (progress.Tic())
                    {
                        break;
                    }
                    array.Set(current, (float)(int)heightmap.GetHeightRaw(current.YX() + helperTerrainOffset) * 1f / 65535f);
                }
            }
            Texture2D texture2D = new Texture2D(biomeMapWidth, biomeMapHeight, TextureFormat.ARGB32, mipChain: false, linear: true);
            texture2D.SetPixels32(biomeMap);
            SplatPrototype[] array2 = new SplatPrototype[1]
            {
                new SplatPrototype()
            };
            array2[0].texture = texture2D;
            array2[0].tileSize = new Vector2(biomeMapWidth, biomeMapHeight);
            if (editingWindow)
            {
                array2[0].tileOffset = blockWindow.mins.xz.ToVector2();
            }
            helperTerrainData = new TerrainData();
            TerrainData terrainData = helperTerrainData;
            terrainData.heightmapResolution = Mathf.Max(xz.x, xz.y);
            terrainData.size = new Vector3(terrainData.heightmapResolution, land.data.sizeY, terrainData.heightmapResolution);
            terrainData.splatPrototypes = array2;
            terrainData.SetHeights(0, 0, array);
            GameObject gameObject = Terrain.CreateTerrainGameObject(terrainData);
            gameObject.transform.position = land.transform.position + helperTerrainOffset.XZToInt3(0).ToVector3();
            helperTerrainObj = gameObject;
        }

        public void UpdateForAllEntities()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("UpdateForAllEntities called during play mode - not really meant for that!");
            }
            UniqueIdentifier[] array = global::UnityEngine.Object.FindObjectsOfType<UniqueIdentifier>();
            foreach (UniqueIdentifier ent in array)
            {
                OnEntityMoved(ent);
            }
        }

        public void PerformEditorSave(bool isAutosave = false)
        {
        }

        public int GenerateProceduralSlots()
        {
            proceduralSlotsRoot = GameObject.Find("/__PROCEDURAL_SLOTS__DELETE_ME__");
            if (proceduralSlotsRoot == null)
            {
                proceduralSlotsRoot = new GameObject("__PROCEDURAL_SLOTS__DELETE_ME__");
            }
            VoxelandTypeBase voxelandTypeBase = new VoxelandTypeBase();
            voxelandTypeBase.grassDensity = 1f;
            voxelandTypeBase.grassMinTilt = 0;
            voxelandTypeBase.grassMaxTilt = 180;
            EntitySlotGenerator.DeleteAutoGeneratedSlots(wsBounds);
            EntitySlotGenerator entitySlotGenerator = new EntitySlotGenerator();
            entitySlotGenerator.Initialize(wsBounds, loadCSVs: true);
            int num = 0;
            Int3.Bounds bounds = blockWindow / land.chunkSize;
            using (Progress progress = new Progress("Gen slots for chunk", bounds.GetInclusiveVolume(), 10))
            {
                VoxelandChunkWorkspace voxelandChunkWorkspace = new VoxelandChunkWorkspace();
                voxelandChunkWorkspace.SetSize(land.chunkSize);
                GameObject gameObject = new GameObject("chunk for EnumerateSurfacePositions");
                VoxelandChunk voxelandChunk = gameObject.AddComponent<VoxelandChunk>();
                voxelandChunk.land = land;
                voxelandChunk.ws = voxelandChunkWorkspace;
                foreach (Int3 item in bounds)
                {
                    if (progress.Tic())
                    {
                        break;
                    }
                    foreach (VoxelandUtils.SurfacePosition item2 in land.EnumerateSurfacePositions(voxelandChunk, voxelandTypeBase, 42, item.Refined(land.chunkSize)))
                    {
                        using (new StreamTiming.Block("OnVoxel"))
                        {
                            num += entitySlotGenerator.OnVoxel(item2.position, item2.normal, item2.typeNum, proceduralSlotsRoot.transform);
                        }
                    }
                    if (LargeWorld.onActivity != null)
                    {
                        LargeWorld.onActivity(this, null);
                    }
                }
                global::UnityEngine.Object.DestroyImmediate(gameObject);
            }
            WaterSlotGenerator waterSlotGenerator = new WaterSlotGenerator();
            waterSlotGenerator.Initialize(wsBounds, loadCSVs: true);
            Voxeland.RasterWorkspace rws = default(Voxeland.RasterWorkspace);
            using Progress progress2 = new Progress("Gen water slots for chunk", bounds.GetInclusiveVolume(), 10);
            foreach (Int3 item3 in bounds)
            {
                if (!progress2.Tic())
                {
                    Int3 blockOrigin = item3 * land.chunkSize;
                    foreach (Vector3 item4 in land.EnumerateWaterPositions(rws, blockOrigin, land.chunkSize))
                    {
                        using (new StreamTiming.Block("OnVoxel"))
                        {
                            num += waterSlotGenerator.OnVoxel(item4, proceduralSlotsRoot.transform);
                        }
                    }
                    continue;
                }
                return num;
            }
            return num;
        }

        public bool IsBatchBaked(Int3 b)
        {
            if (heightmap == null)
            {
                return false;
            }
            return heightmap.batch2baked.Get(b);
        }

        public WorldSignature ComputeCurrentSignature()
        {
            WorldSignature worldSignature = new WorldSignature();
            worldSignature.Compute(fallbackDir);
            return worldSignature;
        }
    }
}
