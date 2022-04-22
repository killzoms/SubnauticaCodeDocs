using UnityEngine;

namespace AssemblyCSharp
{
    public class MeleeAttack : MonoBehaviour, IManagedUpdateBehaviour, IManagedBehaviour
    {
        public float biteAggressionThreshold = 0.3f;

        public float biteInterval = 1f;

        public float biteDamage = 30f;

        protected float timeLastBite;

        public float eatHungerDecrement = 0.5f;

        public float eatHappyIncrement = 0.5f;

        public float biteAggressionDecrement = 0.4f;

        public FMOD_StudioEventEmitter attackSound;

        [AssertNotNull]
        public GameObject mouth;

        [AssertNotNull]
        public LastTarget lastTarget;

        [AssertNotNull]
        public Creature creature;

        [AssertNotNull]
        public LiveMixin liveMixin;

        public GameObject damageFX;

        [AssertNotNull]
        public Animator animator;

        public bool canBeFed = true;

        public bool ignoreSameKind;

        public bool canBiteCreature = true;

        public bool canBitePlayer = true;

        public bool canBiteVehicle;

        public bool canBiteCyclops;

        protected bool frozen;

        private static int biteAnimID = Animator.StringToHash("bite");

        private bool wasBiting;

        private bool initBiting;

        public int managedUpdateIndex { get; set; }

        public string GetProfileTag()
        {
            return "MeleeAttack";
        }

        private void OnEnable()
        {
            BehaviourUpdateUtils.Register(this);
        }

        protected virtual void OnDisable()
        {
            BehaviourUpdateUtils.Deregister(this);
        }

        private void OnDestroy()
        {
            BehaviourUpdateUtils.Deregister(this);
        }

        public virtual bool CanBite(GameObject target)
        {
            Player component = target.GetComponent<Player>();
            if (frozen)
            {
                return false;
            }
            if (component != null && !component.CanBeAttacked())
            {
                return false;
            }
            if (creature.Aggression.Value < biteAggressionThreshold)
            {
                return false;
            }
            if (Time.time < timeLastBite + biteInterval)
            {
                return false;
            }
            bool flag = target.GetComponent<SubControl>() != null;
            if (flag && target != lastTarget.target)
            {
                return false;
            }
            if ((!canBitePlayer || component == null) && (!canBiteCreature || target.GetComponent<Creature>() == null) && (!canBiteVehicle || target.GetComponent<Vehicle>() == null) && (!canBiteCyclops || (!flag && target.GetComponent<CyclopsDecoy>() == null)))
            {
                return false;
            }
            Vector3 direction = target.transform.position - base.transform.position;
            float magnitude = direction.magnitude;
            int num = global::UWE.Utils.RaycastIntoSharedBuffer(base.transform.position, direction, magnitude, -5, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < num; i++)
            {
                Collider collider = global::UWE.Utils.sharedHitBuffer[i].collider;
                GameObject gameObject = ((collider.attachedRigidbody != null) ? collider.attachedRigidbody.gameObject : collider.gameObject);
                if (!(gameObject == target) && !(gameObject == base.gameObject) && !(gameObject.GetComponent<Creature>() != null))
                {
                    return false;
                }
            }
            return true;
        }

        public virtual bool CanEat(BehaviourType behaviourType, bool holdingByPlayer = false)
        {
            BehaviourType behaviourType2 = BehaviourData.GetBehaviourType(base.gameObject);
            switch (behaviourType)
            {
                case BehaviourType.MediumFish:
                    if (!holdingByPlayer)
                    {
                        return behaviourType2 == BehaviourType.Shark;
                    }
                    return true;
                default:
                    return false;
                case BehaviourType.SmallFish:
                    return true;
            }
        }

        protected virtual float GetBiteDamage(GameObject target)
        {
            return biteDamage;
        }

        public GameObject GetTarget(Collider collider)
        {
            GameObject gameObject = collider.gameObject;
            if (gameObject.GetComponent<LiveMixin>() == null && collider.attachedRigidbody != null)
            {
                gameObject = collider.attachedRigidbody.gameObject;
            }
            return gameObject;
        }

        protected bool TryEat(GameObject preyGameObject, bool holdingByPlayer = false)
        {
            bool result = false;
            BehaviourType behaviourType = BehaviourData.GetBehaviourType(preyGameObject);
            if (CanEat(behaviourType, holdingByPlayer))
            {
                SendMessage("OnFishEat", preyGameObject, SendMessageOptions.DontRequireReceiver);
                float num = 1f;
                if (behaviourType == BehaviourType.MediumFish)
                {
                    num = 1.5f;
                }
                if (behaviourType == BehaviourType.Shark)
                {
                    num = 3f;
                }
                if (preyGameObject.GetComponent<Creature>() != null)
                {
                    Object.Destroy(preyGameObject);
                }
                creature.Hunger.Add((0f - eatHungerDecrement) * num);
                creature.Happy.Add(eatHappyIncrement * num);
                Peeper component = preyGameObject.GetComponent<Peeper>();
                if (component != null && component.isHero)
                {
                    InfectedMixin component2 = GetComponent<InfectedMixin>();
                    if (component2 != null)
                    {
                        component2.Heal(0.5f);
                    }
                }
                result = true;
            }
            return result;
        }

        public virtual void OnTouch(Collider collider)
        {
            if (!base.enabled)
            {
                return;
            }
            GameObject target = GetTarget(collider);
            if ((ignoreSameKind && Utils.CompareTechType(base.gameObject, target)) || !liveMixin.IsAlive())
            {
                return;
            }
            Player component = target.GetComponent<Player>();
            if (component != null && canBeFed && component.CanBeAttacked())
            {
                Pickupable held = Inventory.main.GetHeld();
                if (held != null && TryEat(held.gameObject, holdingByPlayer: true))
                {
                    return;
                }
            }
            if (CanBite(target))
            {
                timeLastBite = Time.time;
                LiveMixin component2 = target.GetComponent<LiveMixin>();
                if (component2 != null && component2.IsAlive())
                {
                    component2.TakeDamage(GetBiteDamage(target));
                    component2.NotifyCreatureDeathsOfCreatureAttack();
                }
                Vector3 position = collider.ClosestPointOnBounds(mouth.transform.position);
                if (damageFX != null)
                {
                    Object.Instantiate(damageFX, position, damageFX.transform.rotation);
                }
                if (attackSound != null)
                {
                    Utils.PlayEnvSound(attackSound, position);
                }
                creature.Aggression.Add(0f - biteAggressionDecrement);
                if (component2 != null && !component2.IsAlive())
                {
                    TryEat(component2.gameObject);
                }
                base.gameObject.SendMessage("OnMeleeAttack", target, SendMessageOptions.DontRequireReceiver);
            }
        }

        public void ManagedUpdate()
        {
            bool flag = Time.time - timeLastBite < 0.2f;
            if (flag != wasBiting || !initBiting)
            {
                animator.SetBool(biteAnimID, flag);
            }
            wasBiting = flag;
            initBiting = true;
        }

        public void OnFreeze()
        {
            frozen = true;
        }

        public void OnUnfreeze()
        {
            frozen = false;
        }
    }
}
