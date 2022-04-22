using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    [DisallowMultipleComponent]
    public class WorldForces : MonoBehaviour
    {
        private class Explosion
        {
            public Vector3 position;

            public double startTime;

            public double endTime;

            public float magnitude;

            public float radius;
        }

        private class Current
        {
            public Vector3 position;

            public float radius;

            public double startTime;

            public double endTime;

            public Vector3 direction;

            public float startSpeed;
        }

        public bool handleGravity = true;

        public float aboveWaterGravity = 9.81f;

        public float underwaterGravity = 1f;

        public bool handleDrag = true;

        public float aboveWaterDrag = 0.1f;

        public float underwaterDrag = 1f;

        [NonSerialized]
        public float waterDepth;

        [AssertNotNull]
        public Rigidbody useRigidbody;

        private const float kExplosionTravelSpeed = 500f;

        private const float kExplosionEdgeDuration = 0.03f;

        private static List<Explosion> explosionList = new List<Explosion>();

        private static List<Current> currentsList = new List<Current>();

        [NonSerialized]
        public int updaterIndex = -1;

        private bool been_disabled;

        private bool was_above_water;

        private void Awake()
        {
            if (useRigidbody == null)
            {
                useRigidbody = base.gameObject.GetComponent<Rigidbody>();
            }
        }

        private void OnEnable()
        {
            if (useRigidbody != null)
            {
                WorldForcesManager.Instance.AddWorldForces(this);
            }
        }

        private void OnDisable()
        {
            if (updaterIndex != -1)
            {
                WorldForcesManager.Instance.RemoveWorldForces(this);
            }
        }

        public static void AddExplosion(Vector3 position, double time, float magnitude, float radius)
        {
            Explosion explosion = new Explosion();
            explosion.position = position;
            explosion.startTime = time;
            explosion.endTime = time + (double)(radius / 500f);
            explosion.magnitude = magnitude;
            explosion.radius = radius;
            explosionList.Add(explosion);
        }

        public static void AddCurrent(Vector3 position, double time, float radius, Vector3 direction, float startSpeed, float lifeTime)
        {
            Current current = new Current();
            current.position = position;
            current.radius = radius;
            current.direction = direction;
            current.startSpeed = startSpeed;
            current.startTime = time;
            current.endTime = time + (double)lifeTime;
            currentsList.Add(current);
        }

        private void Start()
        {
            try
            {
                if ((bool)useRigidbody)
                {
                    bool flag = IsAboveWater();
                    if (!flag)
                    {
                        useRigidbody.drag = underwaterDrag;
                    }
                    else
                    {
                        useRigidbody.drag = aboveWaterDrag;
                    }
                    was_above_water = flag;
                }
            }
            finally
            {
            }
        }

        public bool IsAboveWater()
        {
            return base.transform.position.y >= waterDepth;
        }

        public void DoFixedUpdate()
        {
            if (useRigidbody.isKinematic)
            {
                return;
            }
            Vector3 position = base.transform.position;
            if (handleGravity)
            {
                float t = (0f - (position.y - waterDepth)) * 10f;
                float num = Mathf.Lerp(aboveWaterGravity, underwaterGravity, t);
                useRigidbody.AddForce(new Vector3(0f, 0f - num, 0f), ForceMode.Acceleration);
            }
            if (handleDrag)
            {
                bool flag = position.y >= waterDepth;
                if (was_above_water && !flag)
                {
                    useRigidbody.drag = underwaterDrag;
                }
                else if (!was_above_water && flag)
                {
                    useRigidbody.drag = aboveWaterDrag;
                }
                was_above_water = flag;
            }
            for (int i = 0; i < explosionList.Count; i++)
            {
                Explosion explosion = explosionList[i];
                if (DayNightCycle.main.timePassed > explosion.endTime)
                {
                    explosionList[i] = explosionList[explosionList.Count - 1];
                    explosionList.RemoveAt(explosionList.Count - 1);
                    i--;
                    continue;
                }
                double startTime = explosion.startTime;
                float magnitude = (explosion.position - position).magnitude;
                double num2 = startTime + (double)(magnitude / 500f);
                if (DayNightCycle.main.timePassed >= num2 && DayNightCycle.main.timePassed <= num2 + 0.029999999329447746)
                {
                    Vector3 vector = position - explosion.position;
                    vector.Normalize();
                    float num3 = Mathf.Max(explosion.magnitude - magnitude / 500f, 1f);
                    Vector3 vector2 = vector * (num3 * (0.5f + global::UnityEngine.Random.value * 0.5f));
                    useRigidbody.AddForce(vector2, ForceMode.Impulse);
                    Debug.DrawLine(position, position + vector2, Color.yellow, 0.1f);
                }
            }
            Vector3 vector3 = Vector3.zero;
            float num4 = 0f;
            for (int j = 0; j < currentsList.Count; j++)
            {
                Current current = currentsList[j];
                if (DayNightCycle.main.timePassed > current.endTime)
                {
                    currentsList[j] = currentsList[currentsList.Count - 1];
                    currentsList.RemoveAt(currentsList.Count - 1);
                    j--;
                }
                else if ((position - current.position).sqrMagnitude < current.radius * current.radius)
                {
                    float b = (float)(current.endTime - current.startTime);
                    float value = (float)(DayNightCycle.main.timePassed - current.startTime);
                    float t2 = Mathf.InverseLerp(0f, b, value);
                    float num5 = Mathf.Lerp(current.startSpeed, 0f, t2);
                    if (num5 > num4)
                    {
                        num4 = num5;
                        vector3 = current.direction;
                    }
                }
            }
            if (num4 > 0f)
            {
                useRigidbody.AddForce(num4 * vector3, ForceMode.Impulse);
            }
        }

        public string CompileTimeCheck()
        {
            if (!useRigidbody)
            {
                return "Missing rigidbody";
            }
            if (handleGravity && !useRigidbody.isKinematic)
            {
                if (underwaterGravity > 0f)
                {
                    return "Entities that sink under water must be set kinematic to prevent them from falling through the floor.";
                }
                if (underwaterGravity < 0f)
                {
                    return "Entities that float to the surface must be set kinematic to prevent them from rising through the ceiling.";
                }
            }
            return null;
        }
    }
}
