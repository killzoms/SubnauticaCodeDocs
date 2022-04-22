using UnityEngine;

namespace AssemblyCSharp
{
    public class Knife : PlayerTool
    {
        [AssertNotNull]
        public FMODAsset attackSound;

        [AssertNotNull]
        public FMODAsset underwaterMissSound;

        [AssertNotNull]
        public FMODAsset surfaceMissSound;

        public DamageType damageType;

        public float damage = 25f;

        public float attackDist = 2f;

        public VFXEventTypes vfxEventType;

        public override void OnToolUseAnim(GUIHand hand)
        {
            Vector3 position = default(Vector3);
            GameObject closestObj = null;
            global::UWE.Utils.TraceFPSTargetPosition(Player.main.gameObject, attackDist, ref closestObj, ref position);
            if (closestObj == null)
            {
                InteractionVolumeUser component = Player.main.gameObject.GetComponent<InteractionVolumeUser>();
                if (component != null && component.GetMostRecent() != null)
                {
                    closestObj = component.GetMostRecent().gameObject;
                }
            }
            if ((bool)closestObj)
            {
                LiveMixin liveMixin = closestObj.FindAncestor<LiveMixin>();
                if (IsValidTarget(liveMixin))
                {
                    if ((bool)liveMixin)
                    {
                        bool wasAlive = liveMixin.IsAlive();
                        liveMixin.TakeDamage(damage, position, damageType);
                        GiveResourceOnDamage(closestObj, liveMixin.IsAlive(), wasAlive);
                    }
                    Utils.PlayFMODAsset(attackSound, base.transform);
                    VFXSurface component2 = closestObj.GetComponent<VFXSurface>();
                    Vector3 euler = MainCameraControl.main.transform.eulerAngles + new Vector3(300f, 90f, 0f);
                    VFXSurfaceTypeManager.main.Play(component2, vfxEventType, position, Quaternion.Euler(euler), Player.main.transform);
                }
                else
                {
                    closestObj = null;
                }
            }
            if (closestObj == null && hand.GetActiveTarget() == null)
            {
                if (Player.main.IsUnderwater())
                {
                    Utils.PlayFMODAsset(underwaterMissSound, base.transform);
                }
                else
                {
                    Utils.PlayFMODAsset(surfaceMissSound, base.transform);
                }
            }
        }

        private static bool IsValidTarget(LiveMixin liveMixin)
        {
            if (!liveMixin)
            {
                return true;
            }
            if (liveMixin.weldable)
            {
                return false;
            }
            if (!liveMixin.knifeable)
            {
                return false;
            }
            if ((bool)liveMixin.GetComponent<EscapePod>())
            {
                return false;
            }
            return true;
        }

        protected virtual int GetUsesPerHit()
        {
            return 1;
        }

        private void GiveResourceOnDamage(GameObject target, bool isAlive, bool wasAlive)
        {
            TechType techType = CraftData.GetTechType(target);
            HarvestType harvestTypeFromTech = CraftData.GetHarvestTypeFromTech(techType);
            if (techType == TechType.Creepvine)
            {
                GoalManager.main.OnCustomGoalEvent("Cut_Creepvine");
            }
            if ((harvestTypeFromTech == HarvestType.DamageAlive && wasAlive) || (harvestTypeFromTech == HarvestType.DamageDead && !isAlive))
            {
                int num = 1;
                if (harvestTypeFromTech == HarvestType.DamageAlive && !isAlive)
                {
                    num += CraftData.GetHarvestFinalCutBonus(techType);
                }
                TechType harvestOutputData = CraftData.GetHarvestOutputData(techType);
                if (harvestOutputData != 0)
                {
                    CraftData.AddToInventory(harvestOutputData, num, noMessage: false, spawnIfCantAdd: false);
                }
            }
        }
    }
}
