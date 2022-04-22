using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class VFXSeamothDamages : MonoBehaviour, IOnTakeDamage
    {
        [Serializable]
        public class ParticlesEmitter
        {
            public ParticleSystem ps;

            public bool isPlaying;

            public void Play()
            {
                if (!isPlaying)
                {
                    ps.Play();
                    isPlaying = true;
                }
            }

            public void Stop()
            {
                if (isPlaying)
                {
                    ps.Stop();
                    isPlaying = true;
                }
            }
        }

        public ParticlesEmitter[] bubblesEmitters;

        public ParticleSystem dripsParticles;

        public ParticleSystem bubblesParticles;

        public ParticleSystem smokeParticles;

        public Renderer[] modelsAlphaPow;

        public float minPow = 1f;

        public float maxPow = 4f;

        public Transform[] sparksPoints;

        public GameObject[] sparksPrefabs;

        public float minDelay = 0.5f;

        public float maxDelay = 10f;

        private LiveMixin liveMixin;

        private float delayTimer;

        private float currentDelay = 1f;

        private float healthRatio = 1f;

        private float prevHealthRatio;

        private Color[] modelsInitColor;

        private Color[] modelsFadedColor;

        private void Start()
        {
            liveMixin = Utils.FindAncestorWithComponent<LiveMixin>(base.gameObject);
            modelsInitColor = new Color[modelsAlphaPow.Length];
            modelsFadedColor = new Color[modelsAlphaPow.Length];
            for (int i = 0; i < modelsAlphaPow.Length; i++)
            {
                modelsInitColor[i] = modelsAlphaPow[i].material.GetColor(ShaderPropertyID._Color);
                modelsFadedColor[i] = modelsInitColor[i];
                modelsFadedColor[i].a = 0f;
            }
        }

        public void OnTakeDamage(DamageInfo damageInfo)
        {
            if (damageInfo.damage > 10f)
            {
                SpawnRandomSparks();
            }
            ComputeDelay();
        }

        private void Update()
        {
            healthRatio = liveMixin.health / liveMixin.maxHealth;
            if (healthRatio < 1f)
            {
                if (!(prevHealthRatio < 1f))
                {
                    ToggleChildren(enable: true);
                }
                delayTimer += Time.deltaTime / currentDelay;
                if (delayTimer > 0.99f && healthRatio < 0.5f)
                {
                    SpawnRandomSparks();
                    ComputeDelay();
                    delayTimer = 0f;
                }
                UpdateMaterials();
                UpdateParticles();
            }
            else if (prevHealthRatio < 1f)
            {
                ToggleChildren(enable: false);
            }
            prevHealthRatio = healthRatio;
        }

        private void UpdateParticles()
        {
            float emissionRate = Mathf.Clamp(0.5f - healthRatio, 0f, 1f) * 50f;
            float emissionRate2 = Mathf.Clamp(0.25f - healthRatio, 0f, 1f) * 50f;
            dripsParticles.SetEmissionRate(emissionRate);
            bubblesParticles.SetEmissionRate(emissionRate);
            smokeParticles.SetEmissionRate(emissionRate2);
            if (healthRatio < 0.6f)
            {
                bubblesEmitters[0].Play();
            }
            else
            {
                bubblesEmitters[0].Stop();
            }
            if (healthRatio < 0.3f)
            {
                bubblesEmitters[1].Play();
            }
            else
            {
                bubblesEmitters[1].Stop();
            }
        }

        private void ComputeDelay()
        {
            float value = global::UnityEngine.Random.Range(minDelay, maxDelay) * (1f - healthRatio);
            currentDelay = Mathf.Clamp(value, minDelay, maxDelay);
        }

        private void SpawnRandomSparks()
        {
            GameObject prefab = sparksPrefabs[global::UnityEngine.Random.Range(0, sparksPrefabs.Length)];
            Transform parent = sparksPoints[global::UnityEngine.Random.Range(0, sparksPoints.Length)];
            Utils.SpawnZeroedAt(prefab, parent);
        }

        private void UpdateMaterials()
        {
            float value = Mathf.Lerp(minPow, maxPow, healthRatio);
            for (int i = 0; i < modelsAlphaPow.Length; i++)
            {
                modelsAlphaPow[i].material.SetFloat(ShaderPropertyID._AlphaPow, value);
                modelsAlphaPow[i].material.SetColor(ShaderPropertyID._Color, Color.Lerp(modelsInitColor[i], modelsFadedColor[i], healthRatio));
            }
        }

        private void ToggleChildren(bool enable)
        {
            Transform[] componentsInChildren = GetComponentsInChildren<Transform>(includeInactive: true);
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                if (componentsInChildren[i] != base.transform)
                {
                    componentsInChildren[i].gameObject.SetActive(enable);
                }
            }
        }

        private void OnDestroy()
        {
            if (modelsInitColor != null)
            {
                for (int i = 0; i < modelsAlphaPow.Length; i++)
                {
                    global::UnityEngine.Object.Destroy(modelsAlphaPow[i].material);
                }
            }
        }
    }
}
