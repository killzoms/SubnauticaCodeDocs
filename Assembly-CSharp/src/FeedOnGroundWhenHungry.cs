using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(SwimBehaviour))]
    public class FeedOnGroundWhenHungry : CreatureAction
    {
        private float timeLastFoodFound;

        private Vector3 foodSource;

        public Animator animator;

        public OnGroundTracker onGroundTracker;

        public ConstantForce descendForce;

        public float jumpVelocity = 10f;

        public float swimVelocity = 2f;

        public float swimInterval = 1f;

        public float pauseInterval = 5f;

        public GameObject jump_effect;

        public GameObject trail;

        public GameObject walkFX;

        public FMOD_StudioEventEmitter jumpSound;

        private float timeNextSwim;

        private bool isWalking;

        private float lastJumpTime;

        public override void Awake()
        {
            base.Awake();
        }

        public override float Evaluate(Creature creature)
        {
            if (creature.Hunger.Value >= 0.5f && Time.time > lastJumpTime + pauseInterval && (!isWalking || onGroundTracker.onSurface))
            {
                return GetEvaluatePriority() * Mathf.Lerp(0.5f, 1f, creature.Hunger.Value);
            }
            return 0f;
        }

        private void FindFoodSource()
        {
            foodSource = Random.onUnitSphere * 10f + base.transform.position;
            timeLastFoodFound = Time.time;
        }

        public override void StartPerform(Creature behaviour)
        {
            FindFoodSource();
            descendForce.enabled = true;
            descendForce.force = new Vector3(0f, -10f, 0f);
        }

        public override void StopPerform(Creature behaviour)
        {
            if (isWalking)
            {
                GetComponent<Rigidbody>().AddForce(jumpVelocity * onGroundTracker.surfaceNormal, ForceMode.VelocityChange);
                lastJumpTime = Time.time;
                SafeAnimator.SetBool(animator, "on_ground", value: false);
                if ((bool)walkFX)
                {
                    walkFX.SetActive(value: false);
                }
                if ((bool)jump_effect)
                {
                    Utils.SpawnPrefabAt(jump_effect, null, base.transform.position);
                }
                if ((bool)trail)
                {
                    Utils.SpawnPrefabAt(trail, base.transform, base.transform.position);
                }
                if ((bool)jumpSound)
                {
                    Utils.PlayEnvSound(jumpSound);
                }
            }
            isWalking = false;
            descendForce.enabled = false;
        }

        public override void Perform(Creature b, float deltaTime)
        {
            if (!isWalking && onGroundTracker.onSurface)
            {
                isWalking = true;
                SafeAnimator.SetBool(animator, "on_ground", value: true);
                if ((bool)walkFX)
                {
                    walkFX.SetActive(value: true);
                }
            }
            if (timeLastFoodFound + 10f < Time.time && isWalking)
            {
                foodSource = Random.onUnitSphere * 10f + base.transform.position;
                if (Random.value < 0.5f)
                {
                    creature.Hunger.Add(-0.25f);
                    creature.Happy.Add(0.25f);
                }
                timeLastFoodFound = Time.time;
            }
            if (Time.time > timeNextSwim)
            {
                timeNextSwim = Time.time + swimInterval;
                Vector3 targetPosition = new Vector3(foodSource.x, base.transform.position.y - 1f, foodSource.z);
                base.swimBehaviour.SwimTo(targetPosition, swimVelocity);
            }
            if (isWalking)
            {
                Quaternion b2 = Quaternion.LookRotation(Vector3.ProjectOnPlane(base.transform.forward, onGroundTracker.surfaceNormal));
                base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b2, 2f * deltaTime);
            }
        }

        private void Update()
        {
            if ((bool)animator)
            {
                SafeAnimator.SetBool(animator, "jump", Time.time < lastJumpTime + 0.1f);
            }
        }
    }
}
