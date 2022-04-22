using System;
using System.Collections;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;
using UnityEngine.Rendering;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class MapRoomFunctionality : MonoBehaviour
    {
        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public int numNodesScanned;

        [NonSerialized]
        [ProtoMember(3)]
        public TechType typeToScan;

        [AssertNotNull]
        public Transform wireFrameWorld;

        [AssertNotNull]
        public GameObject screenRoot;

        [AssertNotNull]
        public GameObject hologramRoot;

        [AssertNotNull]
        public Material mat;

        [AssertNotNull]
        public GameObject blipPrefab;

        [AssertNotNull]
        public GameObject cameraBlipPrefab;

        [AssertNotNull]
        public GameObject cameraBlipRoot;

        [AssertNotNull]
        public StorageContainer storageContainer;

        [AssertNotNull]
        public GameObject[] upgradeSlots;

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter ambientSound;

        public float hologramRadius = 1f;

        public int mapChunkSize = 32;

        public int mapLOD = 2;

        public const int mapScanRadius = 500;

        private const float defaultRange = 300f;

        private const float rangePerUpgrade = 50f;

        private const float baseScanTime = 14f;

        private const float scanTimeReductionPerUpgrade = 3f;

        private const float rotationTime = 50f;

        private const float powerPerSecond = 0.5f;

        private GameObject mapWorld;

        private GameObject mapBlipRoot;

        private readonly List<ResourceTracker.ResourceInfo> resourceNodes = new List<ResourceTracker.ResourceInfo>();

        private readonly List<GameObject> mapBlips = new List<GameObject>();

        private readonly List<GameObject> cameraBlips = new List<GameObject>();

        private double timeLastScan;

        private bool scanActive;

        private bool prevScanActive;

        private float prevFadeRadius;

        private Material matInstance;

        private bool modelUpdatePending = true;

        private static readonly List<MapRoomFunctionality> mapRooms = new List<MapRoomFunctionality>();

        private bool subscribed;

        private readonly TechType[] allowedUpgrades = new TechType[2]
        {
            TechType.MapRoomUpgradeScanRange,
            TechType.MapRoomUpgradeScanSpeed
        };

        private bool forcePoweredIfNoRelay;

        private bool prevPowerRelayState;

        private PowerRelay powerRelay;

        private float timeLastPowerDrain;

        private float mapScale => hologramRadius / 500f;

        public static void GetMapRoomsInRange(Vector3 position, float range, ICollection<MapRoomFunctionality> outlist)
        {
            float num = range * range;
            for (int i = 0; i < mapRooms.Count; i++)
            {
                MapRoomFunctionality mapRoomFunctionality = mapRooms[i];
                if ((mapRoomFunctionality.transform.position - position).sqrMagnitude <= num)
                {
                    outlist.Add(mapRoomFunctionality);
                }
            }
        }

        private void Start()
        {
            wireFrameWorld.rotation = Quaternion.identity;
            ReloadMapWorld();
            if (typeToScan != 0)
            {
                double num = timeLastScan;
                int num2 = numNodesScanned;
                StartScanning(typeToScan);
                timeLastScan = num;
                numNodesScanned = num2;
            }
            GetComponentInParent<Base>().onPostRebuildGeometry += OnPostRebuildGeometry;
            ResourceTracker.onResourceDiscovered += OnResourceDiscovered;
            ResourceTracker.onResourceRemoved += OnResourceRemoved;
            matInstance = global::UnityEngine.Object.Instantiate(mat);
            matInstance.SetFloat(ShaderPropertyID._ScanIntensity, 0f);
            matInstance.SetVector(ShaderPropertyID._MapCenterWorldPos, base.transform.position);
            mapRooms.Add(this);
            Subscribe(state: true);
            bool flag = false;
            powerRelay = GetComponentInParent<PowerRelay>();
            if ((bool)powerRelay)
            {
                flag = (prevPowerRelayState = !GameModeUtils.RequiresPower() || powerRelay.IsPowered());
                forcePoweredIfNoRelay = false;
            }
            else
            {
                flag = true;
                prevPowerRelayState = true;
                forcePoweredIfNoRelay = true;
            }
            screenRoot.SetActive(flag);
            hologramRoot.SetActive(flag);
            if (flag)
            {
                ambientSound.Play();
            }
        }

        public void OnResourceDiscovered(ResourceTracker.ResourceInfo info)
        {
            if (typeToScan == info.techType && (wireFrameWorld.position - info.position).sqrMagnitude <= 250000f)
            {
                resourceNodes.Add(info);
            }
        }

        public void OnResourceRemoved(ResourceTracker.ResourceInfo info)
        {
            if (typeToScan == info.techType)
            {
                resourceNodes.Remove(info);
            }
        }

        public TechType GetActiveTechType()
        {
            return typeToScan;
        }

        private void OnPostRebuildGeometry(Base b)
        {
            Int3 @int = b.NormalizeCell(b.WorldToGrid(base.transform.position));
            Base.CellType cell = b.GetCell(@int);
            if (cell != Base.CellType.MapRoom && cell != Base.CellType.MapRoomRotated)
            {
                Debug.Log(string.Concat("map room had been destroyed, at cell ", @int, " new celltype is ", cell));
                global::UnityEngine.Object.Destroy(base.gameObject);
            }
        }

        public void ReloadMapWorld()
        {
            global::UnityEngine.Object.Destroy(mapWorld);
            StartCoroutine(LoadMapWorld());
        }

        private IEnumerator LoadMapWorld()
        {
            mapWorld = new GameObject("Map");
            mapWorld.transform.SetParent(wireFrameWorld, worldPositionStays: false);
            Int3 block = LargeWorldStreamer.main.GetBlock(base.transform.position);
            Int3 @int = block - 500;
            Int3 int2 = block + 500;
            Int3 msCenterBlock = block >> mapLOD;
            Int3 mins = (@int >> mapLOD) / mapChunkSize;
            Int3 maxs = (int2 >> mapLOD) / mapChunkSize;
            float chunkScale = mapScale * (float)(1 << mapLOD);
            Int3.RangeEnumerator iter = Int3.Range(mins, maxs);
            while (iter.MoveNext())
            {
                Int3 chunkId = iter.Current;
                string chunkPath = $"WorldMeshes/Mini{mapLOD}/Chunk-{chunkId.x}-{chunkId.y}-{chunkId.z}";
                ResourceRequest request = Resources.LoadAsync<Mesh>(chunkPath);
                yield return request;
                Mesh mesh = (Mesh)request.asset;
                if ((bool)mesh)
                {
                    Int3 int3 = chunkId * mapChunkSize - msCenterBlock;
                    GameObject obj = new GameObject(chunkPath);
                    obj.transform.SetParent(mapWorld.transform, worldPositionStays: false);
                    obj.transform.localScale = new Vector3(chunkScale, chunkScale, chunkScale);
                    obj.transform.localPosition = int3.ToVector3() * chunkScale;
                    obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                    MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    meshRenderer.sharedMaterial = matInstance;
                    meshRenderer.receiveShadows = false;
                }
            }
        }

        private bool CheckIsPowered()
        {
            if (!forcePoweredIfNoRelay)
            {
                if (powerRelay != null)
                {
                    return powerRelay.IsPowered();
                }
                return false;
            }
            return true;
        }

        private void Update()
        {
            ProfilingUtils.BeginSample("MapRoomFunctionality.Update()");
            if (!forcePoweredIfNoRelay)
            {
                bool flag = CheckIsPowered();
                if (prevPowerRelayState && !flag)
                {
                    screenRoot.SetActive(value: false);
                    hologramRoot.SetActive(value: false);
                    ambientSound.Stop();
                }
                else if (!prevPowerRelayState && flag)
                {
                    screenRoot.SetActive(value: true);
                    hologramRoot.SetActive(value: true);
                    timeLastScan = 0.0;
                    ambientSound.Play();
                }
                prevPowerRelayState = flag;
            }
            UpdateScanning();
            if (modelUpdatePending)
            {
                UpdateModel();
                ObtainResourceNodes(typeToScan);
            }
            ProfilingUtils.EndSample();
        }

        private void UpdateModel()
        {
            int count = storageContainer.container.count;
            for (int i = 0; i < upgradeSlots.Length; i++)
            {
                upgradeSlots[i].SetActive(i < count);
            }
            modelUpdatePending = false;
        }

        public float GetScanRange()
        {
            return Mathf.Min(500f, 300f + (float)storageContainer.container.GetCount(TechType.MapRoomUpgradeScanRange) * 50f);
        }

        public float GetScanInterval()
        {
            return Mathf.Max(1f, 14f - (float)storageContainer.container.GetCount(TechType.MapRoomUpgradeScanSpeed) * 3f);
        }

        private void ObtainResourceNodes(TechType typeToScan)
        {
            resourceNodes.Clear();
            Vector3 scannerPos = wireFrameWorld.position;
            Dictionary<string, ResourceTracker.ResourceInfo>.ValueCollection nodes = ResourceTracker.GetNodes(typeToScan);
            if (nodes != null)
            {
                float scanRange = GetScanRange();
                float num = scanRange * scanRange;
                Dictionary<string, ResourceTracker.ResourceInfo>.ValueCollection.Enumerator enumerator = nodes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    ResourceTracker.ResourceInfo current = enumerator.Current;
                    if ((scannerPos - current.position).sqrMagnitude <= num)
                    {
                        resourceNodes.Add(current);
                    }
                }
            }
            ProfilingUtils.BeginSample("SortResourceNodes");
            resourceNodes.Sort(delegate(ResourceTracker.ResourceInfo a, ResourceTracker.ResourceInfo b)
            {
                float sqrMagnitude = (a.position - scannerPos).sqrMagnitude;
                float sqrMagnitude2 = (b.position - scannerPos).sqrMagnitude;
                return sqrMagnitude.CompareTo(sqrMagnitude2);
            });
            ProfilingUtils.EndSample();
        }

        public void StartScanning(TechType newTypeToScan)
        {
            typeToScan = newTypeToScan;
            ObtainResourceNodes(typeToScan);
            mapBlips.Clear();
            global::UnityEngine.Object.Destroy(mapBlipRoot);
            mapBlipRoot = new GameObject("MapBlipRoot");
            mapBlipRoot.transform.SetParent(wireFrameWorld, worldPositionStays: false);
            scanActive = typeToScan != TechType.None;
            numNodesScanned = 0;
            timeLastScan = 0.0;
        }

        public IList<ResourceTracker.ResourceInfo> GetNodes()
        {
            return resourceNodes;
        }

        public void GetDiscoveredNodes(ICollection<ResourceTracker.ResourceInfo> outNodes)
        {
            int num = Mathf.Min(numNodesScanned, resourceNodes.Count);
            for (int i = 0; i < num; i++)
            {
                outNodes.Add(resourceNodes[i]);
            }
        }

        private void UpdateBlips()
        {
            if (!scanActive)
            {
                return;
            }
            Vector3 position = mapBlipRoot.transform.position;
            int num = Mathf.Min(numNodesScanned + 1, resourceNodes.Count);
            if (num != numNodesScanned)
            {
                numNodesScanned = num;
            }
            for (int i = 0; i < num; i++)
            {
                Vector3 vector = (resourceNodes[i].position - position) * mapScale;
                if (i >= mapBlips.Count)
                {
                    GameObject gameObject = global::UnityEngine.Object.Instantiate(blipPrefab, vector, Quaternion.identity);
                    gameObject.transform.SetParent(mapBlipRoot.transform, worldPositionStays: false);
                    mapBlips.Add(gameObject);
                }
                mapBlips[i].transform.localPosition = vector;
                mapBlips[i].SetActive(value: true);
            }
            for (int j = num; j < mapBlips.Count; j++)
            {
                mapBlips[j].SetActive(value: false);
            }
        }

        private void UpdateCameraBlips()
        {
            float scanRange = GetScanRange();
            float num = scanRange * scanRange;
            Vector3 position = cameraBlipRoot.transform.position;
            int num2 = 0;
            for (int i = 0; i < MapRoomCamera.cameras.Count; i++)
            {
                Vector3 position2 = MapRoomCamera.cameras[i].transform.position;
                if ((wireFrameWorld.position - position2).sqrMagnitude <= num)
                {
                    Vector3 vector = (position2 - position) * mapScale;
                    if (num2 >= cameraBlips.Count)
                    {
                        GameObject gameObject = global::UnityEngine.Object.Instantiate(cameraBlipPrefab, vector, Quaternion.identity);
                        gameObject.transform.SetParent(cameraBlipRoot.transform, worldPositionStays: false);
                        cameraBlips.Add(gameObject);
                    }
                    cameraBlips[num2].transform.localPosition = vector;
                    cameraBlips[num2].SetActive(value: true);
                    num2++;
                }
            }
            for (int j = num2; j < cameraBlips.Count; j++)
            {
                cameraBlips[j].SetActive(value: false);
            }
        }

        private void UpdateScanning()
        {
            DayNightCycle main = DayNightCycle.main;
            if (!main)
            {
                return;
            }
            double timePassed = main.timePassed;
            if (timeLastScan + (double)GetScanInterval() <= timePassed && CheckIsPowered())
            {
                timeLastScan = timePassed;
                UpdateBlips();
                UpdateCameraBlips();
                float num = 1f / (GetScanRange() * mapScale);
                if (prevFadeRadius != num)
                {
                    matInstance.SetFloat(ShaderPropertyID._FadeRadius, num);
                    prevFadeRadius = num;
                }
            }
            if (scanActive != prevScanActive)
            {
                matInstance.SetFloat(ShaderPropertyID._ScanIntensity, scanActive ? 0.35f : 0f);
                prevScanActive = scanActive;
            }
            if (scanActive && (bool)powerRelay && timeLastPowerDrain + 1f < Time.time)
            {
                powerRelay.ConsumeEnergy(0.5f, out var _);
                timeLastPowerDrain = Time.time;
            }
        }

        private void OnDestroy()
        {
            global::UnityEngine.Object.Destroy(matInstance);
            Base componentInParent = GetComponentInParent<Base>();
            if ((bool)componentInParent)
            {
                componentInParent.onPostRebuildGeometry -= OnPostRebuildGeometry;
            }
            ResourceTracker.onResourceDiscovered -= OnResourceDiscovered;
            ResourceTracker.onResourceRemoved -= OnResourceRemoved;
            mapRooms.Remove(this);
        }

        private void Subscribe(bool state)
        {
            if (subscribed != state)
            {
                if (subscribed)
                {
                    storageContainer.container.onAddItem -= AddItem;
                    storageContainer.container.onRemoveItem -= RemoveItem;
                    storageContainer.container.isAllowedToAdd = null;
                    storageContainer.container.isAllowedToRemove = null;
                }
                else
                {
                    storageContainer.container.onAddItem += AddItem;
                    storageContainer.container.onRemoveItem += RemoveItem;
                    storageContainer.container.isAllowedToAdd = IsAllowedToAdd;
                }
                subscribed = state;
            }
        }

        private void AddItem(InventoryItem item)
        {
            modelUpdatePending = true;
        }

        private void RemoveItem(InventoryItem item)
        {
            modelUpdatePending = true;
        }

        private bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            TechType techType = pickupable.GetTechType();
            for (int i = 0; i < allowedUpgrades.Length; i++)
            {
                if (allowedUpgrades[i] == techType)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
