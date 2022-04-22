using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp.Exploder
{
    public class ExploderQueue
    {
        private readonly Queue<ExploderSettings> queue;

        private readonly ExploderObject exploder;

        public ExploderQueue(ExploderObject exploder)
        {
            this.exploder = exploder;
            queue = new Queue<ExploderSettings>();
        }

        public void Explode(ExploderObject.OnExplosion callback)
        {
            ExploderSettings item = new ExploderSettings
            {
                Position = ExploderUtils.GetCentroid(exploder.gameObject),
                DontUseTag = exploder.DontUseTag,
                Radius = exploder.Radius,
                ForceVector = exploder.ForceVector,
                UseForceVector = exploder.UseForceVector,
                Force = exploder.Force,
                FrameBudget = exploder.FrameBudget,
                TargetFragments = exploder.TargetFragments,
                DeactivateOptions = exploder.DeactivateOptions,
                DeactivateTimeout = exploder.DeactivateTimeout,
                MeshColliders = exploder.MeshColliders,
                ExplodeSelf = exploder.ExplodeSelf,
                HideSelf = exploder.HideSelf,
                DestroyOriginalObject = exploder.DestroyOriginalObject,
                ExplodeFragments = exploder.ExplodeFragments,
                SplitMeshIslands = exploder.SplitMeshIslands,
                FragmentOptions = exploder.FragmentOptions.Clone(),
                SfxOptions = exploder.SFXOptions.Clone(),
                Callback = callback,
                processing = false
            };
            queue.Enqueue(item);
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            if (queue.Count > 0)
            {
                ExploderSettings exploderSettings = queue.Peek();
                if (!exploderSettings.processing)
                {
                    exploder.DontUseTag = exploderSettings.DontUseTag;
                    exploder.Radius = exploderSettings.Radius;
                    exploder.ForceVector = exploderSettings.ForceVector;
                    exploder.UseForceVector = exploderSettings.UseForceVector;
                    exploder.Force = exploderSettings.Force;
                    exploder.FrameBudget = exploderSettings.FrameBudget;
                    exploder.TargetFragments = exploderSettings.TargetFragments;
                    exploder.DeactivateOptions = exploderSettings.DeactivateOptions;
                    exploder.DeactivateTimeout = exploderSettings.DeactivateTimeout;
                    exploder.MeshColliders = exploderSettings.MeshColliders;
                    exploder.ExplodeSelf = exploderSettings.ExplodeSelf;
                    exploder.HideSelf = exploderSettings.HideSelf;
                    exploder.DestroyOriginalObject = exploderSettings.DestroyOriginalObject;
                    exploder.ExplodeFragments = exploderSettings.ExplodeFragments;
                    exploder.SplitMeshIslands = exploderSettings.SplitMeshIslands;
                    exploder.FragmentOptions = exploderSettings.FragmentOptions;
                    exploder.SFXOptions = exploderSettings.SfxOptions;
                    exploderSettings.id = Random.Range(int.MinValue, int.MaxValue);
                    exploderSettings.processing = true;
                    exploder.StartExplosionFromQueue(exploderSettings.Position, exploderSettings.id, exploderSettings.Callback);
                }
            }
        }

        public void OnExplosionFinished(int id)
        {
            queue.Dequeue();
            ProcessQueue();
        }
    }
}
