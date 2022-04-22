using UnityEngine;

namespace AssemblyCSharp
{
    public static class ParticleSystemExtensions
    {
        public static void EnableEmission(this ParticleSystem particleSystem, bool enabled)
        {
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = enabled;
        }

        public static float GetEmissionRate(this ParticleSystem particleSystem)
        {
            return particleSystem.emission.rate.constantMax;
        }

        public static void SetEmissionRate(this ParticleSystem particleSystem, float emissionRate)
        {
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            ParticleSystem.MinMaxCurve rate = emission.rate;
            rate.constantMax = emissionRate;
            emission.rate = rate;
        }
    }
}
