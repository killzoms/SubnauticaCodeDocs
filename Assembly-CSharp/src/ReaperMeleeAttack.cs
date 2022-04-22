using UnityEngine;

namespace AssemblyCSharp
{
    public class ReaperMeleeAttack : MeleeAttack
    {
        [AssertNotNull]
        public PlayerCinematicController playerDeathCinematic;

        public FMOD_CustomEmitter playerAttackSound;

        public float cyclopsDamage = 160f;

        public override bool CanEat(BehaviourType behaviourType, bool holdingByPlayer = false)
        {
            if (behaviourType != BehaviourType.Shark && behaviourType != BehaviourType.MediumFish)
            {
                return behaviourType == BehaviourType.SmallFish;
            }
            return true;
        }

        protected override float GetBiteDamage(GameObject target)
        {
            if (target.GetComponent<SubControl>() != null)
            {
                return cyclopsDamage;
            }
            return base.GetBiteDamage(target);
        }

        public override void OnTouch(Collider collider)
        {
            if (!liveMixin.IsAlive() || !(Time.time > timeLastBite + biteInterval))
            {
                return;
            }
            Creature component = GetComponent<Creature>();
            if (!(component.Aggression.Value >= 0.5f))
            {
                return;
            }
            GameObject target = GetTarget(collider);
            ReaperLeviathan component2 = GetComponent<ReaperLeviathan>();
            if (component2.IsHoldingVehicle() || playerDeathCinematic.IsCinematicModeActive())
            {
                return;
            }
            Player component3 = target.GetComponent<Player>();
            if (component3 != null)
            {
                if (component3.CanBeAttacked() && !component3.cinematicModeActive)
                {
                    float num = DamageSystem.CalculateDamage(biteDamage, DamageType.Normal, component3.gameObject);
                    if (component3.GetComponent<LiveMixin>().health - num <= 0f)
                    {
                        playerDeathCinematic.StartCinematicMode(component3);
                        if ((bool)playerAttackSound)
                        {
                            playerAttackSound.Play();
                        }
                    }
                }
            }
            else if (component2.GetCanGrabVehicle())
            {
                SeaMoth component4 = target.GetComponent<SeaMoth>();
                if ((bool)component4 && !component4.docked)
                {
                    component2.GrabSeamoth(component4);
                }
                Exosuit component5 = target.GetComponent<Exosuit>();
                if ((bool)component5 && !component5.docked)
                {
                    component2.GrabExosuit(component5);
                }
            }
            base.OnTouch(collider);
            component.Aggression.Value -= 0.25f;
        }
    }
}
