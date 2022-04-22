using UnityEngine;

public class PrefabIdentifier : UniqueIdentifier
{
    public override bool ShouldSerialize(Component comp)
    {
        if (!Application.isPlaying && !UniqueIdentifier.IsTestingPlayMode)
        {
            if (!(comp is Transform))
            {
                return comp is ISerializeInEditMode;
            }
            return true;
        }
        if (comp is Collider)
        {
            return false;
        }
        if (comp is Light)
        {
            return false;
        }
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
        if (!Application.isPlaying)
        {
            return UniqueIdentifier.IsTestingPlayMode;
        }
        return true;
    }

    public override bool ShouldStoreClassId()
    {
        return true;
    }
}
