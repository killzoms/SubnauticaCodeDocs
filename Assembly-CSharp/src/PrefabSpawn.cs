using UnityEngine;

namespace AssemblyCSharp
{
    public class PrefabSpawn : MonoBehaviour
    {
        public SpawnType spawnType;

        public float intermittentSpawnTime;

        private float spawnTime;

        public GameObject prefab;

        public bool inheritLayer;

        public bool usePrefabTransformAsLocal;

        public bool useCurrentTransformAsLocal;

        public bool keepScale = true;

        public Transform attachToParent;

        public float spawnAtHealthPercent = 1f;

        public bool useSpawnAtHealth;

        public GameObject spawnedObj;

        private GameObject spawnedLastFrame;

        public string sendObjectMessageOnSpawn = "";

        public bool deactivateOnSpawn;

        private void Awake()
        {
            if (prefab != null)
            {
                if (spawnType == SpawnType.OnAwake)
                {
                    SpawnObj();
                }
            }
            else
            {
                Debug.Log(base.gameObject.name + ".PrefabSpawn() - No prefab specified, skipping.");
                base.gameObject.SetActive(value: false);
            }
            spawnTime = Random.value * intermittentSpawnTime;
        }

        private void Start()
        {
            if (prefab != null)
            {
                if (spawnType == SpawnType.OnStart)
                {
                    SpawnObj();
                }
            }
            else
            {
                Debug.Log(base.gameObject.name + ".PrefabSpawn() - No prefab specified, skipping.");
                base.gameObject.SetActive(value: false);
            }
        }

        public GameObject SpawnManual()
        {
            if (spawnType == SpawnType.Manual && spawnedObj == null)
            {
                SpawnObj();
                return spawnedObj;
            }
            return null;
        }

        public virtual bool GetTimeToSpawn()
        {
            return Random.value * intermittentSpawnTime < Time.deltaTime;
        }

        private void Update()
        {
            ProfilingUtils.BeginSample("PrefabSpawn.Update()");
            if (spawnType == SpawnType.Intermittent && spawnedObj == null && GetTimeToSpawn())
            {
                SpawnObj();
            }
            spawnedLastFrame = spawnedObj;
            ProfilingUtils.EndSample();
        }

        private void OnNewBorn()
        {
            if (prefab != null && spawnType == SpawnType.OnNewBorn && spawnedObj == null)
            {
                SpawnObj();
            }
        }

        public virtual void SpawnObj()
        {
            if (spawnedObj != null)
            {
                Debug.Log("WARNING: PrefabSpawn (" + base.gameObject.GetFullHierarchyPath() + ") already spawned its object! Object = " + spawnedObj.GetFullHierarchyPath());
                return;
            }
            Transform parent = base.transform;
            if ((bool)attachToParent)
            {
                parent = attachToParent;
            }
            if (usePrefabTransformAsLocal)
            {
                spawnedObj = Object.Instantiate(prefab);
                spawnedObj.transform.parent = parent;
                spawnedObj.transform.localPosition = spawnedObj.transform.position;
                spawnedObj.transform.localRotation = spawnedObj.transform.rotation;
            }
            else if (useCurrentTransformAsLocal)
            {
                spawnedObj = Object.Instantiate(prefab);
                spawnedObj.transform.parent = parent;
                spawnedObj.transform.localPosition = base.transform.localPosition;
                spawnedObj.transform.localRotation = base.transform.localRotation;
            }
            else
            {
                spawnedObj = Utils.SpawnZeroedAt(prefab, parent, keepScale);
            }
            if (inheritLayer)
            {
                spawnedObj.layer = base.gameObject.layer;
                Transform[] allComponentsInChildren = spawnedObj.GetAllComponentsInChildren<Transform>();
                for (int i = 0; i < allComponentsInChildren.Length; i++)
                {
                    allComponentsInChildren[i].gameObject.layer = base.gameObject.layer;
                }
            }
            if (deactivateOnSpawn)
            {
                spawnedObj.SetActive(value: false);
            }
            if (useSpawnAtHealth)
            {
                LiveMixin componentInChildren = spawnedObj.GetComponentInChildren<LiveMixin>();
                if ((bool)componentInChildren)
                {
                    componentInChildren.startHealthPercent = spawnAtHealthPercent;
                }
                else
                {
                    Debug.LogWarningFormat(this, "Failed to set start health percent on spawned object {0}", spawnedObj);
                }
            }
            if (sendObjectMessageOnSpawn != "")
            {
                spawnedObj.SendMessage(sendObjectMessageOnSpawn);
            }
        }
    }
}
