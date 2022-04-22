using System.Collections;
using UnityEngine;

namespace AssemblyCSharp
{
    [SkipProtoContractCheck]
    public class Fire : HandTarget
    {
        public GameObject fireFXprefab;

        public Light fireLight;

        public float lightRange = 1.75f;

        public float lightIntensity = 2f;

        public FMOD_CustomEmitter fireSound;

        [AssertNotNull]
        public LiveMixin livemixin;

        public bool introFire;

        public float fireGrowRate;

        public SubRoot fireSubRoot;

        public Vector3 minScale = Vector3.zero;

        public VFXExtinguishableFire fireFX;

        private bool playerisInFire;

        private float lastTimeDoused;

        private bool isExtinguished;

        private int fmodIndexFireHealth = -1;

        private IEnumerator Start()
        {
            _ = fireFX == null;
            isExtinguished = !livemixin.IsAlive();
            if (isExtinguished)
            {
                Extinguished();
                yield break;
            }
            if (fireFX == null && !isExtinguished)
            {
                DeferredSpawner.Task spawnTask = DeferredSpawner.instance.InstantiateAsync(fireFXprefab);
                yield return spawnTask;
                GameObject result = spawnTask.result;
                DeferredSpawner.instance.ReturnTask(spawnTask);
                result.transform.parent = base.transform.parent;
                global::UWE.Utils.ZeroTransform(result.transform);
                fireFX = result.GetComponent<VFXExtinguishableFire>();
            }
            if (fireLight != null && !fireLight.gameObject.activeSelf)
            {
                fireLight.gameObject.SetActive(value: true);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.Equals(Player.main.gameObject) && !playerisInFire)
            {
                playerisInFire = true;
                InvokeRepeating("DamagePlayerAtInterval", 0f, 1f);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.Equals(Player.main.gameObject))
            {
                playerisInFire = false;
                CancelInvoke("DamagePlayerAtInterval");
            }
        }

        private void DamagePlayerAtInterval()
        {
            if (!(Player.main.currentSub != fireSubRoot))
            {
                Player.main.GetComponent<LiveMixin>().TakeDamage(5f, base.transform.position, DamageType.Fire);
            }
        }

        public void Douse(float amount)
        {
            float healthFraction = livemixin.GetHealthFraction();
            if (fmodIndexFireHealth < 0)
            {
                fmodIndexFireHealth = fireSound.GetParameterIndex("fire_health");
            }
            fireSound.SetParameterValue(fmodIndexFireHealth, healthFraction);
            lastTimeDoused = Time.time;
            livemixin.health = Mathf.Max(livemixin.health - amount, 0f);
            if ((bool)fireFX)
            {
                fireFX.amount = healthFraction;
            }
            base.transform.localScale = Vector3.Lerp(minScale, Vector3.one, healthFraction);
            if (!livemixin.IsAlive() && !isExtinguished)
            {
                Extinguished();
            }
        }

        private void Extinguished()
        {
            if (introFire)
            {
                IntroLifepodDirector.main.ConcludeIntroSequence();
            }
            else
            {
                Object.Destroy(base.transform.parent.gameObject, 4f);
            }
            if ((bool)fireFX)
            {
                fireFX.StopAndDestroy();
            }
            if (fireLight != null)
            {
                Object.Destroy(fireLight.gameObject);
            }
            CancelInvoke("DamagePlayerAtInterval");
            Collider component = base.gameObject.GetComponent<Collider>();
            if (component != null)
            {
                Object.Destroy(component);
            }
            isExtinguished = true;
            SendMessageUpwards("FireExtinguished", null, SendMessageOptions.DontRequireReceiver);
        }

        public int GetExtinguishPercent()
        {
            return (int)(Mathf.Clamp01(livemixin.health / livemixin.maxHealth) * 100f);
        }

        public bool IsExtinguished()
        {
            return isExtinguished;
        }

        public void Update()
        {
            ProfilingUtils.BeginSample("Fire.Update()");
            if (fireFX != null && fireGrowRate != 0f && Time.time - lastTimeDoused > 0.5f)
            {
                float healthFraction = livemixin.GetHealthFraction();
                if (!isExtinguished)
                {
                    livemixin.health = Mathf.Clamp(livemixin.health + fireGrowRate * Time.deltaTime, 0f, livemixin.maxHealth);
                }
                fireFX.amount = healthFraction;
                base.transform.localScale = Vector3.Lerp(minScale, Vector3.one, healthFraction);
            }
            ProfilingUtils.EndSample();
        }

        public void LateUpdate()
        {
            if (livemixin.health > 0f)
            {
                float healthFraction = livemixin.GetHealthFraction();
                if (fireLight != null)
                {
                    fireLight.range = (lightRange + Random.value * 0.25f) * healthFraction + 1f;
                    fireLight.intensity = healthFraction * (lightIntensity + Random.value * 0.5f);
                }
            }
            else if (!isExtinguished)
            {
                Extinguished();
            }
        }

        private void OnDisable()
        {
            playerisInFire = false;
            CancelInvoke("DamagePlayerAtInterval");
        }

        private void OnDestroy()
        {
            playerisInFire = false;
            CancelInvoke("DamagePlayerAtInterval");
            if (fireFX != null)
            {
                Object.Destroy(fireFX.gameObject);
            }
            if (fireLight != null)
            {
                Object.Destroy(fireLight.gameObject);
            }
        }
    }
}
