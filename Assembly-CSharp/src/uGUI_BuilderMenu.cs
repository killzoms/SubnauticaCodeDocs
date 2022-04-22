using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_BuilderMenu : uGUI_InputGroup, uGUI_IIconGridManager, uGUI_IToolbarManager, uGUI_IButtonReceiver, INotificationListener
    {
        private const string prefabPath = "uGUI_BuilderMenu";

        private static readonly List<TechGroup> groups = new List<TechGroup>
        {
            TechGroup.BasePieces,
            TechGroup.ExteriorModules,
            TechGroup.InteriorPieces,
            TechGroup.InteriorModules,
            TechGroup.Miscellaneous
        };

        private const NotificationManager.Group notificationGroup = NotificationManager.Group.Builder;

        private static uGUI_BuilderMenu singleton;

        private static readonly List<TechType>[] groupsTechTypes = new List<TechType>[groups.Count];

        private static Dictionary<TechType, int> techTypeToTechGroupIdx = new Dictionary<TechType, int>();

        private static bool groupsTechTypesInitialized = false;

        [AssertNotNull]
        public uGUI_CanvasScaler canvasScaler;

        [AssertNotNull]
        public Text title;

        [AssertNotNull]
        public uGUI_Toolbar toolbar;

        [AssertNotNull]
        public uGUI_IconGrid iconGrid;

        [AssertNotNull]
        public GameObject content;

        [Range(1f, 256f)]
        public float iconSize = 64f;

        [Range(0f, 64f)]
        public float minSpaceX = 20f;

        [Range(0f, 64f)]
        public float minSpaceY = 20f;

        private Dictionary<string, TechType> items = new Dictionary<string, TechType>();

        private int openInFrame = -1;

        private new int selected;

        private CachedEnumString<TechGroup> techGroupNames = new CachedEnumString<TechGroup>(CraftData.sTechGroupComparer);

        private List<string> toolbarTooltips = new List<string>();

        private int[] groupNotificationCounts = new int[groups.Count];

        public bool state { get; private set; }

        public int TabOpen => selected;

        public int TabCount => groups.Count;

        void INotificationListener.OnAdd(NotificationManager.Group group, string key)
        {
            TechType techType = key.DecodeKey();
            if (KnownTech.Contains(techType))
            {
                int techTypeTechGroupIdx = GetTechTypeTechGroupIdx(techType);
                groupNotificationCounts[techTypeTechGroupIdx]++;
            }
        }

        void INotificationListener.OnRemove(NotificationManager.Group group, string key)
        {
            TechType techType = key.DecodeKey();
            if (KnownTech.Contains(techType))
            {
                int techTypeTechGroupIdx = GetTechTypeTechGroupIdx(techType);
                groupNotificationCounts[techTypeTechGroupIdx]--;
            }
        }

        public static bool IsOpen()
        {
            if (singleton != null)
            {
                return singleton.state;
            }
            return false;
        }

        protected override void Awake()
        {
            if (singleton != null)
            {
                Debug.LogError("Multiple uGUI_BuilderMenu instances found in scene!", this);
                global::UnityEngine.Object.Destroy(base.gameObject);
                return;
            }
            singleton = this;
            base.Awake();
            EnsureTechGroupTechTypeDataInitialized();
            ClearNotificationCounts();
            iconGrid.iconSize = new Vector2(iconSize, iconSize);
            iconGrid.minSpaceX = minSpaceX;
            iconGrid.minSpaceY = minSpaceY;
            iconGrid.Initialize(this);
            int count = groups.Count;
            Atlas.Sprite[] array = new Atlas.Sprite[count];
            for (int i = 0; i < count; i++)
            {
                TechGroup value = groups[i];
                string text = techGroupNames.Get(value);
                array[i] = SpriteManager.Get(SpriteManager.Group.Tab, "group" + text);
            }
            uGUI_Toolbar obj = toolbar;
            object[] array2 = array;
            obj.Initialize(this, array2);
            toolbar.Select(selected);
            UpdateItems();
            KnownTech.onChanged += OnChanged;
            PDAScanner.onAdd = (PDAScanner.OnEntryEvent)Delegate.Combine(PDAScanner.onAdd, new PDAScanner.OnEntryEvent(OnLockedAdd));
            PDAScanner.onRemove = (PDAScanner.OnEntryEvent)Delegate.Combine(PDAScanner.onRemove, new PDAScanner.OnEntryEvent(OnLockedRemove));
            NotificationManager.main.Subscribe(this, NotificationManager.Group.Builder, string.Empty);
        }

        private List<TechType> GetTechTypesForGroup(int groupIdx)
        {
            return groupsTechTypes[groupIdx];
        }

        private void Start()
        {
            Language.main.OnLanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
        }

        public bool GetIsLeftMouseBoundToRightHand()
        {
            if ("MouseButtonLeft" == GameInput.GetBinding(GameInput.Device.Keyboard, GameInput.Button.RightHand, GameInput.BindingSet.Primary) || "MouseButtonLeft" == GameInput.GetBinding(GameInput.Device.Keyboard, GameInput.Button.RightHand, GameInput.BindingSet.Secondary))
            {
                return true;
            }
            return false;
        }

        protected override void Update()
        {
            base.Update();
            if (state && openInFrame != Time.frameCount)
            {
                bool flag = GameInput.GetButtonDown(GameInput.Button.RightHand);
                if (GetIsLeftMouseBoundToRightHand())
                {
                    flag = false;
                }
                if (GameInput.GetButtonDown(GameInput.Button.UICancel) || flag || !base.focused)
                {
                    Close();
                }
            }
        }

        private void LateUpdate()
        {
            if (state)
            {
                UpdateToolbarNotificationNumbers();
            }
        }

        private void OnDestroy()
        {
            NotificationManager.main.Unsubscribe(this);
            KnownTech.onChanged -= OnChanged;
            PDAScanner.onAdd = (PDAScanner.OnEntryEvent)Delegate.Remove(PDAScanner.onAdd, new PDAScanner.OnEntryEvent(OnLockedAdd));
            PDAScanner.onRemove = (PDAScanner.OnEntryEvent)Delegate.Remove(PDAScanner.onRemove, new PDAScanner.OnEntryEvent(OnLockedRemove));
            Language main = Language.main;
            if ((bool)main)
            {
                main.OnLanguageChanged -= OnLanguageChanged;
            }
            singleton = null;
        }

        public override void OnSelect(bool lockMovement)
        {
            base.OnSelect(lockMovement);
            GamepadInputModule.current.SetCurrentGrid(iconGrid);
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            Close();
        }

        public static void Show()
        {
            uGUI_BuilderMenu instance = GetInstance();
            if (instance != null)
            {
                instance.Open();
            }
        }

        public static void Hide()
        {
            uGUI_BuilderMenu instance = GetInstance();
            if (instance != null)
            {
                instance.Close();
            }
        }

        public static void EnsureCreated()
        {
            GetInstance();
        }

        public void Open()
        {
            if (!state)
            {
                UpdateToolbarNotificationNumbers();
                MainCameraControl.main.SaveLockedVRViewModelAngle();
                SetState(newState: true);
                openInFrame = Time.frameCount;
            }
        }

        public void Close()
        {
            if (state)
            {
                SetState(newState: false);
            }
        }

        private void OnChanged(HashSet<TechType> techList)
        {
            UpdateItems();
        }

        private void OnLockedAdd(PDAScanner.Entry entry)
        {
            UpdateItems();
        }

        private void OnLockedRemove(PDAScanner.Entry entry)
        {
            UpdateItems();
        }

        public void GetToolbarTooltip(int index, out string tooltipText, List<TooltipIcon> tooltipIcons)
        {
            tooltipText = null;
            if (index >= 0 && index < toolbarTooltips.Count)
            {
                tooltipText = toolbarTooltips[index];
            }
        }

        public void OnToolbarClick(int index, int button)
        {
            if (button == 0)
            {
                SetCurrentTab(index);
            }
        }

        public void GetTooltip(string id, out string tooltipText, List<TooltipIcon> tooltipIcons)
        {
            if (items.TryGetValue(id, out var value))
            {
                bool locked = !CrafterLogic.IsCraftRecipeUnlocked(value);
                TooltipFactory.BuildTech(value, locked, out tooltipText, tooltipIcons);
            }
            else
            {
                tooltipText = null;
            }
        }

        public void OnPointerEnter(string id)
        {
        }

        public void OnPointerExit(string id)
        {
        }

        public void OnPointerClick(string id, int button)
        {
            if (button == 0 && items.TryGetValue(id, out var value) && KnownTech.Contains(value))
            {
                GameObject buildPrefab = CraftData.GetBuildPrefab(value);
                SetState(newState: false);
                Builder.Begin(buildPrefab);
            }
        }

        public void OnSortRequested()
        {
        }

        private void UpdateToolbarNotificationNumbers()
        {
            for (int i = 0; i < groups.Count; i++)
            {
                toolbar.SetNotificationsAmount(i, groupNotificationCounts[i]);
            }
        }

        private static void EnsureTechGroupTechTypeDataInitialized()
        {
            if (groupsTechTypesInitialized)
            {
                return;
            }
            for (int i = 0; i < groups.Count; i++)
            {
                groupsTechTypes[i] = new List<TechType>();
                List<TechType> list = groupsTechTypes[i];
                CraftData.GetBuilderGroupTech(groups[i], list);
                for (int j = 0; j < list.Count; j++)
                {
                    TechType key = list[j];
                    techTypeToTechGroupIdx.Add(key, i);
                }
            }
            groupsTechTypesInitialized = true;
        }

        private void ClearNotificationCounts()
        {
            _ = NotificationManager.main;
            for (int i = 0; i < groups.Count; i++)
            {
                groupNotificationCounts[i] = 0;
            }
        }

        private int GetTechTypeTechGroupIdx(TechType inTechType)
        {
            if (techTypeToTechGroupIdx.TryGetValue(inTechType, out var value))
            {
                return value;
            }
            throw new ArgumentException("TechType not associated with any of the tech groups.");
        }

        private void CacheToolbarTooltips()
        {
            toolbarTooltips.Clear();
            for (int i = 0; i < groups.Count; i++)
            {
                TechGroup value = groups[i];
                toolbarTooltips.Add(TooltipFactory.Label($"Group{techGroupNames.Get(value)}"));
            }
        }

        private void OnLanguageChanged()
        {
            title.text = Language.main.Get("CraftingLabel");
            CacheToolbarTooltips();
        }

        private static uGUI_BuilderMenu GetInstance()
        {
            if (singleton == null)
            {
                GameObject gameObject = Resources.Load<GameObject>("uGUI_BuilderMenu");
                if (gameObject == null)
                {
                    Debug.LogError("Cannot find main uGUI_BuilderMenu prefab in Resources folder at path 'uGUI_BuilderMenu'");
                    Debug.Break();
                    return null;
                }
                global::UnityEngine.Object.Instantiate(gameObject);
                singleton.state = true;
                singleton.SetState(newState: false);
            }
            return singleton;
        }

        private void SetState(bool newState)
        {
            if (state == newState)
            {
                return;
            }
            state = newState;
            if (state)
            {
                canvasScaler.SetAnchor();
                content.SetActive(value: true);
                if (!base.focused)
                {
                    Select();
                }
            }
            else
            {
                if (base.focused)
                {
                    Deselect();
                }
                content.SetActive(value: false);
            }
        }

        private void SetCurrentTab(int index)
        {
            if (index >= 0 && index < groups.Count && index != selected)
            {
                toolbar.Select(index);
                selected = index;
                UpdateItems();
                iconGrid.UpdateNow();
                GamepadInputModule.current.SetCurrentGrid(iconGrid);
            }
        }

        private void UpdateItems()
        {
            iconGrid.Clear();
            items.Clear();
            _ = groups[selected];
            List<TechType> techTypesForGroup = GetTechTypesForGroup(selected);
            int num = 0;
            for (int i = 0; i < techTypesForGroup.Count; i++)
            {
                TechType techType = techTypesForGroup[i];
                TechUnlockState techUnlockState = KnownTech.GetTechUnlockState(techType);
                if (techUnlockState == TechUnlockState.Available || techUnlockState == TechUnlockState.Locked)
                {
                    string stringForInt = IntStringCache.GetStringForInt(num);
                    items.Add(stringForInt, techType);
                    iconGrid.AddItem(stringForInt, SpriteManager.Get(techType), SpriteManager.GetBackground(techType), techUnlockState == TechUnlockState.Locked, num);
                    iconGrid.RegisterNotificationTarget(stringForInt, NotificationManager.Group.Builder, techType.EncodeKey());
                    num++;
                }
            }
        }

        public bool OnButtonDown(GameInput.Button button)
        {
            switch (button)
            {
                case GameInput.Button.UINextTab:
                {
                    int currentTab2 = (TabOpen + 1) % TabCount;
                    SetCurrentTab(currentTab2);
                    return true;
                }
                case GameInput.Button.UIPrevTab:
                {
                    int currentTab = (TabOpen - 1 + TabCount) % TabCount;
                    SetCurrentTab(currentTab);
                    return true;
                }
                case GameInput.Button.UICancel:
                    Close();
                    return true;
                default:
                    return false;
            }
        }
    }
}
