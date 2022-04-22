using UnityEngine;

namespace AssemblyCSharp
{
    public class SceneObjectIdentifier : UniqueIdentifier
    {
        public string uniqueName;

        public bool serializeObjectTree;

        public override bool ShouldSerialize(Component comp)
        {
            return true;
        }

        public override bool ShouldCreateEmptyObject()
        {
            return false;
        }

        public override bool ShouldMergeObject()
        {
            return false;
        }

        public override bool ShouldOverridePrefab()
        {
            return false;
        }

        public override bool ShouldStoreClassId()
        {
            return false;
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                SceneObjectManager.Instance.Register(this);
            }
        }
    }
}
