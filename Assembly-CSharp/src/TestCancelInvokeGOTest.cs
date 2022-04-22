using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class TestCancelInvokeGOTest : MonoBehaviour
    {
        private List<string> invokeNames = new List<string>();

        private List<GameObject> invokeGOs = new List<GameObject>();

        private bool bCancelFlag;

        private void Start()
        {
            StopwatchProfiler.Instance.ABTestingEnabled = true;
            BuildInvokeNameList();
            MakeInvokeObjects();
            StartCoroutine("LoopTest");
        }

        private void BuildInvokeNameList()
        {
            for (int i = 0; i < 1000; i++)
            {
                string item = $"Dummy{i}";
                invokeNames.Add(item);
            }
        }

        private IEnumerator LoopTest()
        {
            while (true)
            {
                if (StopwatchProfiler.Instance.currentTestVariant == ABTestVariant.A)
                {
                    EnableObjects();
                    yield return new WaitForSeconds(0.5f);
                    DisableObjects();
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    MakeInvokeObjects();
                    yield return new WaitForSeconds(0.5f);
                    DestroyInvokeObjects();
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        private void EnableObjects()
        {
            for (int i = 0; i < invokeGOs.Count; i++)
            {
                invokeGOs[i].SetActive(value: true);
            }
        }

        private void DisableObjects()
        {
            for (int i = 0; i < invokeGOs.Count; i++)
            {
                invokeGOs[i].GetComponent<TestCancelInvoke>().bCancelFlag = bCancelFlag;
                invokeGOs[i].SetActive(value: false);
            }
            bCancelFlag = !bCancelFlag;
        }

        private void MakeInvokeObjects()
        {
            for (int i = 0; i < invokeNames.Count; i++)
            {
                GameObject gameObject = new GameObject(invokeNames[i]);
                gameObject.AddComponent<TestCancelInvoke>();
                invokeGOs.Add(gameObject);
            }
        }

        private void DestroyInvokeObjects()
        {
            while (invokeGOs.Count > 0)
            {
                int index = invokeGOs.Count - 1;
                GameObject obj = invokeGOs[index];
                invokeGOs.RemoveAt(index);
                obj.GetComponent<TestCancelInvoke>().bCancelFlag = bCancelFlag;
                Object.Destroy(obj);
            }
            bCancelFlag = !bCancelFlag;
        }
    }
}
