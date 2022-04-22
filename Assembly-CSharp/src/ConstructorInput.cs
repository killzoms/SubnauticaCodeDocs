using AssemblyCSharp.Story;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class ConstructorInput : Crafter, IHandTarget
    {
        [AssertNotNull]
        public Material beamMaterial;

        [AssertNotNull]
        public Constructor constructor;

        [AssertNotNull]
        public ConstructorCinematicController cinematicController;

        [AssertNotNull]
        public Texture2D validCraftPositionMap;

        [AssertNotNull]
        public PDANotification invalidNotification;

        private const float kWorldExtents = 2048f;

        protected override void Craft(TechType techType, float duration)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            GetCraftTransform(techType, ref position, ref rotation);
            if (techType != TechType.Seamoth && techType != TechType.Exosuit && !ReturnValidCraftingPosition(position))
            {
                invalidNotification.Play();
            }
            else if (CrafterLogic.ConsumeResources(techType))
            {
                duration = 3f;
                switch (techType)
                {
                    case TechType.RocketBase:
                        duration = 25f;
                        break;
                    case TechType.Cyclops:
                        duration = 20f;
                        break;
                    case TechType.Seamoth:
                    case TechType.Exosuit:
                        duration = 10f;
                        break;
                }
                base.Craft(techType, duration);
            }
        }

        protected override void OnCraftingBegin(TechType techType, float duration)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            GetCraftTransform(techType, ref position, ref rotation);
            if (!GameInput.GetButtonHeld(GameInput.Button.Sprint))
            {
                uGUI.main.craftingMenu.Close(this);
                cinematicController.DisengageConstructor();
            }
            GameObject gameObject = null;
            if (techType == TechType.Cyclops)
            {
                SubConsoleCommand.main.SpawnSub("cyclops", position, rotation);
                FMODUWE.PlayOneShot("event:/tools/constructor/spawn", position);
                gameObject = SubConsoleCommand.main.GetLastCreatedSub();
            }
            else
            {
                gameObject = CraftData.InstantiateFromPrefab(techType);
                Transform component = gameObject.GetComponent<Transform>();
                component.position = position;
                component.rotation = rotation;
            }
            CrafterLogic.NotifyCraftEnd(gameObject, techType);
            ItemGoalTracker.OnConstruct(techType);
            VFXConstructing componentInChildren = gameObject.GetComponentInChildren<VFXConstructing>();
            if (componentInChildren != null)
            {
                componentInChildren.timeToConstruct = duration;
                componentInChildren.StartConstruction();
            }
            if (gameObject.GetComponentInChildren<BuildBotPath>() == null)
            {
                new GameObject("ConstructorBeam").AddComponent<TwoPointLine>().Initialize(beamMaterial, base.transform, gameObject.transform, 0.1f, 1f, duration);
            }
            else
            {
                constructor.SendBuildBots(gameObject);
            }
            LargeWorldEntity.Register(gameObject);
        }

        protected override void OnCraftingEnd()
        {
            if (base.logic != null)
            {
                base.logic.Reset();
            }
        }

        private bool GetPlayerAllowedToUse()
        {
            if (!cinematicController.inUse && !Player.main.IsUnderwater())
            {
                return Player.main.transform.position.y - base.transform.position.y > 0.3f;
            }
            return false;
        }

        private void GetCraftTransform(TechType techType, ref Vector3 position, ref Quaternion rotation)
        {
            Transform itemSpawnPoint = constructor.GetItemSpawnPoint(techType);
            position = itemSpawnPoint.position;
            rotation = itemSpawnPoint.rotation;
        }

        private bool ReturnValidCraftingPosition(Vector3 pollPosition)
        {
            float num = Mathf.Clamp01((pollPosition.x + 2048f) / 4096f);
            float num2 = Mathf.Clamp01((pollPosition.z + 2048f) / 4096f);
            int x = (int)(num * (float)validCraftPositionMap.width);
            int y = (int)(num2 * (float)validCraftPositionMap.height);
            return validCraftPositionMap.GetPixel(x, y).g > 0.5f;
        }

        public void OnHandHover(GUIHand hand)
        {
            if (GetPlayerAllowedToUse())
            {
                HandReticle.main.SetInteractText("UseConstructor");
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            if (!(base.logic == null) && !base.logic.inProgress && GetPlayerAllowedToUse())
            {
                cinematicController.EngageConstructor(hand.player);
            }
        }

        public void StartUse()
        {
            uGUI.main.craftingMenu.Open(CraftTree.Type.Constructor, this);
            constructor.usingMenu = true;
        }

        public void EndUse()
        {
            uGUI.main.craftingMenu.Close(this);
            constructor.usingMenu = false;
        }
    }
}
