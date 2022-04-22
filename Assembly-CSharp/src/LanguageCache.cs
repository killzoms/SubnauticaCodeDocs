using System.Collections.Generic;

namespace AssemblyCSharp
{
    public class LanguageCache
    {
        public class ButtonText
        {
            public GameInput.Button button;

            public string bindingName;

            public string cachedUIText;
        }

        private static readonly Dictionary<string, ButtonText> buttonTextCache = new Dictionary<string, ButtonText>();

        private static readonly Dictionary<int, string> oxygenCache = new Dictionary<int, string>();

        private static readonly Dictionary<TechType, string> pickupCache = new Dictionary<TechType, string>();

        private static readonly Dictionary<TechType, string> packupCache = new Dictionary<TechType, string>();

        public static void OnLanguageChanged()
        {
            buttonTextCache.Clear();
            oxygenCache.Clear();
            pickupCache.Clear();
            packupCache.Clear();
        }

        public static string GetButtonFormat(string key, GameInput.Button button)
        {
            string bindingName = GameInput.GetBindingName(button, GameInput.BindingSet.Primary);
            ButtonText orAddNew = buttonTextCache.GetOrAddNew(key);
            if (orAddNew.button != button || orAddNew.bindingName != bindingName || string.IsNullOrEmpty(orAddNew.cachedUIText))
            {
                orAddNew.button = button;
                orAddNew.bindingName = bindingName;
                orAddNew.cachedUIText = Language.main.GetFormat(key, uGUI.FormatButton(button));
            }
            return orAddNew.cachedUIText;
        }

        public static string GetOxygenText(int secondsLeft)
        {
            if (!oxygenCache.TryGetValue(secondsLeft, out var value))
            {
                value = ((secondsLeft > 0) ? Language.main.GetFormat("OxygenFormat", secondsLeft) : Language.main.Get("Empty"));
                oxygenCache.Add(secondsLeft, value);
            }
            return value;
        }

        public static string GetPickupText(TechType techType)
        {
            if (!pickupCache.TryGetValue(techType, out var value))
            {
                value = Language.main.GetFormat("PickUpFormat", Language.main.Get(techType));
                pickupCache.Add(techType, value);
            }
            return value;
        }

        public static string GetPackUpText(TechType techType)
        {
            if (!packupCache.TryGetValue(techType, out var value))
            {
                value = Language.main.GetFormat("PackUpFormat", Language.main.Get(techType));
                packupCache.Add(techType, value);
            }
            return value;
        }
    }
}
