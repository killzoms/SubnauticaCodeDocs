using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_TabbedControlsPanel : MonoBehaviour
    {
        private struct Tab
        {
            public GameObject tab;

            public GameObject pane;

            public RectTransform container;

            public Selectable tabButton;

            public Selectable firstSelectable;

            public Selectable lastSelectable;
        }

        public RectTransform tabsContainer;

        public RectTransform panesContainer;

        public GameObject tabPrefab;

        public GameObject panePrefab;

        public GameObject headingPrefab;

        public GameObject toggleOptionPrefab;

        public GameObject sliderOptionPrefab;

        public GameObject dropdownOptionPrefab;

        public GameObject bindingOptionPrefab;

        public GameObject choiceOptionPrefab;

        public GameObject buttonPrefab;

        public bool tabOpen;

        private int currentTab;

        private Selectable firstWindowSelectable;

        private List<Tab> tabs = new List<Tab>();

        public virtual void Awake()
        {
            firstWindowSelectable = GetComponentInChildren<Selectable>();
        }

        protected virtual void OnEnable()
        {
            uGUI_LegendBar.ClearButtons();
            uGUI_LegendBar.ChangeButton(0, uGUI.FormatButton(GameInput.Button.UICancel, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("Back"));
            uGUI_LegendBar.ChangeButton(1, uGUI.FormatButton(GameInput.Button.UISubmit, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("ItelSelectorSelect"));
        }

        public int AddTab(string label)
        {
            Tab item = default(Tab);
            item.pane = Object.Instantiate(panePrefab);
            item.pane.transform.SetParent(panesContainer, worldPositionStays: false);
            item.tab = Object.Instantiate(tabPrefab);
            item.tab.transform.SetParent(tabsContainer, worldPositionStays: false);
            Text componentInChildren = item.tab.GetComponentInChildren<Text>();
            if (componentInChildren != null)
            {
                componentInChildren.text = Language.main.Get(label);
                item.tab.GetComponentInChildren<TranslationLiveUpdate>().translationKey = label;
            }
            int tabIndex = tabs.Count;
            ToggleButton componentInChildren2 = item.tab.GetComponentInChildren<ToggleButton>();
            UnityAction<bool> call = delegate(bool value)
            {
                if (value)
                {
                    SetVisibleTab(tabIndex);
                }
            };
            componentInChildren2.onValueChanged.AddListener(call);
            UnityAction call2 = delegate
            {
                SelectTab(tabIndex);
            };
            componentInChildren2.onButtonPressed.AddListener(call2);
            bool flag = tabIndex == 0;
            item.pane.SetActive(flag);
            componentInChildren2.isOn = flag;
            componentInChildren2.group = tabsContainer.GetComponentInChildren<ToggleGroup>();
            GameObject gameObject = Utils.FindChild(item.pane, "Content");
            if (gameObject == null)
            {
                gameObject = item.pane;
            }
            Selectable selectable = ((tabs.Count > 0) ? tabs[tabs.Count - 1].tabButton : null);
            Navigation navigation = componentInChildren2.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = selectable;
            firstWindowSelectable.gameObject.SetActive(!PlatformUtils.isConsolePlatform);
            if (!PlatformUtils.isConsolePlatform)
            {
                navigation.selectOnDown = firstWindowSelectable;
            }
            componentInChildren2.navigation = navigation;
            if (selectable != null)
            {
                navigation = selectable.navigation;
                navigation.selectOnDown = componentInChildren2;
                selectable.navigation = navigation;
            }
            item.tabButton = componentInChildren2;
            item.container = gameObject.GetComponent<RectTransform>();
            tabs.Add(item);
            return tabIndex;
        }

        public void AddHeading(int tabIndex, string label)
        {
            AddItem(tabIndex, headingPrefab, label);
        }

        public void AddButton(int tabIndex, string label, UnityAction callback = null)
        {
            Button componentInChildren = AddItem(tabIndex, buttonPrefab, label).GetComponentInChildren<Button>();
            if (callback != null)
            {
                componentInChildren.onClick.AddListener(callback);
            }
        }

        public Toggle AddToggleOption(int tabIndex, string label, bool value, UnityAction<bool> callback = null)
        {
            Toggle componentInChildren = AddItem(tabIndex, toggleOptionPrefab, label).GetComponentInChildren<Toggle>();
            componentInChildren.isOn = value;
            if (callback != null)
            {
                componentInChildren.onValueChanged.AddListener(callback);
            }
            return componentInChildren;
        }

        public void AddSliderOption(int tabIndex, string label, float value, float minValue, float maxValue, float defaultValue, UnityAction<float> callback = null)
        {
            GameObject gameObject = AddItem(tabIndex, sliderOptionPrefab, label);
            uGUI_SnappingSlider slider = gameObject.GetComponentInChildren<uGUI_SnappingSlider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = value;
            slider.defaultValue = defaultValue;
            if (callback != null)
            {
                UnityAction<float> call = delegate
                {
                    callback(slider.value);
                };
                slider.onValueChanged.AddListener(call);
            }
        }

        public void AddSliderOption(int tabIndex, string label, float value, float defaultValue, UnityAction<float> callback = null)
        {
            AddSliderOption(tabIndex, label, value, 0f, 1f, defaultValue, callback);
        }

        private string[] GetEnumNames<T>(T[] values)
        {
            string[] array = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                array[i] = values[i].ToString();
            }
            return array;
        }

        public uGUI_Choice AddChoiceOption<T>(int tabIndex, string label, T[] items, T currentValue, UnityAction<T> callback = null)
        {
            uGUI_Choice componentInChildren = AddItem(tabIndex, choiceOptionPrefab, label).GetComponentInChildren<uGUI_Choice>();
            componentInChildren.SetOptions(GetEnumNames(items));
            componentInChildren.value = items.IndexOf(currentValue);
            if (callback != null)
            {
                UnityAction<int> call = delegate(int index)
                {
                    callback(items[index]);
                };
                componentInChildren.onValueChanged.AddListener(call);
            }
            return componentInChildren;
        }

        public uGUI_Choice AddChoiceOption(int tabIndex, string label, string[] items, int currentIndex, UnityAction<int> callback = null)
        {
            uGUI_Choice componentInChildren = AddItem(tabIndex, choiceOptionPrefab, label).GetComponentInChildren<uGUI_Choice>();
            componentInChildren.SetOptions(items);
            componentInChildren.value = currentIndex;
            if (callback != null)
            {
                componentInChildren.onValueChanged.AddListener(callback);
            }
            return componentInChildren;
        }

        public void AddDropdownOption(int tabIndex, string label, string[] items, int currentIndex, UnityAction<int> callback = null)
        {
            Dropdown componentInChildren = AddItem(tabIndex, dropdownOptionPrefab, label).GetComponentInChildren<Dropdown>();
            componentInChildren.options.Clear();
            foreach (string text in items)
            {
                Dropdown.OptionData optionData = new Dropdown.OptionData();
                optionData.text = text;
                componentInChildren.options.Add(optionData);
            }
            componentInChildren.value = currentIndex;
            if (callback != null)
            {
                componentInChildren.onValueChanged.AddListener(callback);
            }
        }

        public uGUI_Bindings AddBindingOption(int tabIndex, string label, GameInput.Device device, GameInput.Button button)
        {
            GameObject bindingObject;
            return AddBindingOption(tabIndex, label, device, button, out bindingObject);
        }

        public uGUI_Bindings AddBindingOption(int tabIndex, string label, GameInput.Device device, GameInput.Button button, out GameObject bindingObject)
        {
            bindingObject = AddItem(tabIndex, bindingOptionPrefab, label);
            uGUI_Bindings componentInChildren = bindingObject.GetComponentInChildren<uGUI_Bindings>();
            componentInChildren.Initialize(device, button);
            return componentInChildren;
        }

        public GameObject AddItem(int tabIndex, GameObject optionPrefab)
        {
            GameObject gameObject = Object.Instantiate(optionPrefab);
            gameObject.transform.SetParent(tabs[tabIndex].container, worldPositionStays: false);
            Tab value = tabs[tabIndex];
            Selectable[] componentsInChildren = gameObject.GetComponentsInChildren<Selectable>();
            Selectable[] array = componentsInChildren;
            foreach (Selectable selectable in array)
            {
                Navigation navigation = selectable.navigation;
                if (navigation.mode == Navigation.Mode.Explicit)
                {
                    navigation.selectOnUp = value.lastSelectable;
                    selectable.navigation = navigation;
                }
            }
            if (componentsInChildren.Length != 0)
            {
                Selectable selectable2 = componentsInChildren[0];
                if (value.firstSelectable == null)
                {
                    value.firstSelectable = selectable2;
                }
                if (value.lastSelectable == null)
                {
                    Navigation navigation2 = value.tabButton.navigation;
                    if (navigation2.mode == Navigation.Mode.Explicit)
                    {
                        value.tabButton.navigation = navigation2;
                    }
                }
                else
                {
                    Navigation navigation3 = value.lastSelectable.navigation;
                    if (navigation3.mode == Navigation.Mode.Explicit)
                    {
                        navigation3.selectOnDown = selectable2;
                        value.lastSelectable.navigation = navigation3;
                    }
                }
                value.lastSelectable = selectable2;
                tabs[tabIndex] = value;
            }
            return gameObject;
        }

        private GameObject AddItem(int tabIndex, GameObject optionPrefab, string label)
        {
            GameObject gameObject = AddItem(tabIndex, optionPrefab);
            Text componentInChildren = gameObject.GetComponentInChildren<Text>();
            if (componentInChildren != null)
            {
                gameObject.GetComponentInChildren<TranslationLiveUpdate>().translationKey = label;
                componentInChildren.text = Language.main.Get(label);
            }
            return gameObject;
        }

        private void SetVisibleTab(int tabIndex)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].pane.SetActive(tabIndex == i);
            }
            if (tabIndex >= 0 && tabIndex < tabs.Count && currentTab != tabIndex)
            {
                currentTab = tabIndex;
                UIUtils.ScrollToShowItemInCenter(tabs[tabIndex].firstSelectable.transform);
            }
        }

        public void RemoveTabs()
        {
            foreach (Tab tab in tabs)
            {
                Object.Destroy(tab.tab);
                Object.Destroy(tab.pane);
            }
            tabs.Clear();
        }

        private void SelectTab(int tabIndex)
        {
            if (!tabOpen)
            {
                SetVisibleTab(tabIndex);
                GamepadInputModule.current.SelectItem(tabs[tabIndex].firstSelectable);
                tabOpen = true;
            }
        }

        public void HighlightCurrentTab()
        {
            uGUI_LegendBar.ChangeButton(0, uGUI.FormatButton(GameInput.Button.UICancel, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("Back"));
            uGUI_LegendBar.ChangeButton(1, uGUI.FormatButton(GameInput.Button.UISubmit, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("ItelSelectorSelect"));
            StartCoroutine(_InternalHighlightCurrentTab());
        }

        private IEnumerator _InternalHighlightCurrentTab()
        {
            yield return new WaitForEndOfFrame();
            GamepadInputModule.current.SelectItem(tabs[currentTab].tabButton);
            tabOpen = false;
        }
    }
}
