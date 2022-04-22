using System.Collections;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class StreamingPerfTest : MonoBehaviour
    {
        public float initialDelay = 1f;

        public int numInsts = 10;

        public float waitSecs = 1f;

        public Vector3 spawnOrigin = new Vector3(0f, -10f, 0f);

        public GameObject prefab;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(initialDelay);
            yield return StartCoroutine(SimulateLifecycle(numInsts));
            Debug.LogWarning("------------ done");
        }

        private static void Begin(string label)
        {
            Timer.Begin(label);
            ProfilingUtils.BeginSample(label);
        }

        private static void End()
        {
            ProfilingUtils.EndSample();
            Timer.End();
        }

        private IEnumerator SimulateLifecycle(int num)
        {
            GameObject[] go = new GameObject[num];
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(waitSecs);
                Debug.Log("----------- Naive inst");
                Begin("Naive inst+restore");
                for (int l = 0; l < num; l++)
                {
                    go[l] = Object.Instantiate(prefab);
                    go[l].transform.position = spawnOrigin + Random.insideUnitSphere;
                }
                End();
                yield return new WaitForSeconds(waitSecs);
                Begin("Destroy");
                for (int m = 0; m < num; m++)
                {
                    Object.Destroy(go[m]);
                }
                End();
                yield return new WaitForSeconds(waitSecs);
                Debug.Log("----------- inst w/o colliders");
                for (int k = 0; k < 2; k++)
                {
                    Begin("Collider-sensitive inst+restore");
                    for (int n = 0; n < num; n++)
                    {
                        SetCollidersEnabled(prefab, value: false);
                        go[n] = Object.Instantiate(prefab);
                        SetCollidersEnabled(prefab, value: true);
                        go[n].transform.position = spawnOrigin + Random.insideUnitSphere;
                        SetCollidersEnabled(go[n], value: true);
                    }
                    End();
                    yield return new WaitForSeconds(waitSecs);
                    Begin("Destroy");
                    for (int num2 = 0; num2 < num; num2++)
                    {
                        Object.Destroy(go[num2]);
                    }
                    End();
                    yield return new WaitForSeconds(waitSecs);
                }
                Debug.Log("----------- pooling ");
                Begin("Initial use");
                for (int num3 = 0; num3 < num; num3++)
                {
                    SetCollidersEnabled(prefab, value: false);
                    go[num3] = Object.Instantiate(prefab);
                    SetCollidersEnabled(prefab, value: true);
                    go[num3].transform.position = spawnOrigin + Random.insideUnitSphere;
                    SetCollidersEnabled(go[num3], value: true);
                    go[num3].SetActive(value: true);
                }
                End();
                yield return new WaitForSeconds(waitSecs);
                Begin("Return to Pool (deac)");
                for (int num4 = 0; num4 < num; num4++)
                {
                    go[num4].SetActive(value: false);
                }
                End();
                yield return new WaitForSeconds(waitSecs);
                for (int k = 0; k < 2; k++)
                {
                    Begin("Reuse (reac)");
                    for (int num5 = 0; num5 < num; num5++)
                    {
                        go[num5].transform.position = spawnOrigin + Random.insideUnitSphere;
                        SetCollidersEnabled(go[num5], value: true);
                        go[num5].SetActive(value: true);
                    }
                    End();
                    yield return new WaitForSeconds(waitSecs);
                    Begin("Return to Pool (deac)");
                    for (int num6 = 0; num6 < num; num6++)
                    {
                        go[num6].SetActive(value: false);
                    }
                    End();
                    yield return new WaitForSeconds(waitSecs);
                }
                Begin("Destroy");
                for (int num7 = 0; num7 < num; num7++)
                {
                    Object.Destroy(go[num7]);
                }
                End();
            }
        }

        private static void SetCollidersEnabled(GameObject go, bool value)
        {
            Collider[] componentsInChildren = go.GetComponentsInChildren<Collider>(includeInactive: true);
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].enabled = value;
            }
        }

        private static void SetComponentsEnabled(GameObject go, bool value)
        {
            Component[] componentsInChildren = go.GetComponentsInChildren(typeof(Component), includeInactive: true);
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                SetComponentEnabled(componentsInChildren[i], value);
            }
        }

        private static void SetComponentEnabled(Component comp, bool value)
        {
            if (comp is Behaviour)
            {
                (comp as Behaviour).enabled = value;
            }
            else if (comp is Renderer)
            {
                (comp as Renderer).enabled = value;
            }
            else if (comp is Collider)
            {
                (comp as Collider).enabled = value;
            }
            else if (comp is Light)
            {
                (comp as Light).enabled = value;
            }
            else if (comp is LODGroup)
            {
                (comp as LODGroup).enabled = value;
            }
            else if (!(comp is Transform) && !(comp is Rigidbody))
            {
                Debug.LogWarning("Unexpected component: " + comp, comp);
            }
        }
    }
}
