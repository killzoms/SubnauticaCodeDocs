using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public sealed class CrafterLogic : MonoBehaviour, IProtoEventListener
    {
        public delegate void OnItemChanged(TechType techType);

        public delegate void OnProgress(float progress);

        public delegate void OnDone();

        public OnItemChanged onItemChanged;

        public OnProgress onProgress;

        public OnDone onDone;

        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public float timeCraftingBegin = -1f;

        [NonSerialized]
        [ProtoMember(3)]
        public float timeCraftingEnd = -1f;

        [NonSerialized]
        [ProtoMember(4)]
        public TechType craftingTechType;

        [NonSerialized]
        [ProtoMember(5)]
        public int linkedIndex = -1;

        [NonSerialized]
        [ProtoMember(6)]
        public int numCrafted;

        private double lastTime;

        private static List<ICraftTarget> sCraftTargets = new List<ICraftTarget>();

        public bool inProgress
        {
            get
            {
                if (currentTechType != 0)
                {
                    return DayNightCycle.main.timePassed < (double)timeCraftingEnd;
                }
                return false;
            }
        }

        public float progress
        {
            get
            {
                double timePassed = DayNightCycle.main.timePassed;
                double num = timeCraftingEnd - timeCraftingBegin;
                float value = (float)((timePassed - (double)timeCraftingBegin) / num);
                if (!(timeCraftingEnd > timeCraftingBegin))
                {
                    return -1f;
                }
                return Mathf.Clamp01(value);
            }
        }

        public TechType currentTechType
        {
            get
            {
                if (linkedIndex > -1)
                {
                    ITechData techData = CraftData.Get(craftingTechType);
                    if (techData != null && linkedIndex < techData.linkedItemCount)
                    {
                        return techData.GetLinkedItem(linkedIndex);
                    }
                }
                return craftingTechType;
            }
        }

        private void Update()
        {
            if (craftingTechType != 0 && lastTime < (double)timeCraftingEnd)
            {
                NotifyProgress(progress);
                lastTime = DayNightCycle.main.timePassed;
                if (lastTime >= (double)timeCraftingEnd)
                {
                    NotifyEnd();
                }
            }
        }

        public bool Craft(TechType techType, float craftTime)
        {
            if (craftTime > 0f)
            {
                ITechData techData = CraftData.Get(techType);
                if (techData != null)
                {
                    timeCraftingBegin = DayNightCycle.main.timePassedAsFloat;
                    timeCraftingEnd = timeCraftingBegin + craftTime + 0.1f;
                    craftingTechType = techType;
                    linkedIndex = -1;
                    numCrafted = techData.craftAmount;
                    NotifyChanged(craftingTechType);
                    NotifyProgress(0f);
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            timeCraftingBegin = -1f;
            timeCraftingEnd = -1f;
            craftingTechType = TechType.None;
            linkedIndex = -1;
            numCrafted = 0;
            NotifyChanged(TechType.None);
            NotifyProgress(0f);
        }

        public void TryPickup()
        {
            if (craftingTechType == TechType.None || progress < 1f)
            {
                return;
            }
            ITechData techData = CraftData.Get(craftingTechType);
            if (techData != null)
            {
                bool flag = false;
                while (!flag)
                {
                    TechType linkedItem = craftingTechType;
                    if (linkedIndex != -1)
                    {
                        linkedItem = techData.GetLinkedItem(linkedIndex);
                    }
                    while (numCrafted > 0)
                    {
                        if (TryPickupSingle(linkedItem))
                        {
                            numCrafted--;
                            continue;
                        }
                        return;
                    }
                    if (numCrafted == 0)
                    {
                        linkedIndex++;
                        if (linkedIndex < techData.linkedItemCount)
                        {
                            numCrafted = 1;
                            NotifyChanged(currentTechType);
                        }
                        else
                        {
                            flag = true;
                        }
                    }
                }
            }
            Reset();
        }

        private bool TryPickupSingle(TechType techType)
        {
            Inventory main = Inventory.main;
            bool flag = false;
            GameObject gameObject = CraftData.GetPrefabForTechType(techType);
            if (gameObject == null)
            {
                gameObject = Utils.genericLootPrefab;
                flag = true;
            }
            if (gameObject != null)
            {
                Pickupable component = gameObject.GetComponent<Pickupable>();
                if (component != null)
                {
                    Vector2int itemSize = CraftData.GetItemSize(component.GetTechType());
                    if (main.HasRoomFor(itemSize.x, itemSize.y))
                    {
                        GameObject obj = global::UnityEngine.Object.Instantiate(gameObject);
                        component = obj.GetComponent<Pickupable>();
                        if (flag)
                        {
                            component.SetTechTypeOverride(techType, lootCube: true);
                        }
                        NotifyCraftEnd(obj, craftingTechType);
                        main.ForcePickup(component);
                        Player.main.PlayGrab();
                        return true;
                    }
                    ErrorMessage.AddMessage(Language.main.Get("InventoryFull"));
                    return false;
                }
                Debug.LogErrorFormat("Can't find Pickupable component on prefab for TechType.{0}", techType);
                return true;
            }
            Debug.LogErrorFormat("Can't find prefab for TechType.{0}", techType);
            return true;
        }

        private void NotifyChanged(TechType techType)
        {
            if (onItemChanged != null)
            {
                onItemChanged(techType);
            }
        }

        private void NotifyProgress(float progress)
        {
            if (onProgress != null)
            {
                onProgress(progress);
            }
        }

        private void NotifyEnd()
        {
            if (onDone != null)
            {
                onDone();
            }
        }

        public static bool IsCraftRecipeUnlocked(TechType techType)
        {
            if (GameModeUtils.RequiresBlueprints())
            {
                return KnownTech.Contains(techType);
            }
            return true;
        }

        public static bool IsCraftRecipeFulfilled(TechType techType)
        {
            if (Inventory.main == null)
            {
                return false;
            }
            if (!GameModeUtils.RequiresIngredients())
            {
                return true;
            }
            Inventory main = Inventory.main;
            ITechData techData = CraftData.Get(techType);
            if (techData != null)
            {
                int i = 0;
                for (int ingredientCount = techData.ingredientCount; i < ingredientCount; i++)
                {
                    IIngredient ingredient = techData.GetIngredient(i);
                    if (main.GetPickupCount(ingredient.techType) < ingredient.amount)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static bool ConsumeEnergy(PowerRelay powerRelay, float amount)
        {
            if (!GameModeUtils.RequiresPower())
            {
                return true;
            }
            if (powerRelay == null)
            {
                return false;
            }
            float amountConsumed;
            return powerRelay.ConsumeEnergy(amount, out amountConsumed);
        }

        public static bool ConsumeResources(TechType techType)
        {
            if (IsCraftRecipeFulfilled(techType))
            {
                Inventory.main.ConsumeResourcesForRecipe(techType);
                return true;
            }
            ErrorMessage.AddWarning(Language.main.Get("DontHaveNeededIngredients"));
            return false;
        }

        public static void NotifyCraftEnd(GameObject target, TechType techType)
        {
            if (!(target == null))
            {
                sCraftTargets.Clear();
                target.GetComponentsInChildren(includeInactive: true, sCraftTargets);
                int i = 0;
                for (int count = sCraftTargets.Count; i < count; i++)
                {
                    sCraftTargets[i].OnCraftEnd(techType);
                }
                sCraftTargets.Clear();
            }
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            if (craftingTechType != 0)
            {
                ITechData techData = CraftData.Get(craftingTechType);
                if (techData != null)
                {
                    if (linkedIndex != -1 && linkedIndex >= techData.linkedItemCount)
                    {
                        Reset();
                    }
                }
                else
                {
                    Reset();
                }
            }
            DayNightCycle main = DayNightCycle.main;
            if (main != null)
            {
                lastTime = main.timePassed;
            }
            NotifyChanged(currentTechType);
            NotifyProgress(progress);
        }
    }
}
