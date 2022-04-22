using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class UnderwaterMotor : PlayerMotor
    {
        public Vector3 vel;

        public float stepHeight = 0.3f;

        public bool fastSwimMode;

        [AssertNotNull]
        public CapsuleCollider capsulecollider;

        private float stepAmount;

        private Vector3 desiredVelocity = new Vector3(0f, 0f, 0f);

        private Vector3 surfaceNormal = new Vector3(0f, 1f, 0f);

        private Vector3 colliderCenter = Vector3.zero;

        private float timeLastJump;

        private float currentWreckSpeedMultiplier = 1f;

        private float seaglideWreckMaxSpeed = 4f;

        private const string wreck = "wreck";

        private const float plasteelTankSpeedReduction = 0.2125f;

        private const float tankSpeedReduction = 0.85f;

        private const float doubgleTankSpeedReduction = 1f;

        private const float highCapacityTankSpeedReduction = 1.275f;

        private const float surfaceSpeedMultiplier = 1.3f;

        private const float wreckSpeedMultiplier = 0.5f;

        private const float minusSpeedForReinforcedDiveSuit = 1f;

        private const float minSpeed = 2f;

        private const float equipmentTankSpeedReductionMultiplier = 0.5f;

        public override void SetEnabled(bool enabled)
        {
            if (capsulecollider != null)
            {
                capsulecollider.enabled = enabled;
                if (enabled)
                {
                    rb.isKinematic = false;
                    rb.detectCollisions = true;
                    Vector3 velocity = playerController.velocity;
                    rb.velocity = new Vector3(velocity.x, velocity.y * 0.5f, velocity.z);
                }
                else
                {
                    rb.isKinematic = true;
                    rb.detectCollisions = false;
                }
            }
            jumpPressed = false;
            base.enabled = enabled;
        }

        public override void SetControllerRadius(float radius)
        {
            capsulecollider.radius = radius;
        }

        public override float GetControllerRadius()
        {
            return capsulecollider.radius;
        }

        public override void SetControllerHeight(float height)
        {
            if (height != capsulecollider.height)
            {
                capsulecollider.height = height;
                colliderCenter.y = (0f - capsulecollider.height) * 0.5f;
                capsulecollider.center = colliderCenter;
            }
        }

        public override float GetControllerHeight()
        {
            return capsulecollider.height;
        }

        public override bool IsSprinting()
        {
            return false;
        }

        private float AlterMaxSpeed(float inMaxSpeed)
        {
            float num = inMaxSpeed;
            Inventory main = Inventory.main;
            Equipment equipment = main.equipment;
            ItemsContainer container = main.container;
            switch (equipment.GetTechTypeInSlot("Tank"))
            {
                case TechType.PlasteelTank:
                    num -= 17f / 160f;
                    break;
                case TechType.Tank:
                    num -= 0.425f;
                    break;
                case TechType.DoubleTank:
                    num -= 0.5f;
                    break;
                case TechType.HighCapacityTank:
                    num -= 0.6375f;
                    break;
            }
            int count = container.GetCount(TechType.HighCapacityTank);
            num -= (float)count * 1.275f;
            if (num < 2f)
            {
                num = 2f;
            }
            TechType techTypeInSlot = equipment.GetTechTypeInSlot("Body");
            if (techTypeInSlot == TechType.ReinforcedDiveSuit)
            {
                num = Mathf.Max(2f, num - 1f);
            }
            float maxSpeed = 1f;
            Utils.AdjustSpeedScalarFromWeakness(ref maxSpeed);
            num *= maxSpeed;
            switch (equipment.GetTechTypeInSlot("Foots"))
            {
                case TechType.Fins:
                    num += 1.5f;
                    break;
                case TechType.UltraGlideFins:
                    num += 2.5f;
                    break;
            }
            if (main.GetHeldTool() == null)
            {
                num += 1f;
            }
            if (base.gameObject.transform.position.y > Ocean.main.GetOceanLevel())
            {
                num *= 1.3f;
            }
            float to = 1f;
            if (Player.main.GetBiomeString() == "wreck")
            {
                to = 0.5f;
            }
            currentWreckSpeedMultiplier = global::UWE.Utils.Slerp(currentWreckSpeedMultiplier, to, 0.3f * Time.deltaTime);
            return num * currentWreckSpeedMultiplier;
        }

        public override void SetVelocity(Vector3 velocity)
        {
            if (!fastSwimMode)
            {
                rb.velocity = velocity;
            }
        }

        public override Vector3 UpdateMove()
        {
            Rigidbody rigidbody = rb;
            if (playerController == null || playerController.forwardReference == null)
            {
                return rigidbody.velocity;
            }
            fastSwimMode = Application.isEditor && Input.GetKey(KeyCode.LeftShift);
            Vector3 velocity = rigidbody.velocity;
            Vector3 vector = movementInputDirection;
            float y = vector.y;
            float num = Mathf.Min(1f, vector.magnitude);
            vector.y = 0f;
            vector.Normalize();
            float a = 0f;
            if (vector.z > 0f)
            {
                a = forwardMaxSpeed;
            }
            else if (vector.z < 0f)
            {
                a = 0f - backwardMaxSpeed;
            }
            if (vector.x != 0f)
            {
                a = Mathf.Max(a, strafeMaxSpeed);
            }
            a = Mathf.Max(a, verticalMaxSpeed);
            a = AlterMaxSpeed(a);
            bool num2 = Player.main.GetBiomeString() == "wreck";
            bool flag = Player.main.motorMode == Player.MotorMode.Seaglide;
            if (num2 && flag)
            {
                a = seaglideWreckMaxSpeed;
            }
            a *= Player.main.mesmerizedSpeedMultiplier;
            if (fastSwimMode)
            {
                a *= 1000f;
            }
            float num3 = Mathf.Max(b: a * debugSpeedMult, a: velocity.magnitude);
            Vector3 vector2 = playerController.forwardReference.rotation * vector;
            vector = vector2;
            vector.y += y;
            vector.Normalize();
            if (!canSwim)
            {
                vector.y = 0f;
                vector.Normalize();
            }
            float num4 = airAcceleration;
            if (grounded)
            {
                num4 = groundAcceleration;
            }
            else if (underWater)
            {
                num4 = acceleration;
                if (Player.main.GetBiomeString() == "wreck")
                {
                    num4 *= 0.5f;
                }
                else if (Player.main.motorMode == Player.MotorMode.Seaglide)
                {
                    num4 *= 1.45f;
                }
            }
            float num5 = num * num4 * Time.deltaTime;
            if (num5 > 0f)
            {
                Vector3 vector3 = velocity + vector * num5;
                if (vector3.magnitude > num3)
                {
                    vector3.Normalize();
                    vector3 *= num3;
                }
                float num6 = Vector3.Dot(vector3, surfaceNormal);
                if (!canSwim)
                {
                    vector3 -= num6 * surfaceNormal;
                }
                bool flag2 = vector2.y > 0.6f;
                bool flag3 = vector2.y < -0.3f;
                bool flag4 = y < 0f;
                if (base.transform.position.y >= 0.6f && !flag2 && !flag3 && !flag4)
                {
                    vector3.y = 0f;
                }
                rigidbody.velocity = vector3;
                desiredVelocity = vector3;
            }
            else
            {
                desiredVelocity = rigidbody.velocity;
            }
            float num7 = (underWater ? underWaterGravity : gravity);
            if (num7 != 0f)
            {
                rigidbody.AddForce(new Vector3(0f, (0f - num7) * Time.deltaTime, 0f), ForceMode.VelocityChange);
                usingGravity = true;
            }
            else
            {
                usingGravity = false;
            }
            float drag = airDrag;
            if (underWater)
            {
                drag = swimDrag;
            }
            else if (grounded)
            {
                drag = groundDrag;
            }
            rigidbody.drag = drag;
            InertiaGene component = base.gameObject.GetComponent<InertiaGene>();
            if ((bool)component)
            {
                rigidbody.drag -= component.Scalar * rigidbody.drag;
            }
            if (fastSwimMode)
            {
                rigidbody.drag = 0f;
            }
            grounded = false;
            vel = rigidbody.velocity;
            return vel;
        }

        private void OnCollisionStay(Collision collision)
        {
            _ = grounded;
            grounded = false;
            Vector3 vector = default(Vector3);
            int num = 0;
            for (int i = 0; i < collision.contacts.Length; i++)
            {
                ContactPoint contactPoint = collision.contacts[i];
                if (num == 0)
                {
                    vector = contactPoint.normal;
                }
                else
                {
                    vector += contactPoint.normal;
                }
                num++;
            }
            if (num > 0)
            {
                vector /= (float)num;
                grounded = true;
                if (vector.y > 0.5f)
                {
                    grounded = true;
                }
                surfaceNormal = vector;
            }
            else
            {
                surfaceNormal = new Vector3(0f, 1f, 0f);
            }
        }

        private float CalculateJumpVerticalSpeed()
        {
            return Mathf.Sqrt(2f * jumpHeight * gravity);
        }
    }
}
