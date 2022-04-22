using System;
using System.Collections.Generic;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
    [RequireComponent(typeof(LiveMixin))]
    public class Leakable : MonoBehaviour
    {
        public int numGraceLeakPoints = 2;

        public GameObject leakRoot;

        private List<VFXSubLeakPoint> unusedLeakPoints = new List<VFXSubLeakPoint>();

        private List<VFXSubLeakPoint> leakingLeakPoints = new List<VFXSubLeakPoint>();

        [NonSerialized]
        public Vector3 lastDmgPoint;

        private LiveMixin live;

        public static int ComputeNumLeakPoints(float healthFrac, int totalPts)
        {
            return Mathf.CeilToInt(Mathf.Clamp((1f - healthFrac) * (float)totalPts, 0f, totalPts));
        }

        public bool IsLeaking()
        {
            return leakingLeakPoints.Count >= numGraceLeakPoints;
        }

        public void RefreshLeakPoints()
        {
            foreach (VFXSubLeakPoint leakingLeakPoint in leakingLeakPoints)
            {
                if (leakingLeakPoint != null)
                {
                    leakingLeakPoint.Stop();
                }
            }
            leakingLeakPoints.Clear();
            unusedLeakPoints.Clear();
            VFXSubLeakPoint[] componentsInChildren = leakRoot.GetComponentsInChildren<VFXSubLeakPoint>(includeInactive: true);
            foreach (VFXSubLeakPoint vFXSubLeakPoint in componentsInChildren)
            {
                if (vFXSubLeakPoint != null)
                {
                    unusedLeakPoints.Add(vFXSubLeakPoint);
                }
            }
        }

        private void Awake()
        {
            live = GetComponent<LiveMixin>();
            RefreshLeakPoints();
            DevConsole.RegisterConsoleCommand(this, "damagebase");
        }

        private void OnConsoleCommand_damagebase(NotificationCenter.Notification n)
        {
            float result = 20f;
            if (n.data.Count > 0)
            {
                float.TryParse((string)n.data[0], out result);
            }
            live.TakeDamage(result);
        }

        public int GetLeakCount()
        {
            return Mathf.Max(0, leakingLeakPoints.Count - numGraceLeakPoints);
        }

        public int GetMinorCount()
        {
            return leakingLeakPoints.Count - GetLeakCount();
        }

        private void SpringNearestLeak(Vector3 point)
        {
            VFXSubLeakPoint vFXSubLeakPoint = null;
            float num = 10000f;
            int num2 = -1;
            for (int i = 0; i < unusedLeakPoints.Count; i++)
            {
                float magnitude = (unusedLeakPoints[i].transform.position - point).magnitude;
                if (num2 == -1 || magnitude < num)
                {
                    vFXSubLeakPoint = unusedLeakPoints[i];
                    num = magnitude;
                    num2 = i;
                }
            }
            if (vFXSubLeakPoint != null)
            {
                vFXSubLeakPoint.Play();
                leakingLeakPoints.Add(vFXSubLeakPoint);
                unusedLeakPoints.RemoveAt(num2);
                if (leakingLeakPoints.Count > numGraceLeakPoints)
                {
                    vFXSubLeakPoint.StartSpray();
                }
            }
        }

        private void PlugNearestLeak(Vector3 point)
        {
            VFXSubLeakPoint vFXSubLeakPoint = null;
            float num = 10000f;
            int num2 = -1;
            for (int i = 0; i < leakingLeakPoints.Count; i++)
            {
                float magnitude = (leakingLeakPoints[i].transform.position - point).magnitude;
                if (num2 == -1 || magnitude < num)
                {
                    vFXSubLeakPoint = leakingLeakPoints[i];
                    num = magnitude;
                    num2 = i;
                }
            }
            if (vFXSubLeakPoint != null)
            {
                vFXSubLeakPoint.Stop();
                leakingLeakPoints.RemoveAt(num2);
                unusedLeakPoints.Add(vFXSubLeakPoint);
            }
        }

        public void UpdateLeakPoints()
        {
            for (int i = 0; i < unusedLeakPoints.Count; i++)
            {
                if (unusedLeakPoints[i] == null)
                {
                    unusedLeakPoints.RemoveAt(i);
                }
            }
            int totalPts = unusedLeakPoints.Count + leakingLeakPoints.Count;
            int num = ComputeNumLeakPoints(live.GetHealthFraction(), totalPts);
            while (leakingLeakPoints.Count < num)
            {
                SpringNearestLeak(lastDmgPoint);
            }
            bool flag = false;
            while (leakingLeakPoints.Count > num)
            {
                PlugNearestLeak(MainCamera.camera.transform.position);
                flag = true;
            }
            if (!flag)
            {
                return;
            }
            for (int j = 0; j < leakingLeakPoints.Count; j++)
            {
                VFXSubLeakPoint vFXSubLeakPoint = leakingLeakPoints[j];
                if (j < numGraceLeakPoints)
                {
                    vFXSubLeakPoint.StopSpray();
                }
                else
                {
                    vFXSubLeakPoint.StartSpray();
                }
            }
        }
    }
}
