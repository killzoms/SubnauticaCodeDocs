using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class CaveCrawler : Creature
    {
        public float animationMaxSpeed = 1f;

        public float animationMaxTilt = 10f;

        public float dampTime = 0.5f;

        public FMODAsset jumpSound;

        public FMOD_CustomLoopingEmitter walkingSound;

        [AssertNotNull]
        public Collider aliveCollider;

        [AssertNotNull]
        public Collider deadCollider;

        [AssertNotNull]
        public OnSurfaceTracker onSurfaceTracker;

        private Vector3 moveDirection = Vector3.forward;

        private float timeLastJump;

        private float jumpMaxHeight;

        private float prevYAngle;

        private static readonly int animTilt = Animator.StringToHash("tilt");

        public void OnJump()
        {
            if (jumpSound != null)
            {
                Utils.PlayFMODAsset(jumpSound, base.transform);
            }
            timeLastJump = Time.time;
        }

        public bool IsOnSurface()
        {
            return onSurfaceTracker.onSurface;
        }

        public Vector3 GetSurfaceNormal()
        {
            return onSurfaceTracker.surfaceNormal;
        }

        public void Update()
        {
            leashPosition.y = base.transform.position.y;
            Rigidbody component = GetComponent<Rigidbody>();
            if (!onSurfaceTracker.onSurface)
            {
                new Vector3(component.velocity.x, 0f, component.velocity.z);
            }
            else
            {
                _ = component.velocity;
            }
            Vector3 vector = base.transform.InverseTransformVector(component.velocity) / animationMaxSpeed;
            Animator animator = GetAnimator();
            SafeAnimator.SetFloat(animator, "move_speed_x", vector.x);
            SafeAnimator.SetFloat(animator, "move_speed_z", vector.z);
            SafeAnimator.SetFloat(animator, "speed", Mathf.Clamp01(Mathf.Sqrt(vector.x * vector.x + vector.z * vector.z)));
            SafeAnimator.SetBool(animator, "jump", Time.time - timeLastJump < 0.2f);
            SafeAnimator.SetFloat(animator, "jump_height", jumpMaxHeight - base.transform.position.y);
            SafeAnimator.SetBool(animator, "on_ground", onSurfaceTracker.onSurface);
            if (Time.deltaTime > 0f)
            {
                float num = Mathf.DeltaAngle(prevYAngle, base.transform.eulerAngles.y) / Time.deltaTime;
                num = Mathf.Clamp(num / animationMaxTilt, -1f, 1f);
                prevYAngle = base.transform.eulerAngles.y;
                animator.SetFloat(animTilt, num, dampTime, Time.deltaTime);
            }
            jumpMaxHeight = (onSurfaceTracker.onSurface ? base.transform.position.y : Mathf.Max(jumpMaxHeight, base.transform.position.y));
            if (onSurfaceTracker.onSurface && component.velocity.sqrMagnitude > 0.01f)
            {
                walkingSound.Play();
            }
            else
            {
                walkingSound.Stop();
            }
        }

        public override void OnKill()
        {
            SafeAnimator.SetBool(GetAnimator(), "dead", value: true);
            aliveCollider.enabled = false;
            deadCollider.enabled = true;
            deadCollider.isTrigger = false;
            base.OnKill();
        }
    }
}
