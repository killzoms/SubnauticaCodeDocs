using UnityEngine;

namespace AssemblyCSharp
{
    public interface IBuilderGhostModel
    {
        void UpdateGhostModelColor(bool allowed, ref Color color);
    }
}
