using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_PDA : uGUI_InputGroup, uGUI_IToolbarManager, uGUI_IButtonReceiver
    {
        public class PDATabComparer : IEqualityComparer<PDATab>
        {
            public bool Equals(PDATab x, PDATab y)
            {
                int num = (int)x;
                return num.Equals((int)y);
            }

            public int GetHashCode(PDATab tab)
            {
                return (int)tab;
            }
        }

        private static PDATabComparer sPDATabComparer = new PDATabComparer();

        private static readonly List<PDATab> regularTabs = new List<PDATab>
        {
            PDATab.Inventory,
            PDATab.Journal,
            PDATab.Ping,
            PDATab.Gallery,
            PDATab.Log,
            PDATab.Encyclopedia
        };

        [AssertNotNull]
        public CanvasGroup canvasGroup;

        [AssertNotNull]
        public uGUI_Toolbar toolbar;

        [AssertNotNull]
        public CanvasGroup toolbarCanvasGroup;

        [AssertNotNull]
        public Image pdaBackground;

        [AssertNotNull]
        public uGUI_PDATab tabIntro;

        [AssertNotNull]
        public uGUI_PDATab tabInventory;

        [AssertNotNull]
        public uGUI_PDATab tabJournal;

        [AssertNotNull]
        public uGUI_PDATab tabPing;

        [AssertNotNull]
        public uGUI_PDATab tabGallery;

        [AssertNotNull]
        public uGUI_PDATab tabLog;

        [AssertNotNull]
        public uGUI_PDATab tabEncyclopedia;

        [AssertNotNull]
        public uGUI_PDATab tabTimeCapsule;

        [AssertNotNull]
        public FMODAsset soundOpen;

        [AssertNotNull]
        public FMODAsset soundClose;

        [AssertNotNull]
        public Button backButton;

        [AssertNotNull]
        public Text backButtonText;

        public SoundQueue soundQueue = new SoundQueue();

        private bool initialized;

        private Dictionary<PDATab, uGUI_PDATab> tabs;

        private PDATab tabPrev = regularTabs[0];

        private PDATab tabOpen = PDATab.None;

        private Coroutine revealBackgroundRoutine;

        private Coroutine revealContentRoutine;

        private List<PDATab> currentTabs = new List<PDATab>();

        private List<string> toolbarTooltips = new List<string>();

        private BaseRaycaster quickSlotsParentRaycaster;

        public static uGUI_PDA main { get; private set; }

        public bool introActive => tabOpen == PDATab.Intro;

        public uGUI_PDATab currentTab => GetTab(tabOpen);

        public PDATab currentTabType => tabOpen;

        protected override void Awake()
        {
            if (main != null)
            {
                Debug.LogError("uGUI_PDA : Awake() : Multiple instances of uGUI_PDA found in scene!");
                Object.Destroy(base.gameObject);
                return;
            }
            main = this;
            base.Awake();
            quickSlotsParentRaycaster = uGUI.main.quickSlots.GetComponentInParent<BaseRaycaster>();
            DevConsole.RegisterConsoleCommand(this, "pdaintro");
        }

        private void Start()
        {
            Language.main.OnLanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
        }

        protected override void Update()
        {
            soundQueue.Update();
            if (!base.selected && Player.main.GetPDA().isOpen && AvatarInputHandler.main.IsEnabled())
            {
                Select();
            }
            FPSInputModule.current.EscapeMenu();
        }

        private void LateUpdate()
        {
            if (base.isActiveAndEnabled)
            {
                for (int i = 0; i < currentTabs.Count; i++)
                {
                    PDATab key = currentTabs[i];
                    uGUI_PDATab uGUI_PDATab2 = tabs[key];
                    toolbar.SetNotificationsAmount(i, uGUI_PDATab2.notificationsCount);
                }
            }
        }

        private void OnDestroy()
        {
            Language language = Language.main;
            if (language != null)
            {
                language.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        private void OnLanguageChanged()
        {
            CacheToolbarTooltips();
            Dictionary<PDATab, uGUI_PDATab>.Enumerator enumerator = tabs.GetEnumerator();
            while (enumerator.MoveNext())
            {
                enumerator.Current.Value.OnLanguageChanged();
            }
        }

        private void CacheToolbarTooltips()
        {
            toolbarTooltips.Clear();
            for (int i = 0; i < currentTabs.Count; i++)
            {
                PDATab pDATab = currentTabs[i];
                toolbarTooltips.Add(TooltipFactory.Label($"Tab{pDATab.ToString()}"));
            }
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            tabs = new Dictionary<PDATab, uGUI_PDATab>(sPDATabComparer)
            {
                {
                    PDATab.Intro,
                    tabIntro
                },
                {
                    PDATab.Inventory,
                    tabInventory
                },
                {
                    PDATab.Journal,
                    tabJournal
                },
                {
                    PDATab.Ping,
                    tabPing
                },
                {
                    PDATab.Gallery,
                    tabGallery
                },
                {
                    PDATab.Log,
                    tabLog
                },
                {
                    PDATab.Encyclopedia,
                    tabEncyclopedia
                },
                {
                    PDATab.TimeCapsule,
                    tabTimeCapsule
                }
            };
            foreach (KeyValuePair<PDATab, uGUI_PDATab> tab in tabs)
            {
                uGUI_PDATab value = tab.Value;
                value.Register(this);
                value.Close();
            }
            backButton.gameObject.SetActive(value: false);
            SetTabs(regularTabs);
        }

        public void SetTabs(List<PDATab> tabs)
        {
            int num = tabs?.Count ?? 0;
            Atlas.Sprite[] array = new Atlas.Sprite[num];
            currentTabs.Clear();
            for (int i = 0; i < num; i++)
            {
                PDATab item = tabs[i];
                array[i] = SpriteManager.Get(SpriteManager.Group.Tab, $"Tab{item.ToString()}");
                currentTabs.Add(item);
            }
            uGUI_Toolbar obj = toolbar;
            object[] content = array;
            obj.Initialize(this, content);
            CacheToolbarTooltips();
        }

        public void OnOpenPDA(PDATab tabID = PDATab.None)
        {
            if (!introActive)
            {
                soundQueue.PlayImmediately(soundOpen);
            }
            Dictionary<PDATab, uGUI_PDATab>.Enumerator enumerator = tabs.GetEnumerator();
            while (enumerator.MoveNext())
            {
                enumerator.Current.Value.OnOpenPDA();
            }
            if (tabID == PDATab.None && tabOpen == PDATab.None)
            {
                tabID = tabPrev;
            }
            OpenTab(tabID);
        }

        public void OnClosePDA()
        {
            soundQueue.PlayImmediately(soundClose);
            if (tabOpen != PDATab.None)
            {
                tabs[tabOpen].Close();
                tabOpen = PDATab.None;
            }
            Dictionary<PDATab, uGUI_PDATab>.Enumerator enumerator = tabs.GetEnumerator();
            while (enumerator.MoveNext())
            {
                enumerator.Current.Value.OnClosePDA();
            }
            Deselect();
            SetTabs(regularTabs);
        }

        public void OpenTab(PDATab tabId)
        {
            if (tabId != tabOpen && tabId >= PDATab.Intro && tabs.TryGetValue(tabId, out var value))
            {
                if (tabOpen != PDATab.None)
                {
                    tabs[tabOpen].Close();
                }
                if (backButton.gameObject.activeSelf)
                {
                    backButton.onClick.RemoveAllListeners();
                    backButton.gameObject.SetActive(value: false);
                }
                tabOpen = tabId;
                value.Open();
                if (regularTabs.Contains(tabId))
                {
                    tabPrev = tabId;
                }
                int num = currentTabs.IndexOf(tabId);
                if (num != -1)
                {
                    toolbarCanvasGroup.alpha = 1f;
                    toolbarCanvasGroup.interactable = true;
                    toolbarCanvasGroup.blocksRaycasts = true;
                    toolbar.Select(num);
                }
                else
                {
                    toolbarCanvasGroup.alpha = 0f;
                    toolbarCanvasGroup.interactable = false;
                    toolbarCanvasGroup.blocksRaycasts = false;
                }
                GamepadInputModule.current.SetCurrentGrid(currentTab.GetInitialGrid());
            }
        }

        public uGUI_PDATab GetTab(PDATab tabId)
        {
            if (tabId == PDATab.None)
            {
                return null;
            }
            if (tabs.TryGetValue(tabId, out var value))
            {
                return value;
            }
            return null;
        }

        public PDATab GetNextTab()
        {
            int num = currentTabs.IndexOf(tabOpen);
            int count = currentTabs.Count;
            int num2 = ((num >= 0) ? (num + 1) : 0);
            if (num2 >= count)
            {
                num2 = 0;
            }
            for (int num3 = num2; num3 != num; num3 = ((num3 + 1 < count) ? (num3 + 1) : 0))
            {
                PDATab pDATab = currentTabs[num3];
                if (tabs.ContainsKey(pDATab))
                {
                    return pDATab;
                }
            }
            return PDATab.None;
        }

        public PDATab GetPreviousTab()
        {
            int num = currentTabs.IndexOf(tabOpen);
            int count = currentTabs.Count;
            int num2 = ((num < 0) ? (count - 1) : (num - 1));
            if (num2 < 0)
            {
                num2 = count - 1;
            }
            for (int num3 = num2; num3 != num; num3 = ((num3 - 1 < 0) ? (count - 1) : (num3 - 1)))
            {
                PDATab pDATab = currentTabs[num3];
                if (tabs.ContainsKey(pDATab))
                {
                    return pDATab;
                }
            }
            return PDATab.None;
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
            if (button == 0 && index >= 0 && index < currentTabs.Count)
            {
                OpenTab(currentTabs[index]);
            }
        }

        private void OnConsoleCommand_pdaintro(NotificationCenter.Notification n)
        {
            PlayIntro();
        }

        public void PlayIntro()
        {
            OpenTab(PDATab.Intro);
        }

        public void SetBackgroundAlpha(float alpha)
        {
            Color color = pdaBackground.color;
            color.a = Mathf.Clamp01(alpha);
            pdaBackground.color = color;
        }

        public void RevealBackground()
        {
            if (revealBackgroundRoutine == null)
            {
                revealBackgroundRoutine = StartCoroutine(RevealBackgroundRoutine());
            }
        }

        public void RevealContent()
        {
            if (revealContentRoutine == null)
            {
                revealContentRoutine = StartCoroutine(RevealContentRoutine());
            }
        }

        private IEnumerator RevealBackgroundRoutine()
        {
            SetBackgroundAlpha(0f);
            for (float alpha = 0f; alpha < 1f; alpha += Time.deltaTime / 0.333333f)
            {
                SetBackgroundAlpha(alpha);
                yield return null;
            }
            SetBackgroundAlpha(1f);
            revealBackgroundRoutine = null;
        }

        private IEnumerator RevealContentRoutine()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            for (float alpha = 0f; alpha < 1f; alpha += Time.deltaTime * 5f)
            {
                canvasGroup.alpha = Mathf.Clamp01(alpha);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            revealContentRoutine = null;
        }

        public override void OnSelect(bool lockMovement)
        {
            base.OnSelect(lockMovement);
            uGUI_INavigableIconGrid currentGrid = null;
            if (currentTab != null)
            {
                currentGrid = currentTab.GetInitialGrid();
            }
            GamepadInputModule.current.SetCurrentGrid(currentGrid);
            if (introActive)
            {
                uGUI_Tooltip.Clear();
            }
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            GamepadInputModule.current.SetCurrentGrid(null);
        }

        public override bool Raycast(PointerEventData eventData, List<RaycastResult> raycastResults)
        {
            bool num = base.Raycast(eventData, raycastResults);
            if (num && quickSlotsParentRaycaster != null)
            {
                quickSlotsParentRaycaster.Raycast(eventData, raycastResults);
            }
            return num;
        }

        public bool OnButtonDown(GameInput.Button button)
        {
            uGUI_PDATab uGUI_PDATab2 = currentTab;
            if (uGUI_PDATab2 != null)
            {
                return uGUI_PDATab2.OnButtonDown(button);
            }
            return false;
        }
    }
}
