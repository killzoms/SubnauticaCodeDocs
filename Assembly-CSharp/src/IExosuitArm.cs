using UnityEngine;

namespace AssemblyCSharp
{
    public interface IExosuitArm
    {
        GameObject GetGameObject();

        void SetSide(Exosuit.Arm arm);

        bool OnUseDown(out float cooldownDuration);

        bool OnUseHeld(out float cooldownDuration);

        bool OnUseUp(out float cooldownDuration);

        bool OnAltDown();

        void Update(ref Quaternion aimDirection);

        void Reset();
    }
}
