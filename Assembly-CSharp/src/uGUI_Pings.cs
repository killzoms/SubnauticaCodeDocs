using System;
using System.Collections.Generic;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
    public class uGUI_Pings : MonoBehaviour
    {
        private const ManagedUpdate.Queue updateQueue = ManagedUpdate.Queue.Ping;

        [AssertNotNull]
        public uGUI_Ping prefabPing;

        [AssertNotNull]
        public RectTransform pingCanvas;

        [AssertNotNull]
        public CanvasGroup canvasGroup;

        public Vector2 safeZone = new Vector2(0.1f, 0.1f);

        public float alphaMin;

        public float alphaMax = 0.5f;

        private Dictionary<int, uGUI_Ping> pings = new Dictionary<int, uGUI_Ping>(20);

        private const int poolChunkSize = 4;

        private List<uGUI_Ping> pool = new List<uGUI_Ping>(4);

        private bool visible = true;

        private void OnEnable()
        {
            using (Dictionary<int, PingInstance>.Enumerator pingManagerEnumerator = PingManager.GetEnumerator())
            {
                while (pingManagerEnumerator.MoveNext())
                {
                    KeyValuePair<int, PingInstance> current = pingManagerEnumerator.Current;
                    OnAdd(current.Key, current.Value);
                }
            }

            ManagedUpdate.Subscribe(ManagedUpdate.Queue.Ping, OnWillRenderCanvases);
            PingManager.onAdd = (PingManager.OnAdd)Delegate.Combine(PingManager.onAdd, new PingManager.OnAdd(OnAdd));
            PingManager.onRemove = (PingManager.OnRemove)Delegate.Combine(PingManager.onRemove, new PingManager.OnRemove(OnRemove));
            PingManager.onRename = (PingManager.OnRename)Delegate.Combine(PingManager.onRename, new PingManager.OnRename(OnRename));
            PingManager.onColor = (PingManager.OnColor)Delegate.Combine(PingManager.onColor, new PingManager.OnColor(OnColor));
            PingManager.onVisible = (PingManager.OnVisible)Delegate.Combine(PingManager.onVisible, new PingManager.OnVisible(OnVisible));
        }

        private void OnDisable()
        {
            foreach (KeyValuePair<int, uGUI_Ping> ping in pings)
            {
                ReleaseEntry(ping.Value);
            }
            pings.Clear();
            ManagedUpdate.Unsubscribe(ManagedUpdate.Queue.Ping, OnWillRenderCanvases);
            PingManager.onAdd = (PingManager.OnAdd)Delegate.Remove(PingManager.onAdd, new PingManager.OnAdd(OnAdd));
            PingManager.onRemove = (PingManager.OnRemove)Delegate.Remove(PingManager.onRemove, new PingManager.OnRemove(OnRemove));
            PingManager.onRename = (PingManager.OnRename)Delegate.Remove(PingManager.onRename, new PingManager.OnRename(OnRename));
            PingManager.onColor = (PingManager.OnColor)Delegate.Remove(PingManager.onColor, new PingManager.OnColor(OnColor));
            PingManager.onVisible = (PingManager.OnVisible)Delegate.Remove(PingManager.onVisible, new PingManager.OnVisible(OnVisible));
        }

        private bool IsVisibleNow()
        {
            Player main = Player.main;
            if (main == null)
            {
                return false;
            }
            PDA pDA = main.GetPDA();
            if (pDA == null)
            {
                return false;
            }
            uGUI_PDA main2 = uGUI_PDA.main;
            if (pDA.isInUse && main2 != null && main2.currentTabType != PDATab.Ping)
            {
                return false;
            }
            return true;
        }

        private void OnWillRenderCanvases()
        {
            ProfilingUtils.BeginSample("Pings UI Update");
            try
            {
                bool flag = IsVisibleNow();
                if (visible != flag)
                {
                    visible = flag;
                    canvasGroup.alpha = (visible ? 1f : 0f);
                }
                if (!visible)
                {
                    return;
                }
                Camera camera = MainCamera.camera;
                Transform transform = camera.transform;
                Matrix4x4 worldToLocalMatrix = transform.worldToLocalMatrix;
                float aspect = camera.aspect;
                float num = Mathf.Tan(camera.fieldOfView * 0.5f * ((float)Math.PI / 180f));
                Rect rect = pingCanvas.rect;
                float width = rect.width;
                float height = rect.height;
                Vector3 forward = transform.forward;
                Vector3 position = transform.position;
                Vector3 a = new Vector3(0.5f, 0.5f, 0.5f);
                foreach (KeyValuePair<int, uGUI_Ping> ping in pings)
                {
                    PingInstance pingInstance = PingManager.Get(ping.Key);
                    if (!pingInstance.visible)
                    {
                        continue;
                    }
                    uGUI_Ping value = ping.Value;
                    float minDist = pingInstance.minDist;
                    float maxDist = pingInstance.maxDist;
                    Vector3 position2 = pingInstance.origin.position;
                    Vector3 vector = new Vector3(worldToLocalMatrix.m00 * position2.x + worldToLocalMatrix.m01 * position2.y + worldToLocalMatrix.m02 * position2.z + worldToLocalMatrix.m03, worldToLocalMatrix.m10 * position2.x + worldToLocalMatrix.m11 * position2.y + worldToLocalMatrix.m12 * position2.z + worldToLocalMatrix.m13, worldToLocalMatrix.m20 * position2.x + worldToLocalMatrix.m21 * position2.y + worldToLocalMatrix.m22 * position2.z + worldToLocalMatrix.m23);
                    float magnitude = vector.magnitude;
                    float num2 = vector.z * num;
                    float num3 = num2 * aspect;
                    if (magnitude > minDist && Mathf.Abs(vector.x) < num3 * (1f + safeZone.x) && Mathf.Abs(vector.y) < num2 * (1f + safeZone.y))
                    {
                        Vector2 vector2 = new Vector2(vector.x / num3, vector.y / num2);
                        Vector2 anchoredPosition = new Vector2((vector2.x * 0.5f + 0.5f) * width, (vector2.y * 0.5f + 0.5f) * height);
                        value.rectTransform.anchoredPosition = anchoredPosition;
                        float iconAlpha = ((maxDist > 0f) ? Mathf.Lerp(alphaMin, alphaMax, (magnitude - minDist) / maxDist) : 0f);
                        value.SetIconAlpha(iconAlpha);
                        value.SetDistance(magnitude);
                        Vector3 rhs = position2 - position;
                        rhs.Normalize();
                        bool num4 = Vector3.Dot(forward, rhs) > 0.9848f;
                        float currentFadeAlpha = value.currentFadeAlpha;
                        currentFadeAlpha = ((!num4) ? Mathf.Max(0f, currentFadeAlpha - Time.deltaTime * 3f) : Mathf.Min(1f, currentFadeAlpha + Time.deltaTime * 5f));
                        if (value.currentFadeAlpha != currentFadeAlpha)
                        {
                            value.currentFadeAlpha = currentFadeAlpha;
                            value.SetTextAlpha(currentFadeAlpha);
                            value.rectTransform.localScale = Vector3.Lerp(a, Vector3.one, currentFadeAlpha);
                        }
                    }
                    else
                    {
                        value.SetIconAlpha(0f);
                        value.SetTextAlpha(0f);
                    }
                }
            }
            finally
            {
                ProfilingUtils.EndSample();
            }
        }

        private void OnAdd(int id, PingInstance instance)
        {
            uGUI_Ping entry = GetEntry();
            entry.Initialize();
            entry.SetVisible(instance.visible);
            entry.SetColor(PingManager.colorOptions[instance.colorIndex]);
            entry.SetIcon(SpriteManager.Get(SpriteManager.Group.Pings, PingManager.sCachedPingTypeStrings.Get(instance.pingType)));
            entry.SetLabel(instance.GetLabel());
            entry.SetIconAlpha(0f);
            entry.SetTextAlpha(0f);
            pings.Add(id, entry);
        }

        private void OnRemove(int id)
        {
            if (pings.TryGetValue(id, out var value))
            {
                pings.Remove(id);
                ReleaseEntry(value);
            }
        }

        private void OnRename(int id, PingInstance instance)
        {
            if (!(instance == null) && pings.TryGetValue(id, out var value))
            {
                value.SetLabel(instance.GetLabel());
            }
        }

        private void OnColor(int id, Color color)
        {
            if (pings.TryGetValue(id, out var value))
            {
                value.SetColor(color);
            }
        }

        private void OnVisible(int id, bool visible)
        {
            if (pings.TryGetValue(id, out var value))
            {
                value.SetVisible(visible);
            }
        }

        private uGUI_Ping GetEntry()
        {
            uGUI_Ping component;
            if (pool.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    component = global::UnityEngine.Object.Instantiate(prefabPing.gameObject).GetComponent<uGUI_Ping>();
                    component.rectTransform.SetParent(pingCanvas, worldPositionStays: false);
                    component.rectTransform.anchorMin = Vector2.zero;
                    component.rectTransform.anchorMax = Vector2.zero;
                    component.Uninitialize();
                    pool.Add(component);
                }
            }
            int index = pool.Count - 1;
            component = pool[index];
            pool.RemoveAt(index);
            return component;
        }

        private void ReleaseEntry(uGUI_Ping entry)
        {
            if (!(entry == null))
            {
                entry.Uninitialize();
                pool.Add(entry);
            }
        }
    }
}
