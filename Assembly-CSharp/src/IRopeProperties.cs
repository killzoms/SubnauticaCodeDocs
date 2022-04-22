using UnityEngine;

namespace AssemblyCSharp
{
    public interface IRopeProperties
    {
        Vector3 GetStartPosition();

        Vector3 GetEndPosition();

        float GetLength();
    }
}
