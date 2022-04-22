using System.Collections;
using UnityEngine;

namespace AssemblyCSharp
{
    [SkipProtoContractCheck]
    public class BulkheadDoor : HandTarget, IHandTarget
    {
        private enum State
        {
            Zero,
            In,
            One,
            Out
        }

        public delegate void OnStateChange(bool open);

        public bool initiallyOpen;

        public Transform playerAnchor;

        public Animation doorAnimation;

        public Collider doorCollider;

        public Vector3 playerOffsetAngles = new Vector3(0f, 180f, 0f);

        public Vector3 cameraOffsetAngles = new Vector3(0f, 180f, 0f);

        public Transform frontSideDummy;

        public float sideDistanceThreshold = 0.7f;

        public float timeIn = 0.25f;

        public float timeOut = 0.25f;

        [Range(0f, 1f)]
        public float cameraCrossfade = 0.3f;

        public string viewFrontOpen = "hatch_frontopen";

        public string viewFrontClose = "hatch_frontclose";

        public string viewBackOpen = "hatch_backopen";

        public string viewBackClose = "hatch_backclose";

        public string doorFrontOpen = "FrontOpen";

        public string doorFrontClose = "FrontClose";

        public string doorBackOpen = "BackOpen";

        public string doorBackClose = "BackClose";

        public FMODAsset soundFrontOpen;

        public FMODAsset soundFrontClose;

        public FMODAsset soundBackOpen;

        public FMODAsset soundBackClose;

        public OnStateChange onStateChange;

        private bool initialized;

        private Sequence sequence;

        private State state;

        private bool targetState;

        private AnimationState animState;

        private string viewClipName;

        private string doorClipName;

        private FMODAsset sound;

        private Vector3 playerFromPosition = Vector3.zero;

        private Quaternion playerFromRotation = Quaternion.identity;

        private Vector3 cameraPosition = Vector3.zero;

        private Quaternion cameraRotation = Quaternion.identity;

        private bool stateSet;

        private static int quickSlot = -1;

        public bool inTransition => state != State.Zero;

        public bool isOpen
        {
            get
            {
                if (!inTransition)
                {
                    return targetState;
                }
                return false;
            }
        }

        public bool isClosed
        {
            get
            {
                if (!inTransition)
                {
                    return !targetState;
                }
                return false;
            }
        }

        public bool isOpening
        {
            get
            {
                if (inTransition)
                {
                    return targetState;
                }
                return false;
            }
        }

        public bool isClosing
        {
            get
            {
                if (inTransition)
                {
                    return !targetState;
                }
                return false;
            }
        }

        public override void Awake()
        {
            base.Awake();
            Initialize();
            if (frontSideDummy == null)
            {
                Debug.LogError("BulkheadDoor : Awake() : frontSideDummy is not assigned!");
            }
        }

        private void Update()
        {
            sequence.Update();
            if (sequence.active && state == State.One)
            {
                animState.normalizedTime = sequence.t;
            }
        }

        private void LateUpdate()
        {
            if (sequence.active)
            {
                Player main = Player.main;
                Transform component = main.GetComponent<Transform>();
                Transform component2 = MainCameraControl.main.GetComponent<Transform>();
                Quaternion quaternion = Quaternion.Euler(playerOffsetAngles);
                Quaternion quaternion2 = Quaternion.Euler(cameraOffsetAngles);
                switch (state)
                {
                    case State.In:
                        component.position = Vector3.Lerp(playerFromPosition, playerAnchor.position, sequence.t);
                        component.rotation = Quaternion.Slerp(playerFromRotation, playerAnchor.rotation * quaternion, sequence.t);
                        component2.position = main.camAnchor.position + cameraPosition * (1f - sequence.t);
                        component2.rotation = main.camAnchor.rotation * Quaternion.Slerp(cameraRotation, quaternion2, sequence.t);
                        break;
                    case State.One:
                    {
                        float t = Mathf.Clamp01(1f / cameraCrossfade * sequence.t);
                        component.position = playerAnchor.position;
                        component.rotation = playerAnchor.rotation * quaternion;
                        component2.position = Vector3.Lerp(cameraPosition, main.camAnchor.position, t);
                        cameraPosition = component2.position;
                        component2.rotation = Quaternion.Slerp(cameraRotation, main.camAnchor.rotation * quaternion2, t);
                        cameraRotation = component2.rotation;
                        break;
                    }
                    case State.Out:
                        component2.position = Vector3.Lerp(cameraPosition, main.camAnchor.position, sequence.t);
                        cameraPosition = component2.position;
                        break;
                    case State.Zero:
                        break;
                }
            }
        }

