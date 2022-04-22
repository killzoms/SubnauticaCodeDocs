using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class OxygenManager : MonoBehaviour
    {
        private float oxygenUnitsPerSecondSurface = 30f;

        private List<Oxygen> sources = new List<Oxygen>();

        private void Update()
        {
            ProfilingUtils.BeginSample("OxygenManager.Update()");
            AddOxygenAtSurface(Time.deltaTime);
            ProfilingUtils.EndSample();
        }

        public void RegisterSource(Oxygen src)
        {
            if (!sources.Contains(src))
            {
                sources.Add(src);
                sources.Sort((Oxygen a, Oxygen b) => (a.isPlayer && !b.isPlayer) ? (-1) : 0);
            }
        }

        public void UnregisterSource(Oxygen src)
        {
            sources.Remove(src);
        }

        public void GetTotal(out float available, out float capacity)
        {
            available = 0f;
            capacity = 0f;
            for (int i = 0; i < sources.Count; i++)
            {
                Oxygen oxygen = sources[i];
                available += oxygen.oxygenValue;
                capacity += oxygen.oxygenCapacity;
            }
        }

        public float GetOxygenAvailable()
        {
            float num = 0f;
            for (int i = 0; i < sources.Count; i++)
            {
                num += sources[i].oxygenValue;
            }
            return num;
        }

        public float GetOxygenCapacity()
        {
            float num = 0f;
            for (int i = 0; i < sources.Count; i++)
            {
                num += sources[i].oxygenCapacity;
            }
            return num;
        }

        public float GetOxygenFraction()
        {
            GetTotal(out var available, out var capacity);
            if (!(capacity > 0f))
            {
                return 0f;
            }
            return available / capacity;
        }

        public float AddOxygen(float secondsToAdd)
        {
            float num = 0f;
            for (int i = 0; i < sources.Count; i++)
            {
                float num2 = sources[i].AddOxygen(secondsToAdd);
                secondsToAdd -= num2;
                num += num2;
                if (Utils.NearlyEqual(secondsToAdd, 0f))
                {
                    break;
                }
            }
            return num;
        }

        public float RemoveOxygen(float amountToRemove)
        {
            float num = 0f;
            for (int num2 = sources.Count - 1; num2 >= 0; num2--)
            {
                float num3 = sources[num2].RemoveOxygen(amountToRemove);
                num += num3;
                amountToRemove -= num3;
            }
            return num;
        }

        public bool HasOxygenTank()
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (!sources[i].isPlayer)
                {
                    return true;
                }
            }
            return false;
        }

        private void AddOxygenAtSurface(float timeInterval)
        {
            float secondsToAdd = timeInterval * oxygenUnitsPerSecondSurface;
            bool flag = false;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i].gameObject.transform.position.y > Ocean.main.GetOceanLevel() - 1f)
                {
                    flag = true;
                    break;
                }
            }
            Player component = GetComponent<Player>();
            if (component != null)
            {
                flag = flag || component.CanBreathe();
            }
            if (flag)
            {
                AddOxygen(secondsToAdd);
            }
        }
    }
}
