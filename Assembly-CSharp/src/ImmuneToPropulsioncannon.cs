using UnityEngine;

namespace AssemblyCSharp
{
    public class ImmuneToPropulsioncannon : MonoBehaviour, IPropulsionCannonAmmo, IObstacle
    {
        public bool immuneToRepulsionCannon;

        void IPropulsionCannonAmmo.OnShoot()
        {
        }

        void IPropulsionCannonAmmo.OnRelease()
        {
        }

        void IPropulsionCannonAmmo.OnImpact()
        {
        }

        bool IPropulsionCannonAmmo.GetAllowedToGrab()
        {
            return false;
        }

        bool IPropulsionCannonAmmo.GetAllowedToShoot()
        {
            return !immuneToRepulsionCannon;
        }

        void IPropulsionCannonAmmo.OnGrab()
        {
        }

        public bool CanDeconstruct(out string reason)
        {
            reason = null;
            return false;
        }
    }
}
