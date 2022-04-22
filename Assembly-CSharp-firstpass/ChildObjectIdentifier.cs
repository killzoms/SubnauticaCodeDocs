using UnityEngine;

public class ChildObjectIdentifier : UniqueIdentifier, ICompileTimeCheckable
{
    public override bool ShouldSerialize(Component comp)
    {
        if (!Application.isPlaying && !UniqueIdentifier.IsTestingPlayMode)
        {
            return false;
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
        return true;
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

    public string CompileTimeCheck()
    {
        Transform parent = base.transform.parent;
        if (!parent)
        {
            return null;
        }
        if (!parent.GetComponent<UniqueIdentifier>())
        {
            return $"ChildObjectIdentifier {base.name}'s parent {parent.name} has no unique identifier.";
        }
        return null;
    }
}
