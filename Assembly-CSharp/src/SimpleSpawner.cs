using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class SimpleSpawner : MonoBehaviour
    {
        public void Spawn(Type componentType, int num)
        {
            for (int i = 0; i < num; i++)
            {
                GameObject obj = new GameObject();
                obj.transform.parent = base.transform;
                obj.AddComponent(componentType);
            }
        }

        public void SimpleSendMessage(string message)
        {
            Component[] componentsInChildren = GetComponentsInChildren(typeof(Transform), includeInactive: true);
            ProfilingUtils.BeginSample("SendMessage");
            for (int i = 1; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].gameObject.SendMessage(message, SendMessageOptions.DontRequireReceiver);
            }
            ProfilingUtils.EndSample();
        }

        public void SimpleBroadcastMessage(string message)
        {
            ProfilingUtils.BeginSample("BroadcastMessage");
            base.gameObject.BroadcastMessage(message, SendMessageOptions.DontRequireReceiver);
            ProfilingUtils.EndSample();
        }

        public void SendDo()
        {
            SimpleSendMessage("Do");
        }

        public void BroadcastDo()
        {
            SimpleBroadcastMessage("Do");
        }

        public void SetActive(bool value)
        {
            Component[] componentsInChildren = GetComponentsInChildren(typeof(Transform), includeInactive: true);
            ProfilingUtils.BeginSample("SetActive");
            for (int i = 1; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].gameObject.SetActive(value);
            }
            ProfilingUtils.EndSample();
        }

        public void SetEnabled(bool value)
        {
            MonoBehaviour[] componentsInChildren = GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            ProfilingUtils.BeginSample("SetEnabled");
            for (int i = 1; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].enabled = value;
            }
            ProfilingUtils.EndSample();
        }

        public void Cancel()
        {
            MonoBehaviour[] componentsInChildren = GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            ProfilingUtils.BeginSample("Cancel");
            for (int i = 1; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].CancelInvoke();
            }
            ProfilingUtils.EndSample();
        }

        public void Stop()
        {
            MonoBehaviour[] componentsInChildren = GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            ProfilingUtils.BeginSample("Start");
            for (int i = 1; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].StopCoroutine("Start");
            }
            ProfilingUtils.EndSample();
        }

        public void Destroy()
        {
            Component[] componentsInChildren = GetComponentsInChildren(typeof(Transform), includeInactive: true);
            ProfilingUtils.BeginSample("Destroy");
            for (int i = 1; i < componentsInChildren.Length; i++)
            {
                global::UnityEngine.Object.Destroy(componentsInChildren[i].gameObject);
            }
            ProfilingUtils.EndSample();
        }
    }
}
