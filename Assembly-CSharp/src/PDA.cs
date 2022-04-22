using UnityEngine;

namespace AssemblyCSharp
{
    public class PDA : MonoBehaviour
    {
        public enum State
        {
            Opened,
            Closed,
            Opening,
            Closing
        }

        public delegate void OnClose(PDA pda);

        private const float animationTime = 0.5f;

        [AssertNotNull]
        public GameObject prefabScreen;

        [AssertNotNull]
        public Transform screenAnchor;

        public float cameraFieldOfView = 62f;

        public float cameraFieldOfViewAtFourThree = 66f;

        private Sequence sequence = new Sequence(initialState: false);

        private int prevQuickSlot = -1;

        private bool targetWasSet;

        private Transform target;

        private OnClose onClose;

        private float activeSqrDistance;

        private bool shouldPlayIntro;

        private bool ignorePDAInput;

        private GameObject screen;

        private uGUI_PDA _ui;

        public bool isInUse { get; private set; }

        public bool isFocused
        {
            get
            {
                if (ui != null)
                {
                    return ui.focused;
                }
                return false;
            }
        }

        public bool isOpen => state == State.Opened;

        public State state
        {
            get
            {
                if (sequence.target)
                {
                    if (!sequence.active)
                    {
                        return State.Opened;
                    }
                    return State.Opening;
                }
                if (!base.gameObject.activeInHierarchy || !sequence.active)
                {
                    return State.Closed;
                }
                return State.Closing;
            }
        }

        public uGUI_PDA ui
        {
            get
            {
                if (_ui == null)
                {
                    screen = Object.Instantiate(prefabScreen);
                    _ui = screen.GetComponent<uGUI_PDA>();
                    screen.GetComponent<uGUI_CanvasScaler>().SetAnchor(screenAnchor);
                    _ui.Initialize();
                }
                return _ui;
            }
        }

        public void SetIgnorePDAInput(bool ignore)
        {
            ignorePDAInput = ignore;
        }

        private void Update()
        {
            sequence.Update();
            if (sequence.active)
            {
                float b = ((SNCameraRoot.main.mainCamera.aspect > 1.5f) ? cameraFieldOfView : cameraFieldOfViewAtFourThree);
                SNCameraRoot.main.SetFov(Mathf.Lerp(MiscSettings.fieldOfView, b, sequence.t));
            }
            Player main = Player.main;
            if (isInUse && isFocused && GameInput.GetButtonDown(GameInput.Button.PDA) && !ui.introActive)
            {
                Close();
            }
            else if (targetWasSet && (target == null || (target.transform.position - main.transform.position).sqrMagnitude >= activeSqrDistance))
            {
                Close();
            }
        }

        public bool Open(PDATab tab = PDATab.None, Transform target = null, OnClose onCloseCallback = null, float activeDistance = -1f)
        {
            if (isInUse || ignorePDAInput)
            {
                return false;
            }
            uGUI.main.quickSlots.SetTarget(null);
            prevQuickSlot = Inventory.main.quickSlots.activeSlot;
            bool num = Inventory.main.ReturnHeld();
            Player main = Player.main;
            if (!num || main.cinematicModeActive)
            {
                return false;
            }
            MainCameraControl.main.SaveLockedVRViewModelAngle();
            isInUse = true;
            base.gameObject.SetActive(value: true);
            if (shouldPlayIntro)
            {
                shouldPlayIntro = false;
                ui.PlayIntro();
            }
            uGUI_PopupNotification main2 = uGUI_PopupNotification.main;
            if (tab == PDATab.None && main2.isShowingMessage)
            {
                tab = main2.tabId;
            }
            if (tab == PDATab.TimeCapsule)
            {
                ui.SetTabs(null);
                Inventory.main.SetUsedStorage(PlayerTimeCapsule.main.container);
                uGUI_GalleryTab obj = ui.GetTab(PDATab.Gallery) as uGUI_GalleryTab;
                uGUI_TimeCapsuleTab @object = ui.GetTab(PDATab.TimeCapsule) as uGUI_TimeCapsuleTab;
                obj.SetSelectListener(@object.SelectImage, "ScreenshotSelect", "ScreenshotSelectTooltip");
            }
            ui.OnOpenPDA(tab);
            sequence.Set(0.5f, target: true, Activated);
            GoalManager.main.OnCustomGoalEvent("Open_PDA");
            global::UWE.Utils.lockCursor = false;
            if (HandReticle.main != null)
            {
                HandReticle.main.RequestCrosshairHide();
            }
            Inventory.main.SetViewModelVis(state: false);
            screen.SetActive(value: true);
            targetWasSet = target != null;
            this.target = target;
            onClose = onCloseCallback;
            if (activeDistance < 0f)
            {
                activeDistance = 3f;
            }
            activeSqrDistance = activeDistance * activeDistance;
            UwePostProcessingManager.OpenPDA();
            return true;
        }

        public void Close()
        {
            if (isInUse && !ignorePDAInput)
            {
                Player main = Player.main;
                MainCameraControl.main.ResetLockedVRViewModelAngle();
                Vehicle vehicle = main.GetVehicle();
                if (vehicle != null)
                {
                    uGUI.main.quickSlots.SetTarget(vehicle);
                }
                targetWasSet = false;
                target = null;
                isInUse = false;
                ui.OnClosePDA();
                MainGameController.Instance.PerformGarbageCollection();
                if (HandReticle.main != null)
                {
                    HandReticle.main.UnrequestCrosshairHide();
                }
                Inventory.main.SetViewModelVis(state: true);
                sequence.Set(0.5f, target: false, Deactivated);
                screen.SetActive(value: false);
                UwePostProcessingManager.ClosePDA();
                if (onClose != null)
                {
                    OnClose obj = onClose;
                    onClose = null;
                    obj(this);
                }
            }
        }

        public void Activated()
        {
            ui.Select();
        }

        public void Deactivated()
        {
            if (!ignorePDAInput)
            {
                Inventory.main.quickSlots.Select(prevQuickSlot);
            }
            base.gameObject.SetActive(value: false);
            SNCameraRoot.main.SetFov(0f);
        }

        public void OpenFirst(OnClose onCloseCallback)
        {
            shouldPlayIntro = true;
            Open(PDATab.None, null, onCloseCallback);
        }
    }
}
