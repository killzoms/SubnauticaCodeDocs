using System.Collections.Generic;
using System.Text;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    public static class TooltipFactory
    {
        private static bool initialized;

        private static string stringBatteryNotInserted;

        private static string stringLockedRecipeHint;

        private static string stringUse;

        private static string stringEat;

        private static string stringEquip;

        private static string stringUnequip;

        private static string stringAssignQuickSlot;

        private static string stringBindQuickSlot;

        private static string stringSwitchContainer;

        private static string stringSwapItems;

        private static string stringDrop;

        private static string stringPlace;

        private static string stringKeyRange15;

        private static string stringLeftHand;

        private static string stringRightHand;

        public static readonly CachedEnumString<TechType> techTypeTooltipStrings = new CachedEnumString<TechType>("Tooltip_", TechTypeExtensions.sTechTypeComparer);

        public static readonly CachedEnumString<TechType> techTypeIngredientStrings = new CachedEnumString<TechType>(string.Empty, ".TooltipIngredient", TechTypeExtensions.sTechTypeComparer);

        private static void Initialize()
        {
            if (!initialized)
            {
                initialized = true;
                Language.main.OnLanguageChanged += OnLanguageChanged;
                RefreshActionStrings();
                GameInput.OnBindingsChanged += OnBindingsChanged;
                RefreshBindingStrings();
            }
        }

        private static void OnLanguageChanged()
        {
            RefreshActionStrings();
            RefreshBindingStrings();
        }

        private static void RefreshActionStrings()
        {
            Language main = Language.main;
            stringBatteryNotInserted = main.Get("BatteryNotInserted");
            stringLockedRecipeHint = main.Get("LockedRecipeHint");
            stringUse = main.Get("Use");
            stringEat = main.Get("Eat");
            stringEquip = main.Get("Equip");
            stringUnequip = main.Get("Unequip");
            stringAssignQuickSlot = main.Get("AssignQuickSlot");
            stringBindQuickSlot = main.Get("BindQuickSlot");
            stringSwitchContainer = main.Get("SwitchContainer");
            stringSwapItems = main.Get("SwapItems");
            stringDrop = main.Get("Drop");
            stringPlace = main.Get("Place");
            stringKeyRange15 = main.Get("KeyRange15");
        }

        private static void OnBindingsChanged()
        {
            RefreshBindingStrings();
        }

        private static void RefreshBindingStrings()
        {
            stringLeftHand = uGUI.FormatButton(GameInput.Button.LeftHand);
            stringRightHand = uGUI.FormatButton(GameInput.Button.RightHand);
        }

        public static string Label(string label)
        {
            Initialize();
            StringBuilder stringBuilder = new StringBuilder();
            WriteTitle(stringBuilder, Language.main.Get(label));
            return stringBuilder.ToString();
        }

        public static string InventoryItem(InventoryItem item)
        {
            Initialize();
            StringBuilder stringBuilder = new StringBuilder();
            Pickupable item2 = item.item;
            ItemCommons(stringBuilder, item2.GetTechType(), item2.gameObject);
            ItemActions(stringBuilder, item);
            return stringBuilder.ToString();
        }

        public static string InventoryItemView(InventoryItem item)
        {
            Initialize();
            StringBuilder stringBuilder = new StringBuilder();
            Pickupable item2 = item.item;
            ItemCommons(stringBuilder, item2.GetTechType(), item2.gameObject);
            return stringBuilder.ToString();
        }

        public static void QuickSlot(TechType techType, GameObject obj, out string text)
        {
            Initialize();
            StringBuilder stringBuilder = new StringBuilder();
            ItemCommons(stringBuilder, techType, obj);
            text = stringBuilder.ToString();
        }

        public static void Recipe(TechType techType, bool locked, out string tooltipText, List<TooltipIcon> tooltipIcons)
        {
            Initialize();
            StringBuilder stringBuilder = new StringBuilder();
            if (locked)
            {
                WriteTitle(stringBuilder, Language.main.Get(techType));
                WriteDescription(stringBuilder, stringLockedRecipeHint);
                tooltipText = stringBuilder.ToString();
                return;
            }
            ITechData techData = CraftData.Get(techType);
            string text = Language.main.Get(techType);
            int num = techData?.craftAmount ?? 1;
            if (num > 1)
            {
                text = Language.main.GetFormat("CraftMultipleFormat", text, num);
            }
            WriteTitle(stringBuilder, text);
            WriteDescription(stringBuilder, Language.main.Get(techTypeTooltipStrings.Get(techType)));
            if (techData != null)
            {
                WriteIngredients(techData, tooltipIcons);
            }
            tooltipText = stringBuilder.ToString();
        }

        public static void BuildTech(TechType techType, bool locked, out string tooltipText, List<TooltipIcon> tooltipIcons)
        {
            Initialize();
            StringBuilder stringBuilder = new StringBuilder();
            string key = techType.AsString();
            if (locked)
            {
                WriteTitle(stringBuilder, Language.main.Get(key));
                WriteDescription(stringBuilder, stringLockedRecipeHint);
                tooltipText = stringBuilder.ToString();
                return;
            }
            WriteTitle(stringBuilder, Language.main.Get(key));
            WriteDescription(stringBuilder, Language.main.Get(techTypeTooltipStrings.Get(techType)));
            ITechData techData = CraftData.Get(techType, skipWarnings: true);
            if (techData != null)
            {
                WriteIngredients(techData, tooltipIcons);
            }
            tooltipText = stringBuilder.ToString();
        }

        private static void ItemCommons(StringBuilder sb, TechType techType, GameObject obj)
        {
            string text = Language.main.Get(techType);
            Creature component = obj.GetComponent<Creature>();
            if (component != null)
            {
                LiveMixin liveMixin = component.liveMixin;
                if (liveMixin != null && !liveMixin.IsAlive())
                {
                    text = Language.main.GetFormat("DeadFormat", text);
                }
            }
            Eatable component2 = obj.GetComponent<Eatable>();
            if (component2 != null)
            {
                string secondaryTooltip = component2.GetSecondaryTooltip();
                if (!string.IsNullOrEmpty(secondaryTooltip))
                {
                    text = Language.main.GetFormat("DecomposingFormat", secondaryTooltip, text);
                }
            }
            WriteTitle(sb, text);
            bool flag = true;
            EnergyMixin component3 = obj.GetComponent<EnergyMixin>();
            if (component3 != null)
            {
                GameObject battery = component3.GetBattery();
                IBattery battery2 = ((battery != null) ? battery.GetComponent<IBattery>() : null);
                if (battery2 != null)
                {
                    WriteDescription(sb, battery2.GetChargeValueText());
                }
                else
                {
                    WriteDescription(sb, stringBatteryNotInserted);
                }
            }
            IBattery component4 = obj.GetComponent<IBattery>();
            if (component4 != null)
            {
                WriteDescription(sb, component4.GetChargeValueText());
                flag = false;
            }
            if (component2 != null && GameModeUtils.RequiresSurvival())
            {
                if (component2.GetFoodValue() != 0f)
                {
                    WriteDescription(sb, Language.main.GetFormat("FoodFormat", component2.GetFoodValue()));
                }
                if (component2.GetWaterValue() != 0f)
                {
                    WriteDescription(sb, Language.main.GetFormat("WaterFormat", component2.GetWaterValue()));
                }
            }
            Oxygen component5 = obj.GetComponent<Oxygen>();
            if ((bool)component5)
            {
                WriteDescription(sb, component5.GetSecondaryTooltip());
            }
            if (flag)
            {
                WriteDescription(sb, Language.main.Get(techTypeTooltipStrings.Get(techType)));
            }
            Signal component6 = obj.GetComponent<Signal>();
            if (component6 != null)
            {
                WriteDescription(sb, Language.main.Get(component6.targetDescription));
            }
            FireExtinguisher component7 = obj.GetComponent<FireExtinguisher>();
            if (component7 != null)
            {
                WriteDescription(sb, component7.GetFuelValueText());
            }
        }

        private static void ItemActions(StringBuilder sb, InventoryItem item)
        {
            bool flag = Inventory.main.GetCanBindItem(item) && GameInput.IsKeyboardAvailable();
            ItemAction useItemAction = Inventory.main.GetUseItemAction(item);
            ItemAction altUseItemAction = Inventory.main.GetAltUseItemAction(item);
            if (flag || useItemAction != 0 || altUseItemAction != 0)
            {
                sb.Append("\n");
                if (flag && GameInput.GetPrimaryDevice() != GameInput.Device.Controller)
                {
                    WriteAction(sb, stringKeyRange15, stringBindQuickSlot);
                }
                if (useItemAction != 0)
                {
                    WriteAction(sb, stringLeftHand, GetUseActionString(useItemAction));
                }
                if (altUseItemAction != 0)
                {
                    WriteAction(sb, stringRightHand, GetUseActionString(altUseItemAction));
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidUnnecessarySpecializationRule")]
        private static void WriteIngredients(ITechData data, List<TooltipIcon> icons)
        {
            int ingredientCount = data.ingredientCount;
            Inventory main = Inventory.main;
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < ingredientCount; i++)
            {
                stringBuilder.Length = 0;
                IIngredient ingredient = data.GetIngredient(i);
                TechType techType = ingredient.techType;
                int pickupCount = main.GetPickupCount(techType);
                int amount = ingredient.amount;
                bool num = pickupCount >= amount || !GameModeUtils.RequiresIngredients();
                Atlas.Sprite sprite = SpriteManager.Get(techType);
                if (num)
                {
                    stringBuilder.Append("<color=#94DE00FF>");
                }
                else
                {
                    stringBuilder.Append("<color=#DF4026FF>");
                }
                string orFallback = Language.main.GetOrFallback(techTypeIngredientStrings.Get(techType), techType);
                stringBuilder.Append(orFallback);
                if (amount > 1)
                {
                    stringBuilder.Append(" x");
                    stringBuilder.Append(amount);
                }
                if (pickupCount > 0 && pickupCount < amount)
                {
                    stringBuilder.Append(" (");
                    stringBuilder.Append(pickupCount);
                    stringBuilder.Append(")");
                }
                stringBuilder.Append("</color>");
                icons.Add(new TooltipIcon(sprite, stringBuilder.ToString()));
            }
        }

        private static void WriteTitle(StringBuilder sb, string title)
        {
            sb.AppendFormat("<size=25><color=#ffffffff>{0}</color></size>", title);
        }

        private static void WriteDescription(StringBuilder sb, string description)
        {
            sb.AppendFormat("\n<size=20><color=#DDDEDEFF>{0}</color></size>", description);
        }

        private static void WriteAction(StringBuilder sb, string key, string action)
        {
            sb.AppendFormat("\n<size=20><color=#ffffffff>{0}</color> - <color=#00ffffff>{1}</color></size>", key, action);
        }

        private static string GetUseActionString(ItemAction action)
        {
            return action switch
            {
                ItemAction.Use => stringUse, 
                ItemAction.Eat => stringEat, 
                ItemAction.Equip => stringEquip, 
                ItemAction.Unequip => stringUnequip, 
                ItemAction.Switch => stringSwitchContainer, 
                ItemAction.Swap => stringSwapItems, 
                ItemAction.Drop => stringDrop, 
                ItemAction.Assign => stringAssignQuickSlot, 
                _ => null, 
            };
        }
    }
}
