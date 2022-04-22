using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class CrabSquid : Creature
    {
        public OnGroundTracker onGroundTracker;

        public void Update()
        {
            SafeAnimator.SetBool(GetAnimator(), "on_ground", onGroundTracker.onSurface);
            if (onGroundTracker.onSurface)
            {
                Vector3 direction = Vector3.ProjectOnPlane(GetComponent<Rigidbody>().velocity, onGroundTracker.surfaceNormal);
                Vector3 vector = base.transform.InverseTransformDirection(direction);
                SafeAnimator.SetFloat(GetAnimator(), "move_speed_horizontal", vector.x);
                SafeAnimator.SetFloat(GetAnimator(), "move_speed_vertical", vector.z);
            }
        }
    }
}
