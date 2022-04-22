using UnityEngine;

namespace AssemblyCSharp
{
    public interface IGroundMoveable
    {
        Vector3 GetVelocity();

        bool IsOnGround();

        bool IsActive();

        VFXSurfaceTypes GetGroundSurfaceType();
    }
}
