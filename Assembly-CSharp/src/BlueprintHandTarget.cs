using System;
using System.Collections;
using AssemblyCSharp.Story;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    [RequireComponent(typeof(GenericHandTarget))]
    public class BlueprintHandTarget : MonoBehaviour, ICompileTimeCheckable
    {
        public TechType unlockTechType;

        public string primaryTooltip;

        public string secondaryTooltip;

        [AssertNotNull]
        public StoryGoal onUseGoal;

        public Animator animator;

        public string animParam;

        public string viewAnimParam;

        public FMODAsset useSound;

        public GameObject disableGameObject;

        public GameObject inspectPrefab;

        public float disableDelay = 1f;

        public float viewAnimDuration = 2f;

        [NonSerialized]
        [ProtoMember(1)]
        public bool used;

        private GameObject inspectObject;

        private string alreadyUnlockedTooltip;

        private void Start()
        {
            KnownTech.Contains(unlockTechType);
            if (string.IsNullOrEmpty(primaryTooltip))
            {
                primaryTooltip = unlockTechType.AsString();
            }
            if (string.IsNullOrEmpty(secondaryTooltip))
            {
                string arg = Language.main.Get(unlockTechType);
                string arg2 = Language.main.Get(TooltipFactory.techTypeTooltipStrings.Get(unlockTechType));
                secondaryTooltip = Language.main.GetFormat("DataboxToolipFormat", arg, arg2);
                alreadyUnlockedTooltip = Language.main.GetFormat("DataboxAlreadyUnlockedToolipFormat", arg, arg2);
            }
            else
            {
                alreadyUnlockedTooltip = secondaryTooltip;
            }
            if (!string.IsNullOrEmpty(animParam) && animator != null)
            {
                animator.SetBool(animParam, used);
            }
            if (used && (bool)disableGameObject)
            {
                disableGameObject.SetActive(value: false);
            }
        }

        private void OnDestroy()
        {
            if ((bool)inspectObject)
            {
                global::UnityEngine.Object.Destroy(inspectObject);
                Inventory.main.quickSlots.SetIgnoreHotkeyInput(ignore: false);
                Player.main.GetPDA().SetIgnorePDAInput(ignore: false);
            }
        }

        public void HoverBlueprint()
        {
            if (!used)
            {
                bool flag = KnownTech.Contains(unlockTechType);
                HandReticle.main.SetInteractText(primaryTooltip, flag ? alreadyUnlockedTooltip : secondaryTooltip);
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
            }
        }

        public void UnlockBlueprint()
        {
            if (used)
            {
                return;
            }
            bool flag = false;
            if (!string.IsNullOrEmpty(onUseGoal.key))
            {
                onUseGoal.Trigger();
            }
            if ((bool)useSound)
            {
                Utils.PlayFMODAsset(useSound, base.transform);
            }
            used = true;
            if (!string.IsNullOrEmpty(animParam) && animator != null)
            {
                animator.SetBool(animParam, value: true);
                if ((bool)disableGameObject)
                {
                    if (disableDelay > 0f)
                    {
                        StartDisableGameObject();
                    }
                    else
                    {
                        disableGameObject.SetActive(value: false);
                        flag = !KnownTech.Add(unlockTechType);
                    }
                }
            }
            else
            {
                flag = !KnownTech.Add(unlockTechType);
            }
            if (flag)
            {
                CraftData.AddToInventory(TechType.Titanium, 2);
            }
        }

        private void StartDisableGameObject()
        {
            StartCoroutine(DisableGameObjectAsync());
        }

        private IEnumerator DisableGameObjectAsync()
        {
            bool hasViewAnim = !string.IsNullOrEmpty(viewAnimParam);
            if (hasViewAnim)
            {
                Player.main.armsController.StartHolsterTime(disableDelay + viewAnimDuration);
            }
            yield return new WaitForSeconds(disableDelay);
            disableGameObject.SetActive(value: false);
            bool redundant = !KnownTech.Add(unlockTechType);
            if (hasViewAnim)
            {
                Player.main.armsController.TriggerAnimParam(viewAnimParam, viewAnimDuration);
                if ((bool)inspectPrefab)
                {
                    inspectObject = global::UnityEngine.Object.Instantiate(inspectPrefab);
                    inspectObject.transform.SetParent(Player.main.armsController.leftHandAttach);
                    inspectObject.transform.localPosition = Vector3.zero;
                    inspectObject.transform.localRotation = Quaternion.identity;
                }
                yield return new WaitForSeconds(viewAnimDuration);
                if ((bool)inspectObject)
                {
                    global::UnityEngine.Object.Destroy(inspectObject);
                }
            }
            if (redundant)
            {
                CraftData.AddToInventory(TechType.Titanium, 2);
            }
        }

        public string CompileTimeCheck()
        {
            if (!string.IsNullOrEmpty(onUseGoal.key))
            {
                return StoryGoalUtils.CheckStoryGoal(onUseGoal);
            }
            return null;
        }
    }
}
