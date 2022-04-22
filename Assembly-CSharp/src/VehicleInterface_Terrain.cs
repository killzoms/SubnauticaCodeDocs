using System.Collections;
using System.Collections.Generic;
using Gendarme;
using UnityEngine;
using UnityEngine.Rendering;
using UWE;

namespace AssemblyCSharp
{
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidUnnecessarySpecializationRule")]
    [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
    public class VehicleInterface_Terrain : MonoBehaviour
    {
        [AssertNotNull]
        public Transform hologramHolder;

        [AssertNotNull]
        public Material hologramMaterial;

        public float hologramRadius = 1f;

        public int mapWorldRadius = 20;

        public int mapChunkSize = 32;

        public int mapLOD = 2;

        public bool active = true;

        private GameObject hologramObject;

        private Material materialInstance;

        private float chunkScale;

        private Stack<GameObject> chunkPool = new Stack<GameObject>();

        private readonly string pooledName = "PooledHologramChunk";

        private Dictionary<Int3, GameObject> loadedChunks = new Dictionary<Int3, GameObject>();

        private Coroutine rebuildHologramCoroutine;

        private bool initialized;

        private Color mapColor;

        private Color mapColorNoAlpha;

        private HashSet<Int3> requestChunks = new HashSet<Int3>();

        private HashSet<Int3> disableChunks = new HashSet<Int3>();

        private Dictionary<Int3, string> chunkFilenameCache = new Dictionary<Int3, string>();

        private const int maxFilenameCacheSize = 500;

        private float mapScale => hologramRadius / (float)mapWorldRadius;

        public void EnableMap()
        {
            active = true;
        }

        public void DisableMap()
        {
            active = false;
        }

        public void ToggleMap()
        {
            active = !active;
        }

        private void Start()
        {
            InitializeHologram();
        }

        private void OnEnable()
        {
            rebuildHologramCoroutine = StartCoroutine(RebuildHologram());
        }

        private void OnDisable()
        {
            if (rebuildHologramCoroutine != null)
            {
                StopCoroutine(rebuildHologramCoroutine);
                rebuildHologramCoroutine = null;
            }
        }

        private void InitializeHologram()
        {
            chunkScale = mapScale * 1f;
            materialInstance = Object.Instantiate(hologramMaterial);
            hologramObject = new GameObject("Map");
            hologramObject.transform.SetParent(hologramHolder, worldPositionStays: false);
            mapColor = materialInstance.GetColor(ShaderPropertyID._Color);
            mapColorNoAlpha = new Color(mapColor.r, mapColor.g, mapColor.b, 0f);
            initialized = true;
        }

        private void Update()
        {
            hologramHolder.rotation = Quaternion.identity;
            materialInstance.SetVector(ShaderPropertyID._MapCenterWorldPos, base.transform.position);
            Vector3 vector = LargeWorldStreamer.main.land.transform.InverseTransformPoint(base.transform.position) / (1 << mapLOD);
            foreach (KeyValuePair<Int3, GameObject> loadedChunk in loadedChunks)
            {
                GameObject obj = loadedChunks[loadedChunk.Key];
                Vector3 vector2 = (loadedChunk.Key * mapChunkSize).ToVector3() - vector;
                obj.transform.localPosition = vector2 * chunkScale;
            }
            if (active)
            {
                SetColor(mapColor);
            }
            else
            {
                SetColor(mapColorNoAlpha);
            }
        }

        private void SetColor(Color goToColor)
        {
            Color value = Color.LerpUnclamped(materialInstance.GetColor(ShaderPropertyID._Color), goToColor, Time.deltaTime * 5f);
            materialInstance.SetColor(ShaderPropertyID._Color, value);
        }

        private void ClearUnusedChunks(HashSet<Int3> requestChunks)
        {
            foreach (KeyValuePair<Int3, GameObject> loadedChunk in loadedChunks)
            {
                if (!requestChunks.Contains(loadedChunk.Key))
                {
                    disableChunks.Add(loadedChunk.Key);
                }
            }
            foreach (Int3 disableChunk in disableChunks)
            {
                DisableChunk(disableChunk);
            }
            disableChunks.Clear();
        }

        private void DisableChunk(Int3 chunkIndex)
        {
            GameObject gameObject = loadedChunks[chunkIndex];
            gameObject.name = pooledName;
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            gameObject.GetComponent<MeshFilter>().sharedMesh = null;
            loadedChunks.Remove(chunkIndex);
            chunkPool.Push(gameObject);
        }

        private void GetOrMakeChunk(Int3 chunkId, Mesh mesh, string chunkPath)
        {
            GameObject gameObject = null;
            MeshRenderer meshRenderer = null;
            MeshFilter meshFilter = null;
            if (chunkPool.Count > 0)
            {
                gameObject = chunkPool.Pop();
                gameObject.name = chunkPath;
                meshRenderer = gameObject.GetComponent<MeshRenderer>();
                meshRenderer.enabled = true;
                meshFilter = gameObject.GetComponent<MeshFilter>();
            }
            else
            {
                gameObject = new GameObject(chunkPath);
                gameObject.transform.SetParent(hologramObject.transform, worldPositionStays: false);
                gameObject.transform.localScale = new Vector3(chunkScale, chunkScale, chunkScale);
                meshFilter = gameObject.AddComponent<MeshFilter>();
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            loadedChunks.Add(chunkId, gameObject);
            meshFilter.sharedMesh = mesh;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.sharedMaterial = materialInstance;
            meshRenderer.receiveShadows = false;
        }

        private bool GetChunkExists(Int3 requestChunk)
        {
            if (loadedChunks.ContainsKey(requestChunk))
            {
                return true;
            }
            return false;
        }

        private string GetChunkFilename(Int3 chunkId)
        {
            if (!chunkFilenameCache.TryGetValue(chunkId, out var value))
            {
                if (chunkFilenameCache.Count == 500)
                {
                    chunkFilenameCache.Clear();
                }
                value = $"WorldMeshes/Mini2/Chunk-{chunkId.x}-{chunkId.y}-{chunkId.z}";
                chunkFilenameCache.Add(chunkId, value);
            }
            return value;
        }

        private IEnumerator RebuildHologram()
        {
            while (!initialized)
            {
                yield return null;
            }
            while (true)
            {
                float startBuildTime = Time.time;
                Int3 block = LargeWorldStreamer.main.GetBlock(base.transform.position);
                Int3 @int = block - mapWorldRadius;
                Int3 int2 = block + mapWorldRadius;
                _ = block >> mapLOD;
                Int3 mins = (@int >> mapLOD) / mapChunkSize;
                Int3 maxs = (int2 >> mapLOD) / mapChunkSize;
                Int3.RangeEnumerator iter = Int3.Range(mins, maxs);
                while (iter.MoveNext())
                {
                    Int3 chunkId = iter.Current;
                    requestChunks.Add(chunkId);
                    if (!GetChunkExists(chunkId))
                    {
                        string chunkPath = GetChunkFilename(chunkId);
                        ResourceRequest request = Resources.LoadAsync<Mesh>(chunkPath);
                        yield return request;
                        Mesh mesh = (Mesh)request.asset;
                        if ((bool)mesh && !GetChunkExists(chunkId))
                        {
                            GetOrMakeChunk(chunkId, mesh, chunkPath);
                        }
                    }
                }
                ClearUnusedChunks(requestChunks);
                requestChunks.Clear();
                float num = Time.time - startBuildTime;
                float num2 = Mathf.Max(10f - num, 2f);
                float nextUpdateTime = Time.time + num2;
                while (Time.time < nextUpdateTime)
                {
                    yield return CoroutineUtils.waitForNextFrame;
                }
            }
        }

        private void OnDestroy()
        {
            Object.Destroy(materialInstance);
        }
    }
}
