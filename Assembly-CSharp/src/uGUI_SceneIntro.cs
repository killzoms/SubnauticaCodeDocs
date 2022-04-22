using System.Collections;
using AssemblyCSharp.Story;
using UnityEngine;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(FMOD_StudioEventEmitter))]
    public class uGUI_SceneIntro : uGUI_Scene, IInputHandler
    {
        private delegate bool Condition();

        public uGUI_Fader fader;

        public uGUI_TextFade mainText;

        public uGUI_TextFade skipText;

        public float timeBeforeCinematicStart;

        public float timeLaunching;

        public float timeBlackout;

        public float blackoutDuration;

        public FMOD_StudioEventEmitter emitter;

        public FMODAsset unmuteEvent;

        public string launchingText;

        private bool moveNext;

        private Coroutine coroutine;

        private bool hasHitMenu;

        private bool menuStillDown;

        public bool showing => coroutine != null;

        private void Start()
        {
            if (Screen.fullScreen)
            {
                fader.SetColor(Color.black);
            }
            else
            {
                fader.SetColor(Color.white);
            }
            fader.SetState(base.enabled);
            fader.FadeOut();
            mainText.SetState(enabled: false);
            skipText.SetState(enabled: false);
            UpdateBindings();
            GameInput.OnBindingsChanged += OnBindingsChanged;
        }

        private void OnDestroy()
        {
            GameInput.OnBindingsChanged -= OnBindingsChanged;
        }

        private void OnBindingsChanged()
        {
            UpdateBindings();
        }

        private void UpdateBindings()
        {
            string format = Language.main.GetFormat("SkipIntro", uGUI.FormatButton(GameInput.Button.UIMenu));
            skipText.SetText(format, translate: true);
        }

        public void Play()
        {
            if (!showing)
            {
                coroutine = StartCoroutine(IntroSequence());
                InputHandlerStack.main.Push(this);
                BiomeGoalTracker.main.enabled = false;
            }
        }

        public void Stop(bool isInterrupted)
        {
            ResumeGameTime();
            MainMenuMusic.Stop();
            VRLoadingOverlay.Hide();
            BiomeGoalTracker.main.enabled = true;
            if (showing)
            {
                StopCoroutine(coroutine);
                coroutine = null;
                skipText.SetState(enabled: false);
                mainText.SetState(enabled: false);
                fader.SetState(enabled: false);
                emitter.Stop();
                Utils.PlayFMODAsset(unmuteEvent);
                EscapePod.main.StopIntroCinematic(isInterrupted);
            }
        }

        private IEnumerator IntroSequence()
        {
            fader.SetColor(Color.black);
            fader.SetState(enabled: true);
            PauseGameTime();
            yield return new WaitForSecondsRealtime(0.5f);
            mainText.SetText("");
            yield return new WaitForSecondsRealtime(2f);
            while (!LargeWorldStreamer.main.IsWorldSettled())
            {
                yield return new WaitForSecondsRealtime(1f);
            }
            mainText.SetText(Language.main.Get("PressAnyButton"));
            mainText.SetState(enabled: true);
            VRLoadingOverlay.Hide();
            while (!Input.anyKeyDown)
            {
                yield return null;
            }
            moveNext = false;
            mainText.FadeOut(0.2f, Callback);
            while (!moveNext)
            {
                yield return null;
            }
            emitter.StartEvent();
            float timeFootStepSoundStart = Time.time;
            MainMenuMusic.Stop();
            EscapePod.main.TriggerIntroCinematic();
            uGUI_EscapePod.main.SetHeader(Language.main.Get("IntroEscapePod1Header"), new Color32(243, 94, 63, byte.MaxValue), 4f);
            uGUI_EscapePod.main.SetContent(Language.main.Get("IntroEscapePod1Content"), new Color32(233, 63, 27, byte.MaxValue));
            uGUI_EscapePod.main.SetPower(Language.main.Get("IntroEscapePod1Power"), new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));
            skipText.FadeOut(5f, null);
            mainText.SetText(Language.main.Get("IntroUWEPresents"), translate: true);
            moveNext = false;
            mainText.FadeIn(1f, Callback);
            while (!moveNext)
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(3f);
            mainText.FadeOut(1f, Callback);
            while (Time.time < timeFootStepSoundStart + timeBeforeCinematicStart)
            {
                yield return null;
            }
            moveNext = false;
            fader.FadeOut(3f, Callback);
            if (XRSettings.enabled && VROptions.skipIntro)
            {
                Stop(isInterrupted: true);
                GameObject gameObject = GameObject.Find("fire_extinguisher_01_tp");
                if (gameObject != null)
                {
                    Object.Destroy(gameObject);
                }
                GameObject gameObject2 = GameObject.Find("IntroFireExtinugisherPickup");
                if (gameObject2 != null)
                {
                    Object.Destroy(gameObject2);
                }
            }
            while (Time.time < timeFootStepSoundStart + timeLaunching)
            {
                yield return null;
            }
            if (!string.IsNullOrEmpty(launchingText))
            {
                Subtitles.main.Add(launchingText);
            }
            while (Time.time < timeFootStepSoundStart + timeBlackout)
            {
                yield return null;
            }
            fader.SetState(enabled: true);
            yield return new WaitForSecondsRealtime(blackoutDuration);
            uGUI_EscapePod.main.SetHeader(Language.main.Get("IntroEscapePod2Header"), new Color32(243, 94, 63, byte.MaxValue), 4f);
            uGUI_EscapePod.main.SetContent(Language.main.Get("IntroEscapePod2Content"), new Color32(233, 63, 27, byte.MaxValue));
            uGUI_EscapePod.main.SetPower(Language.main.Get("IntroEscapePod2Power"), new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));
            fader.FadeOut(4f, Callback);
            while (EscapePod.main.IsPlayingIntroCinematic())
            {
                yield return null;
            }
            Stop(isInterrupted: false);
            StartCoroutine(ControlsHints());
        }

        private void ResumeGameTime()
        {
            DayNightCycle main = DayNightCycle.main;
            if (main == null)
            {
                return;
            }
            main.Resume();
            Player main2 = Player.main;
            if (!(main2 == null))
            {
                Survival component = main2.GetComponent<Survival>();
                if (!(component == null))
                {
                    component.freezeStats = false;
                }
            }
        }

        private void PauseGameTime()
        {
            DayNightCycle main = DayNightCycle.main;
            if (main == null)
            {
                return;
            }
            main.Pause();
            Player main2 = Player.main;
            if (!(main2 == null))
            {
                Survival component = main2.GetComponent<Survival>();
                if (!(component == null))
                {
                    component.freezeStats = true;
                }
            }
        }

        private void Callback()
        {
            moveNext = true;
        }

        private IEnumerator EscapeHold()
        {
            yield return new WaitForSeconds(1f);
            if (GameInput.GetButtonHeld(GameInput.Button.UIMenu))
            {
                menuStillDown = true;
                yield break;
            }
            hasHitMenu = false;
            menuStillDown = false;
            skipText.FadeOut(1f, null);
        }

        bool IInputHandler.HandleInput()
        {
            if (!showing)
            {
                return false;
            }
            if (GameInput.GetButtonHeld(GameInput.Button.UIMenu))
            {
                if (menuStillDown)
                {
                    Stop(isInterrupted: true);
                    return false;
                }
                if (!hasHitMenu)
                {
                    hasHitMenu = true;
                    skipText.SetState(enabled: false);
                    skipText.FadeIn(1f, null);
                    StartCoroutine(EscapeHold());
                }
            }
            return true;
        }

        bool IInputHandler.HandleLateInput()
        {
            return true;
        }

        public void OnFocusChanged(InputFocusMode mode)
        {
        }

        private IEnumerator ControlsHints()
        {
            Hint.main.message.anchor = TextAnchor.UpperCenter;
            Player player = Player.main;
            yield return new WaitForSeconds(1f);
            Condition condition = () => GameInput.GetLookDelta().sqrMagnitude > 0f;
            bool gamepad = GameInput.GetPrimaryDevice() == GameInput.Device.Controller || PlatformUtils.isConsolePlatform;
            string message = ((!gamepad) ? Language.main.GetFormat("HintLook", string.Format("<color=#ADF8FFFF>{0}</color>", Language.main.Get("Mouse"))) : Language.main.GetFormat("HintLook", string.Format("<color=#ADF8FFFF>{0}</color>", Language.main.Get("ControllerRightStick"))));
            yield return HintRoutine(10f, condition, message, Language.main.Get("HintSuccess"));
            condition = () => GameInput.GetMoveDirection().sqrMagnitude > 0f;
            if (gamepad)
            {
                message = Language.main.GetFormat("HintMove", string.Format("<color=#ADF8FFFF>{0}</color>", Language.main.GetFormat("ControllerLeftStick")));
            }
            else
            {
                string arg = $"{uGUI.FormatButton(GameInput.Button.MoveForward)}, {uGUI.FormatButton(GameInput.Button.MoveBackward)}, {uGUI.FormatButton(GameInput.Button.MoveLeft)}, {uGUI.FormatButton(GameInput.Button.MoveRight)}";
                message = Language.main.GetFormat("HintMove", arg);
            }
            yield return HintRoutine(5f, condition, message, Language.main.Get("HintSuccess"));
            condition = () => Inventory.main.container.Contains(TechType.FireExtinguisher);
            message = Language.main.GetFormat("HintPickupFireExtinguisher", uGUI.FormatButton(GameInput.Button.LeftHand));
            yield return HintRoutine(5f, condition, message, Language.main.Get("HintSuccess"));
            condition = delegate
            {
                Pickupable held = Inventory.main.GetHeld();
                return held != null && held.GetTechType() == TechType.FireExtinguisher && player.GetRightHandDown();
            };
            message = Language.main.GetFormat("HintUseFireExtinguisher", uGUI.FormatButton(GameInput.Button.RightHand));
            yield return HintRoutine(1f, condition, message, Language.main.Get("HintSuccess"));
            PDA pda = player.GetPDA();
            while (pda.state != 0)
            {
                yield return null;
            }
            condition = () => pda.state == PDA.State.Closing;
            message = Language.main.GetFormat("HintOpenClosePDA", uGUI.FormatButton(GameInput.Button.PDA));
            yield return HintRoutine(30f, condition, message, Language.main.Get("HintSuccess"));
            while (pda.state != PDA.State.Closed)
            {
                yield return null;
            }
            QuickSlots quickSlots = Inventory.main.quickSlots;
            yield return null;
            int slot = quickSlots.GetActiveSlotID();
            condition = () => slot != quickSlots.GetActiveSlotID();
            message = ((!gamepad) ? Language.main.GetFormat("HintKeyboardQuickslots", uGUI.FormatButton(GameInput.Button.Slot1), uGUI.FormatButton(GameInput.Button.Slot2), uGUI.FormatButton(GameInput.Button.Slot3), uGUI.FormatButton(GameInput.Button.Slot4), uGUI.FormatButton(GameInput.Button.Slot5)) : Language.main.GetFormat("HintGamepadQuickslots", uGUI.FormatButton(GameInput.Button.CyclePrev), uGUI.FormatButton(GameInput.Button.CycleNext)));
            yield return HintRoutine(1f, condition, message, Language.main.Get("HintSuccess"));
        }

        private IEnumerator HintRoutine(float timeout, Condition condition, string message, string success)
        {
            uGUI_PopupMessage hint = Hint.main.message;
            while (!condition())
            {
                timeout -= Time.deltaTime;
                if (timeout > 0f)
                {
                    yield return null;
                    continue;
                }
                hint.SetText(message, TextAnchor.MiddleCenter);
                timeout = 60f;
                hint.Show(timeout);
                while (!condition())
                {
                    if (timeout > 0f)
                    {
                        yield return null;
                        timeout -= Time.deltaTime;
                        continue;
                    }
                    yield break;
                }
                hint.SetText(success, TextAnchor.MiddleCenter);
                hint.Show(1f);
                break;
            }
        }
    }
}