        public void OnHandHover(GUIHand hand)
        {
            if (base.enabled && state == State.Zero)
            {
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
                HandReticle.main.SetInteractText(targetState ? "Close" : "Open");
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            Base componentInParent = GetComponentInParent<Base>();
            if (componentInParent != null && !componentInParent.isReady)
            {
                ToggleImmediately();
            }
            else if (base.enabled && state == State.Zero)
            {
                if (GameOptions.GetVrAnimationMode())
                {
                    ToggleImmediately();
                }
                else
                {
                    SequenceDone();
                }
            }
        }

        private void Initialize()
        {
            if (!initialized)
            {
                sequence = new Sequence();
                IEnumerator enumerator = doorAnimation.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    AnimationState obj = (AnimationState)enumerator.Current;
                    obj.enabled = false;
                    obj.layer = 1;
                    obj.speed = 0f;
                    obj.blendMode = AnimationBlendMode.Blend;
                    obj.weight = 0f;
                    obj.normalizedTime = 0f;
                }
                SampleAnimationState(initiallyOpen ? doorFrontClose : doorFrontOpen);
                targetState = initiallyOpen;
                sequence.ForceState(initiallyOpen);
                sequence.Update();
                SetColliderState(state: true);
                initialized = true;
            }
        }

        private void ResetAnimations()
        {
            IEnumerator enumerator = doorAnimation.GetEnumerator();
            while (enumerator.MoveNext())
            {
                AnimationState obj = (AnimationState)enumerator.Current;
                obj.enabled = false;
                obj.normalizedTime = 0f;
                obj.weight = 0f;
            }
        }

        private AnimationState SetAnimationState(string name)
        {
            AnimationState animationState = doorAnimation[name];
            if (animationState == null)
            {
                return null;
            }
            animationState.weight = 1f;
            animationState.enabled = true;
            return animationState;
        }

        private void SampleAnimationState(string name)
        {
            AnimationState animationState = doorAnimation[name];
            if (!(animationState == null))
            {
                animationState.normalizedTime = 0f;
                animationState.weight = 1f;
                animationState.enabled = true;
                doorAnimation.Sample();
                animationState.enabled = false;
            }
        }

        private void ToggleImmediately()
        {
            if (!sequence.active)
            {
                Transform component = Player.main.GetComponent<Transform>();
                targetState = !targetState;
                SetClips();
                ResetAnimations();
                animState = SetAnimationState(doorClipName);
                animState.normalizedTime = 1f;
                doorAnimation.Sample();
                component.position = playerAnchor.position;
                doorClipName = null;
                viewClipName = null;
                sound = null;
                NotifyStateChange();
            }
        }

