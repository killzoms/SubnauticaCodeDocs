using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(LiveMixin))]
    public class CoralBlendWhite : MonoBehaviour, IManagedUpdateBehaviour, IManagedBehaviour
    {
        private Renderer[] renderers;

        private PooledMaterialList materialList;

        public float blendTime = 8f;

        private bool killed;

        private float timeOfDeath;

        private bool done;

        private int timesDied;

        private bool updatingDeathFade;

        private ObjectPool<PooledMaterialList> materialListPool = ObjectPoolHelper.CreatePool<PooledMaterialList>();

        public int managedUpdateIndex { get; set; }

        public string GetProfileTag()
        {
            return "CoralBlendWhite";
        }

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        private void OnKill()
        {
            timeOfDeath = Time.time;
            killed = true;
            timesDied++;
            if (!done)
            {
                RegisterForDeathUpdate();
            }
            if (timesDied >= 2)
            {
                Living component = base.gameObject.GetComponent<Living>();
                if ((bool)component)
                {
                    Object.Destroy(component);
                }
                ExploderObject.ExplodeGameObject(base.gameObject);
            }
            else
            {
                LiveMixin component2 = GetComponent<LiveMixin>();
                component2.health = component2.maxHealth * 0.5f;
            }
        }

        public void ManagedUpdate()
        {
            if (done)
            {
                return;
            }
            float num = Time.time - timeOfDeath;
            float num2 = Mathf.Min(1f, num / blendTime);
            float value = num2 * 0.3f;
            float value2 = num2;
            if (materialList == null)
            {
                materialList = materialListPool.Get();
                for (int i = 0; i < renderers.Length; i++)
                {
                    for (int j = 0; j < renderers[i].materials.Length; j++)
                    {
                        materialList.materials.Add(renderers[i].materials[j]);
                    }
                }
            }
            if (materialList != null)
            {
                for (int k = 0; k < materialList.materials.Count; k++)
                {
                    Material material = materialList.materials[k];
                    if (material != null)
                    {
                        material.SetFloat(ShaderPropertyID._Brightness, value);
                        material.SetFloat(ShaderPropertyID._Gray, value2);
                    }
                }
            }
            if (num2 == 1f)
            {
                UnregisterFromDeathUpdate();
                updatingDeathFade = false;
                done = true;
            }
        }

        private void RegisterForDeathUpdate()
        {
            BehaviourUpdateUtils.Register(this);
            updatingDeathFade = true;
        }

        private void UnregisterFromDeathUpdate()
        {
            if (updatingDeathFade)
            {
                if (materialList != null)
                {
                    materialList.materials.Clear();
                    materialListPool.Return(materialList);
                }
                BehaviourUpdateUtils.Deregister(this);
            }
        }

        private void OnEnable()
        {
            if (updatingDeathFade)
            {
                RegisterForDeathUpdate();
            }
        }

        private void OnDisable()
        {
            BehaviourUpdateUtils.Deregister(this);
        }

        private void OnDestroy()
        {
            BehaviourUpdateUtils.Deregister(this);
        }
    }
}
