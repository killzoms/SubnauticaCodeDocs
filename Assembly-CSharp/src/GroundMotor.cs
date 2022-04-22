using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    [AddComponentMenu("Character/Character Motor")]
    public class GroundMotor : PlayerMotor, IGroundMoveable
    {
        [Serializable]
        public class CharacterMotorMovement
        {
            public float maxForwardSpeed = 10f;

            public float maxSidewaysSpeed = 10f;

            public float maxBackwardsSpeed = 10f;

            public AnimationCurve slopeSpeedMultiplier = new AnimationCurve(new Keyframe(-90f, 1f), new Keyframe(0f, 1f), new Keyframe(90f, 0f));

            public float maxFallSpeed = 20f;

            [NonSerialized]
            public CollisionFlags collisionFlags;

            [NonSerialized]
            public Vector3 velocity;

            [NonSerialized]
            public Vector3 frameVelocity = new Vector3(0f, 0f, 0f);

            [NonSerialized]
            public Vector3 hitPoint = new Vector3(0f, 0f, 0f);

            [NonSerialized]
            public Vector3 lastHitPoint = new Vector3(float.PositiveInfinity, 0f, 0f);
        }

        public enum MovementTransferOnJump
        {
            None,
            InitTransfer,
            PermaTransfer,
            PermaLocked
        }

        [Serializable]
        public class CharacterMotorJumping
        {
            public bool enabled = true;

            public float baseHeight = 1f;

            public float extraHeight = 4.1f;

            public float perpAmount;

            public float steepPerpAmount = 0.5f;

            [NonSerialized]
            public bool jumping;

            [NonSerialized]
            public bool holdingJumpButton;

            [NonSerialized]
            public float lastStartTime;

            [NonSerialized]
            public float lastButtonDownTime = -100f;

            [NonSerialized]
            public Vector3 jumpDir = new Vector3(0f, 1f, 0f);
        }

        [Serializable]
        public class CharacterMotorMovingPlatform
        {
            public bool enabled = true;

            public MovementTransferOnJump movementTransfer = MovementTransferOnJump.PermaTransfer;

            [NonSerialized]
            public Transform hitPlatform;

            [NonSerialized]
            public Transform activePlatform;

            [NonSerialized]
            public Vector3 activeLocalPoint;

            [NonSerialized]
            public Vector3 activeGlobalPoint;

            [NonSerialized]
            public Quaternion activeLocalRotation;

            [NonSerialized]
            public Quaternion activeGlobalRotation;

            [NonSerialized]
            public Matrix4x4 lastMatrix;

            [NonSerialized]
            public Vector3 platformVelocity;

            [NonSerialized]
            public bool newPlatform;
        }

        [Serializable]
        public class CharacterMotorSliding
        {
            public bool enabled = true;

            public float slidingSpeed = 15f;

            public float sidewaysControl = 1f;

            public float speedControl = 0.4f;
        }

        [Serializable]
        public class CharacterMotorController
        {
            public float stepOffset = 0.3f;

            public float slopeLimit = 45f;
        }

        public CharacterController controller;

        public CharacterMotorMovement movement = new CharacterMotorMovement();

        public CharacterMotorJumping jumping = new CharacterMotorJumping();

        public CharacterMotorMovingPlatform movingPlatform = new CharacterMotorMovingPlatform();

        public CharacterMotorSliding sliding = new CharacterMotorSliding();

        public CharacterMotorController controllerSetup = new CharacterMotorController();

        [NonSerialized]
        public bool allowMidAirJumping;

        [NonSerialized]
        private Vector3 groundNormal = Vector3.zero;

        private bool sprinting;

        private Vector3 colliderCenter = Vector3.zero;

        private Vector3 velocity = Vector3.zero;

        private Vector3 lastGroundNormal = Vector3.zero;

        private VFXSurfaceTypes groundSurfaceType;

        private void Awake()
        {
            controller = base.gameObject.AddComponent<CharacterController>();
            controller.enabled = false;
            controller.stepOffset = controllerSetup.stepOffset;
            controller.slopeLimit = controllerSetup.slopeLimit;
        }

        public override bool IsSprinting()
        {
            return sprinting;
        }

        public override void SetControllerRadius(float radius)
        {
            controller.radius = radius;
        }

        public override float GetControllerRadius()
        {
            return controller.radius;
        }

        public override void SetControllerHeight(float height)
        {
            if (controller.height != height)
            {
                float num = height - controller.height;
                if (num > 0f)
                {
                    base.transform.localPosition = base.transform.localPosition + Vector3.up * num;
                }
                controller.height = height;
                colliderCenter.y = (0f - controller.height) * 0.5f;
                controller.center = colliderCenter;
            }
        }

        public override float GetControllerHeight()
        {
            return controller.height;
        }

        public override void SetEnabled(bool enabled)
        {
            if (controller != null)
            {
                controller.enabled = enabled;
                if (enabled)
                {
                    movement.velocity = playerController.velocity;
                    movingPlatform.activePlatform = null;
                }
            }
            jumpPressed = false;
            sprintPressed = false;
            base.enabled = enabled;
        }

        private void UpdateFunction()
        {
            sprinting = false;
            if (!canControl)
            {
                return;
            }
            Vector3 vector = movement.velocity;
            Vector3 vector2 = default(Vector3);
            vector2 = movement.velocity;
            vector2 = ApplyInputVelocityChange(vector2);
            vector2 = ApplyGravityAndJumping(vector2);
            if ((bool)movingPlatform.activePlatform && !movingPlatform.activePlatform.gameObject.activeInHierarchy)
            {
                movingPlatform.activePlatform = null;
            }
            Vector3 vector3 = default(Vector3);
            if (MoveWithPlatform())
            {
                vector3 = movingPlatform.activePlatform.TransformPoint(movingPlatform.activeLocalPoint) - movingPlatform.activeGlobalPoint;
                if (vector3 != Vector3.zero)
                {
                    controller.Move(vector3);
                }
                Quaternion quaternion = movingPlatform.activePlatform.rotation * movingPlatform.activeLocalRotation;
                Quaternion quaternion2 = default(Quaternion);
                float y = (quaternion * Quaternion.Inverse(movingPlatform.activeGlobalRotation)).eulerAngles.y;
                if (y != 0f)
                {
                    base.transform.Rotate(0f, y, 0f);
                }
            }
            Vector3 position = base.transform.position;
            Vector3 vector4 = default(Vector3);
            vector4 = vector2 * Time.deltaTime;
            float num = 0f;
            if (!underWater)
            {
                num = Mathf.Max(controller.stepOffset, new Vector3(vector4.x, 0f, vector4.z).magnitude);
                if (grounded)
                {
                    vector4 -= num * Vector3.up;
                }
            }
            movingPlatform.hitPlatform = null;
            groundNormal = Vector3.zero;
            _ = base.transform.position;
            movement.collisionFlags = controller.Move(vector4);
            movement.lastHitPoint = movement.hitPoint;
            lastGroundNormal = groundNormal;
            if (movingPlatform.enabled && movingPlatform.activePlatform != movingPlatform.hitPlatform && movingPlatform.hitPlatform != null)
            {
                movingPlatform.activePlatform = movingPlatform.hitPlatform;
                movingPlatform.lastMatrix = movingPlatform.hitPlatform.localToWorldMatrix;
                movingPlatform.newPlatform = true;
            }
            Vector3 vector5 = new Vector3(vector2.x, 0f, vector2.z);
            Vector3 vector6 = (base.transform.position - position) / Time.deltaTime;
            if (vector6.y > 0f)
            {
                vector6.y = vector2.y;
            }
            movement.velocity = vector6;
            Vector3 lhs = new Vector3(movement.velocity.x, 0f, movement.velocity.z);
            if (vector5 == Vector3.zero)
            {
                movement.velocity = new Vector3(0f, movement.velocity.y, 0f);
            }
            else
            {
                float value = Vector3.Dot(lhs, vector5) / vector5.sqrMagnitude;
                movement.velocity = vector5 * Mathf.Clamp01(value) + movement.velocity.y * Vector3.up;
            }
            if ((double)movement.velocity.y < (double)vector2.y - 0.001)
            {
                if (movement.velocity.y < 0f)
                {
                    movement.velocity.y = vector2.y;
                }
                else
                {
                    jumping.holdingJumpButton = false;
                }
            }
            if (grounded && !IsGroundedTest())
            {
                grounded = false;
                if (movingPlatform.enabled && (movingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer || movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer))
                {
                    movement.frameVelocity = movingPlatform.platformVelocity;
                    movement.velocity += movingPlatform.platformVelocity;
                }
                SendMessage("OnFall", SendMessageOptions.DontRequireReceiver);
                base.transform.position += num * Vector3.up;
            }
            else if (!grounded && IsGroundedTest())
            {
                grounded = true;
                jumping.jumping = false;
                SubtractNewPlatformVelocity();
                SendMessage("OnLand", vector, SendMessageOptions.DontRequireReceiver);
            }
            if (MoveWithPlatform())
            {
                movingPlatform.activeGlobalPoint = base.transform.position + Vector3.up * (controller.center.y - controller.height * 0.5f + controller.radius);
                movingPlatform.activeLocalPoint = movingPlatform.activePlatform.InverseTransformPoint(movingPlatform.activeGlobalPoint);
                movingPlatform.activeGlobalRotation = base.transform.rotation;
                movingPlatform.activeLocalRotation = Quaternion.Inverse(movingPlatform.activePlatform.rotation) * movingPlatform.activeGlobalRotation;
            }
        }

        public override Vector3 UpdateMove()
        {
            if (movingPlatform.enabled && !underWater)
            {
                if (movingPlatform.activePlatform != null && movingPlatform.activePlatform.gameObject.activeInHierarchy)
                {
                    if (!movingPlatform.newPlatform)
                    {
                        movingPlatform.platformVelocity = (movingPlatform.activePlatform.localToWorldMatrix.MultiplyPoint3x4(movingPlatform.activeLocalPoint) - movingPlatform.lastMatrix.MultiplyPoint3x4(movingPlatform.activeLocalPoint)) / Time.deltaTime;
                    }
                    movingPlatform.lastMatrix = movingPlatform.activePlatform.localToWorldMatrix;
                    movingPlatform.newPlatform = false;
                }
                else
                {
                    movingPlatform.platformVelocity = Vector3.zero;
                }
            }
            UpdateFunction();
            return movement.velocity;
        }

        private Vector3 ApplyInputVelocityChange(Vector3 velocity)
        {
            if (playerController == null || playerController.forwardReference == null)
            {
                return Vector3.zero;
            }
            Quaternion quaternion = ((underWater && canSwim) ? playerController.forwardReference.rotation : Quaternion.Euler(0f, playerController.forwardReference.rotation.eulerAngles.y, 0f));
            Vector3 vector = movementInputDirection;
            float num = Mathf.Min(1f, vector.magnitude);
            float num2 = ((underWater && canSwim) ? vector.y : 0f);
            vector.y = 0f;
            vector = quaternion * vector;
            vector.y += num2;
            vector.Normalize();
            Vector3 vector2 = default(Vector3);
            if (grounded && !underWater && TooSteep() && sliding.enabled)
            {
                vector2 = new Vector3(groundNormal.x, 0f, groundNormal.z).normalized;
                Vector3 vector3 = Vector3.Project(movementInputDirection, vector2);
                vector2 = vector2 + vector3 * sliding.speedControl + (movementInputDirection - vector3) * sliding.sidewaysControl;
                vector2 *= sliding.slidingSpeed;
            }
            else
            {
                float maxSpeed = 1f;
                Utils.AdjustSpeedScalarFromWeakness(ref maxSpeed);
                if (!underWater && sprintPressed)
                {
                    maxSpeed *= sprintModifier;
                    sprinting = true;
                }
                vector2 = vector * forwardMaxSpeed * maxSpeed * num;
            }
            if (!underWater && XRSettings.enabled)
            {
                vector2 *= VROptions.groundMoveScale;
            }
            if (!underWater && movingPlatform.enabled && movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)
            {
                vector2 += movement.frameVelocity;
                vector2.y = 0f;
            }
            if (!underWater)
            {
                if (grounded)
                {
                    vector2 = AdjustGroundVelocityToNormal(vector2, groundNormal);
                }
                else
                {
                    velocity.y = 0f;
                }
            }
            float num3 = GetMaxAcceleration(grounded) * Time.deltaTime;
            Vector3 vector4 = default(Vector3);
            vector4 = vector2 - velocity;
            if (vector4.sqrMagnitude > num3 * num3)
            {
                vector4 = vector4.normalized * num3;
            }
            if (grounded || canControl)
            {
                velocity += vector4;
            }
            if (grounded && !underWater)
            {
                velocity.y = Mathf.Min(velocity.y, 0f);
            }
            return velocity;
        }

        private Vector3 ApplyGravityAndJumping(Vector3 velocity)
        {
            if (underWater)
            {
                return velocity;
            }
            if (!jumpPressed || !canControl)
            {
                jumping.holdingJumpButton = false;
                jumping.lastButtonDownTime = -100f;
            }
            if (jumpPressed && (jumping.lastButtonDownTime < 0f || allowMidAirJumping) && canControl)
            {
                jumping.lastButtonDownTime = Time.time;
            }
            if (!grounded)
            {
                velocity.y = movement.velocity.y - gravity * Time.deltaTime;
                velocity.y = Mathf.Max(velocity.y, 0f - movement.maxFallSpeed);
            }
            if (grounded || allowMidAirJumping)
            {
                if (canControl && (double)(Time.time - jumping.lastButtonDownTime) < 0.2)
                {
                    grounded = false;
                    jumping.jumping = true;
                    jumping.lastStartTime = Time.time;
                    jumping.lastButtonDownTime = -100f;
                    jumping.holdingJumpButton = true;
                    if (TooSteep())
                    {
                        jumping.jumpDir = Vector3.Slerp(Vector3.up, groundNormal, jumping.steepPerpAmount);
                    }
                    else
                    {
                        jumping.jumpDir = Vector3.Slerp(Vector3.up, groundNormal, jumping.perpAmount);
                    }
                    velocity.y = 0f;
                    velocity += jumping.jumpDir * CalculateJumpVerticalSpeed(jumping.baseHeight);
                    if (movingPlatform.enabled && movingPlatform.movementTransfer != MovementTransferOnJump.InitTransfer)
                    {
                        _ = movingPlatform.movementTransfer;
                        _ = 2;
                    }
                    SendMessage("OnJump", SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    jumping.holdingJumpButton = false;
                }
            }
            return velocity;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            onLadder = hit.gameObject.CompareTag("Ladder");
            if (hit.normal.y > 0f && hit.normal.y > groundNormal.y && hit.moveDirection.y < 0f)
            {
                if ((double)(hit.point - movement.lastHitPoint).sqrMagnitude > 0.001 || lastGroundNormal == Vector3.zero)
                {
                    groundNormal = hit.normal;
                }
                else
                {
                    groundNormal = lastGroundNormal;
                }
                movingPlatform.hitPlatform = hit.collider.transform;
                movement.hitPoint = hit.point;
                movement.frameVelocity = Vector3.zero;
                VFXSurface component = hit.gameObject.GetComponent<VFXSurface>();
                groundSurfaceType = ((component != null) ? component.surfaceType : VFXSurfaceTypes.none);
            }
        }

        public VFXSurfaceTypes GetGroundSurfaceType()
        {
            return groundSurfaceType;
        }

        private IEnumerator SubtractNewPlatformVelocity()
        {
            if (!movingPlatform.enabled || (movingPlatform.movementTransfer != MovementTransferOnJump.InitTransfer && movingPlatform.movementTransfer != MovementTransferOnJump.PermaTransfer))
            {
                yield break;
            }
            if (movingPlatform.newPlatform)
            {
                Transform platform = movingPlatform.activePlatform;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                if (grounded && platform == movingPlatform.activePlatform)
                {
                    Debug.Log("CharacterMotor.SubtractNewPlatformVelocity() yielding WaitForFixedUpdate (was 1) - seeing weird results?");
                    yield return new WaitForFixedUpdate();
                }
            }
            movement.velocity -= movingPlatform.platformVelocity;
        }

        private bool MoveWithPlatform()
        {
            if (!underWater && movingPlatform.enabled && (grounded || movingPlatform.movementTransfer == MovementTransferOnJump.PermaLocked))
            {
                return movingPlatform.activePlatform != null;
            }
            return false;
        }

        private Vector3 AdjustGroundVelocityToNormal(Vector3 hVelocity, Vector3 groundNormal)
        {
            return Vector3.Cross(Vector3.Cross(Vector3.up, hVelocity), groundNormal).normalized * hVelocity.magnitude;
        }

        private bool IsGroundedTest()
        {
            return (double)groundNormal.y > 0.01;
        }

        private float GetMaxAcceleration(bool grounded)
        {
            if (grounded || underWater)
            {
                return groundAcceleration;
            }
            return airAcceleration;
        }

        private float CalculateJumpVerticalSpeed(float targetJumpHeight)
        {
            JumpGene component = base.gameObject.GetComponent<JumpGene>();
            float num = (component ? (component.Scalar * 5f * targetJumpHeight) : targetJumpHeight);
            return Mathf.Sqrt(2f * num * gravity);
        }

        private bool IsJumping()
        {
            return jumping.jumping;
        }

        private bool IsSliding()
        {
            if (grounded && sliding.enabled)
            {
                return TooSteep();
            }
            return false;
        }

        private bool IsTouchingCeiling()
        {
            return (movement.collisionFlags & CollisionFlags.Above) != 0;
        }

        public bool IsGrounded()
        {
            return grounded;
        }

        private bool TooSteep()
        {
            return groundNormal.y <= Mathf.Cos(controller.slopeLimit * ((float)Math.PI / 180f));
        }

        private Vector3 GetDirection()
        {
            return movementInputDirection;
        }

        private float MaxSpeedInDirection(Vector3 desiredMovementDirection)
        {
            if (desiredMovementDirection == Vector3.zero)
            {
                return 0f;
            }
            float num = ((desiredMovementDirection.z > 0f) ? movement.maxForwardSpeed : movement.maxBackwardsSpeed) / movement.maxSidewaysSpeed;
            Vector3 normalized = new Vector3(desiredMovementDirection.x, 0f, desiredMovementDirection.z / num).normalized;
            return new Vector3(normalized.x, 0f, normalized.z * num).magnitude * movement.maxSidewaysSpeed;
        }

        public Vector3 GetVelocity()
        {
            return movement.velocity;
        }

        public override void SetVelocity(Vector3 velocity)
        {
            movement.velocity = velocity;
        }

        public void OnTeleport()
        {
            movingPlatform.activePlatform = null;
        }

        Vector3 IGroundMoveable.GetVelocity()
        {
            return movement.velocity;
        }

        bool IGroundMoveable.IsOnGround()
        {
            return grounded;
        }

        bool IGroundMoveable.IsActive()
        {
            return base.enabled;
        }
    }
}