        private void SequenceDone()
        {
            Player main = Player.main;
            Transform component = main.GetComponent<Transform>();
            MainCameraControl main2 = MainCameraControl.main;
            Transform component2 = main2.GetComponent<Transform>();
            PlayerController playerController = main.playerController;
            switch (state)
            {
                case State.Zero:
                    quickSlot = Inventory.main.quickSlots.activeSlot;
                    if (Inventory.main.ReturnHeld())
                    {
                        targetState = !targetState;
                        SetClips();
                        ResetAnimations();
                        animState = SetAnimationState(doorClipName);
                        state = State.In;
                        SetColliderState(state: false);
                        doorAnimation.Sample();
                        cameraPosition = main.camAnchor.InverseTransformPoint(component2.position);
                        cameraRotation = Quaternion.Inverse(main.camAnchor.rotation) * component2.rotation;
                        component.localRotation *= main2.viewModel.localRotation;
                        main2.viewModel.localRotation = Quaternion.identity;
                        playerFromPosition = component.position;
                        playerFromRotation = component.rotation;
                        main.forceCinematicMode = true;
                        sequence.Set(timeIn, current: false, target: true, SequenceDone);
                        main2.enabled = false;
                        main2.rotationX = 0f;
                        main2.rotationY = 0f;
                        global::UWE.Utils.lockCursor = true;
                        playerController.SetEnabled(enabled: false);
                    }
                    break;
                case State.In:
                {
                    state = State.One;
                    cameraPosition = component2.position;
                    cameraRotation = component2.rotation;
                    float length = animState.clip.length;
                    sequence.Set(length, current: false, target: true, SequenceDone);
                    if (!string.IsNullOrEmpty(viewClipName))
                    {
                        Animator componentInChildren2 = main.GetComponentInChildren<Animator>();
                        if (componentInChildren2 != null && componentInChildren2.gameObject.activeInHierarchy)
                        {
                            SafeAnimator.SetBool(componentInChildren2, viewClipName, value: true);
                        }
                    }
                    if (sound != null)
                    {
                        FMODUWE.PlayOneShot(sound, frontSideDummy.position);
                    }
                    break;
                }
                case State.One:
                    state = State.Out;
                    cameraPosition = component2.position;
                    cameraRotation = component2.rotation;
                    sequence.Set(timeOut, current: false, target: true, SequenceDone);
                    SetColliderState(state: true);
                    if (!string.IsNullOrEmpty(viewClipName))
                    {
                        Animator componentInChildren = main.GetComponentInChildren<Animator>();
                        if (componentInChildren != null && componentInChildren.gameObject.activeInHierarchy)
                        {
                            SafeAnimator.SetBool(componentInChildren, viewClipName, value: false);
                        }
                    }
                    playerController.SetEnabled(enabled: true);
                    Inventory.main.quickSlots.Select(quickSlot);
                    break;
                case State.Out:
                    state = State.Zero;
                    doorClipName = null;
                    viewClipName = null;
                    sound = null;
                    main2.enabled = true;
                    main.forceCinematicMode = false;
                    NotifyStateChange();
                    break;
            }
        }

        private bool GetSide()
        {
            Transform aimingTransform = SNCameraRoot.main.GetAimingTransform();
            Vector3 position = aimingTransform.position;
            Vector3 position2 = frontSideDummy.position;
            Vector3 forward = frontSideDummy.forward;
            float num = Vector3.Dot(position - position2, forward);
            if (Mathf.Abs(num) > sideDistanceThreshold)
            {
                return num < 0f;
            }
            num = Vector3.Dot(aimingTransform.forward, forward);
            if (Mathf.Approximately(num, 0f))
            {
                return true;
            }
            return num > 0f;
        }

        private void SetClips()
        {
            if (GetSide())
            {
                if (targetState)
                {
                    viewClipName = viewFrontOpen;
                    doorClipName = doorFrontOpen;
                    sound = soundFrontOpen;
                }
                else
                {
                    viewClipName = viewFrontClose;
                    doorClipName = doorFrontClose;
                    sound = soundFrontClose;
                }
            }
            else if (targetState)
            {
                viewClipName = viewBackOpen;
                doorClipName = doorBackOpen;
                sound = soundBackOpen;
            }
            else
            {
                viewClipName = viewBackClose;
                doorClipName = doorBackClose;
                sound = soundBackClose;
            }
        }

        private void SetColliderState(bool state)
        {
            if (doorCollider != null)
            {
                doorCollider.enabled = state;
            }
        }

        private void NotifyStateChange()
        {
            if (onStateChange != null)
            {
                onStateChange(targetState);
            }
        }

        public void SetState(bool open)
        {
            Initialize();
            if (!stateSet)
            {
                stateSet = true;
                if (!sequence.active && targetState != open)
                {
                    targetState = open;
                    state = State.Zero;
                    sequence.ForceState(state: false);
                    ResetAnimations();
                    SampleAnimationState(open ? doorFrontClose : doorFrontOpen);
                    SetColliderState(state: true);
                    NotifyStateChange();
                }
            }
        }
    }
}
