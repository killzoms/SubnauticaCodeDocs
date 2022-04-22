using UnityEngine;

namespace AssemblyCSharp.Exploder
{
    internal class ExploderSettings
    {
        public Vector3 Position;

        public Vector3 ForceVector;

        public float Force;

        public float FrameBudget;

        public float Radius;

        public float DeactivateTimeout;

        public int id;

        public int TargetFragments;

        public DeactivateOptions DeactivateOptions;

        public ExploderObject.FragmentOption FragmentOptions;

        public ExploderObject.SFXOption SfxOptions;

        public ExploderObject.OnExplosion Callback;

        public bool DontUseTag;

        public bool UseForceVector;

        public bool MeshColliders;

        public bool ExplodeSelf;

        public bool HideSelf;

        public bool DestroyOriginalObject;

        public bool ExplodeFragments;

        public bool SplitMeshIslands;

        public bool processing;
    }
}
