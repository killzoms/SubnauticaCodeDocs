using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class FPSCounter : MonoBehaviour
    {
        [AssertNotNull]
        public GUIText text;

        private float timeNextSample = -1f;

        private float timeNextUpdate = -1f;

        private long lastTotalMem;

        private long diffTotalMem;

        private float accumulatedFrameTime;

        private int numAccumulatedFrames;

        private int lastCollectionCount1;

        private int lastCollectionCount2;

        private float lastCollectionTime;

        private float timeBetweenCollections;

        private float avgFrameTime;

        private int numCollections;

        private int numFixedUpdates;

        private int numUpdates;

        private float avgFixedUpdatesPerFrame;

        private void OnConsoleCommand_fps()
        {
            base.enabled = !base.enabled;
        }

        private void Start()
        {
            DevConsole.RegisterConsoleCommand(this, "fps");
            base.enabled = false;
        }

        private void OnEnable()
        {
            text.enabled = true;
        }

        private void OnDisable()
        {
            text.enabled = false;
        }

        private void Update()
        {
            numAccumulatedFrames++;
            numUpdates++;
            accumulatedFrameTime += Time.unscaledDeltaTime;
            bool flag = false;
            if (Time.unscaledTime > timeNextSample)
            {
                SampleTotalMemory();
                timeNextSample = Time.unscaledTime + 1f;
                flag = true;
            }
            if (Time.unscaledTime > timeNextUpdate)
            {
                SampleFrameRate();
                flag = true;
                timeNextUpdate = Time.unscaledTime + 0.1f;
                if (numUpdates > 0)
                {
                    avgFixedUpdatesPerFrame = (float)numFixedUpdates / (float)numUpdates;
                    numUpdates = 0;
                    numFixedUpdates = 0;
                }
            }
            int num = GC.CollectionCount(1);
            int num2 = GC.CollectionCount(2);
            if (num2 > lastCollectionCount2 || num > lastCollectionCount1)
            {
                float unscaledTime = Time.unscaledTime;
                timeBetweenCollections = unscaledTime - lastCollectionTime;
                lastCollectionTime = unscaledTime;
                numCollections++;
                flag = true;
            }
            lastCollectionCount1 = num;
            lastCollectionCount2 = num2;
            if (flag)
            {
                UpdateDisplay();
            }
        }

        private void FixedUpdate()
        {
            numFixedUpdates++;
        }

        private void SampleTotalMemory()
        {
            ProfilingUtils.BeginSample("SampleTotalMemory");
            long totalMemory = GC.GetTotalMemory(forceFullCollection: false);
            diffTotalMem = totalMemory - lastTotalMem;
            lastTotalMem = totalMemory;
            ProfilingUtils.EndSample();
        }

        private void SampleFrameRate()
        {
            avgFrameTime = accumulatedFrameTime / (float)numAccumulatedFrames;
            numAccumulatedFrames = 0;
            accumulatedFrameTime = 0f;
        }

        private void UpdateDisplay()
        {
            ProfilingUtils.BeginSample("UpdateDisplay");
            float num = (float)lastTotalMem / 1048576f;
            float num2 = (float)diffTotalMem / 1048576f;
            text.text = $"{1f / avgFrameTime:N0} FPS {avgFrameTime * 1000f:N2}ms\n{num:N2} MB\n+{num2:N2} MB/s\n{timeBetweenCollections:N0}s between GC ({numCollections})\n{avgFixedUpdatesPerFrame} FixedUpdates per frame";
            ProfilingUtils.EndSample();
        }
    }
}
