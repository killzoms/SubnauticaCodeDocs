using UnityEngine;

namespace AssemblyCSharp
{
    public interface IEcoTarget
    {
        EcoTargetType GetTargetType();

        Vector3 GetPosition();

        string GetName();

        GameObject GetGameObject();
    }
}
