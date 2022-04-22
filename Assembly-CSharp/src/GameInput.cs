using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    public class GameInput : MonoBehaviour
    {
        public enum Device
        {
            Keyboard,
            Controller
        }

        public enum Button
        {
            Jump,
            PDA,
            Deconstruct,
            Exit,
            LeftHand,
            RightHand,
            CycleNext,
            CyclePrev,
            Slot1,
            Slot2,
            Slot3,
            Slot4,
            Slot5,
            AltTool,
            TakePicture,
            Reload,
            Sprint,
            MoveUp,
            MoveDown,
            MoveForward,
            MoveBackward,
            MoveLeft,
            MoveRight,
            LookUp,
            LookDown,
            LookLeft,
            LookRight,
            UISubmit,
            UICancel,
            UIClear,
            UILeft,
            UIRight,
            UIUp,
            UIDown,
            UIMenu,
            UIAdjustLeft,
            UIAdjustRight,
            UINextTab,
            UIPrevTab,
            Feedback,
            UIRightStickAdjustLeft,
            UIRightStickAdjustRight
        }

        [Flags]
        private enum InputStateFlags : uint
        {
            Down = 0x1u,
            Up = 0x2u,
            Held = 0x4u
        }

        private struct InputState
        {
            public InputStateFlags flags;

            public float timeDown;
        }

        public enum BindingSet
        {
            Primary,
            Secondary
        }

        private enum AnalogAxis
        {
            ControllerRightStickX,
            ControllerRightStickY,
            ControllerLeftStickX,
            ControllerLeftStickY,
            ControllerLeftTrigger,
            ControllerRightTrigger,
            ControllerDPadX,
            ControllerDPadY,
            MouseX,
            MouseY,
            MouseWheel
        }

        public enum ControllerLayout
        {
            Automatic,
            Xbox360,
            XboxOne,
            PS4
        }

        private struct Input
        {
            public string name;

            public KeyCode keyCode;

            public AnalogAxis axis;

            public bool axisPositive;

            public Device device;

            public float axisDeadZone;
        }

        private static bool bindingsChanged = false;

        private static GameInput instance;

        public const float defaultMouseSensitivity = 0.15f;

        public const float defaultControllerSensitivity = 0.405f;

        private static bool invertMouse = false;

        private static float mouseSensitivity = 0.15f;

        private static bool controllerEnabled = PlatformUtils.isConsolePlatform;

        private static bool invertController = false;

        private static Vector2 controllerSensitivity = new Vector2(0.405f, 0.405f);

        private static float[] axisValues;

        private static float[] lastAxisValues;

        private static InputState[] inputStates;

        private static Array3<int> buttonBindings;

        private static int numDevices;

        private static int numButtons;

        private static int numBindingSets;

        private static List<Input> inputs = new List<Input>();

        private static int[] lastInputPressed;

        private static bool scanningInput = false;

        private static Device lastDevice;

        private static ControllerLayout automaticControllerLayout = ControllerLayout.Xbox360;

        private static ControllerLayout chosenControllerLayout = ControllerLayout.Automatic;

        private static bool clearInput = false;

        private static bool keyboardAvailable = false;

        private static bool controllerAvailable = false;

        public static event Action OnBindingsChanged;

        public static event Action OnPrimaryDeviceChanged;

        public static Device GetPrimaryDevice()
        {
            if (PlatformUtils.isConsolePlatform)
            {
                return Device.Controller;
            }
            return lastDevice;
        }

        public static bool IsPrimaryDeviceGamepad()
        {
            if (PlatformUtils.isConsolePlatform)
            {
                return true;
            }
            return lastDevice == Device.Controller;
        }

        public static int GetNumBindingSets()
        {
            return numBindingSets;
        }

        public static bool IsKeyboardAvailable()
        {
            if (PlatformUtils.isConsolePlatform)
            {
                return false;
            }
            return keyboardAvailable;
        }

        public static bool IsControllerAvailable()
        {
            if (PlatformUtils.isConsolePlatform)
            {
                return true;
            }
            return controllerAvailable;
        }

        public static void SetupDefaultBindings(Device device)
        {
            switch (device)
            {
                case Device.Keyboard:
                    SetupDefaultKeyboardBindings();
                    break;
                case Device.Controller:
                    SetupDefaultControllerBindings();
                    break;
            }
        }

        public static bool IsBindable(Device device, Button button)
        {
            if (device == Device.Keyboard && (uint)(button - 23) <= 3u)
            {
                return false;
            }
            if ((uint)(button - 27) <= 14u)
            {
                return false;
            }
            return true;
        }

        private static void SetupDefaultKeyboardBindings()
        {
            ClearBindings(Device.Keyboard);
            SetBindingInternal(Device.Keyboard, Button.Jump, BindingSet.Primary, "Space");
            SetBindingInternal(Device.Keyboard, Button.PDA, BindingSet.Primary, "Tab");
            SetBindingInternal(Device.Keyboard, Button.Deconstruct, BindingSet.Primary, "Q");
            SetBindingInternal(Device.Keyboard, Button.Exit, BindingSet.Primary, "E");
            SetBindingInternal(Device.Keyboard, Button.LeftHand, BindingSet.Primary, "MouseButtonLeft");
            SetBindingInternal(Device.Keyboard, Button.RightHand, BindingSet.Primary, "MouseButtonRight");
            SetBindingInternal(Device.Keyboard, Button.AltTool, BindingSet.Primary, "F");
            SetBindingInternal(Device.Keyboard, Button.Slot1, BindingSet.Primary, "1");
            SetBindingInternal(Device.Keyboard, Button.Slot2, BindingSet.Primary, "2");
            SetBindingInternal(Device.Keyboard, Button.Slot3, BindingSet.Primary, "3");
            SetBindingInternal(Device.Keyboard, Button.Slot4, BindingSet.Primary, "4");
            SetBindingInternal(Device.Keyboard, Button.Slot5, BindingSet.Primary, "5");
            SetBindingInternal(Device.Keyboard, Button.TakePicture, BindingSet.Primary, "F11");
            SetBindingInternal(Device.Keyboard, Button.Reload, BindingSet.Primary, "R");
            SetBindingInternal(Device.Keyboard, Button.MoveForward, BindingSet.Primary, "W");
            SetBindingInternal(Device.Keyboard, Button.MoveBackward, BindingSet.Primary, "S");
            SetBindingInternal(Device.Keyboard, Button.MoveLeft, BindingSet.Primary, "A");
            SetBindingInternal(Device.Keyboard, Button.MoveRight, BindingSet.Primary, "D");
            SetBindingInternal(Device.Keyboard, Button.MoveUp, BindingSet.Primary, "Space");
            SetBindingInternal(Device.Keyboard, Button.MoveDown, BindingSet.Primary, "C");
            SetBindingInternal(Device.Keyboard, Button.Sprint, BindingSet.Primary, "LeftShift");
            SetBindingInternal(Device.Keyboard, Button.CycleNext, BindingSet.Primary, "MouseWheelUp");
            SetBindingInternal(Device.Keyboard, Button.CyclePrev, BindingSet.Primary, "MouseWheelDown");
            SetBindingInternal(Device.Keyboard, Button.CycleNext, BindingSet.Secondary, "LeftBracket");
            SetBindingInternal(Device.Keyboard, Button.CyclePrev, BindingSet.Secondary, "RightBracket");
            SetBindingInternal(Device.Keyboard, Button.UICancel, BindingSet.Primary, "Escape");
            SetBindingInternal(Device.Keyboard, Button.UIMenu, BindingSet.Primary, "Escape");
            SetBindingInternal(Device.Keyboard, Button.UIClear, BindingSet.Primary, "Delete");
            SetBindingInternal(Device.Keyboard, Button.Feedback, BindingSet.Primary, "F8");
            SetBindingInternal(Device.Keyboard, Button.UILeft, BindingSet.Primary, "LeftArrow");
            SetBindingInternal(Device.Keyboard, Button.UIRight, BindingSet.Primary, "RightArrow");
        }

        public static void SetupDefaultControllerBindings()
        {
            ClearBindings(Device.Controller);
            SetBindingInternal(Device.Controller, Button.Jump, BindingSet.Primary, "ControllerButtonY");
            SetBindingInternal(Device.Controller, Button.PDA, BindingSet.Primary, "ControllerButtonBack");
            SetBindingInternal(Device.Controller, Button.Deconstruct, BindingSet.Primary, "ControllerDPadDown");
            SetBindingInternal(Device.Controller, Button.Exit, BindingSet.Primary, "ControllerButtonB");
            SetBindingInternal(Device.Controller, Button.LeftHand, BindingSet.Primary, "ControllerButtonA");
            SetBindingInternal(Device.Controller, Button.LeftHand, BindingSet.Secondary, "ControllerLeftTrigger");
            SetBindingInternal(Device.Controller, Button.RightHand, BindingSet.Primary, "ControllerRightTrigger");
            SetBindingInternal(Device.Controller, Button.AltTool, BindingSet.Primary, "ControllerDPadUp");
            SetBindingInternal(Device.Controller, Button.TakePicture, BindingSet.Primary, "ControllerButtonRightStick");
            SetBindingInternal(Device.Controller, Button.Reload, BindingSet.Primary, "ControllerButtonX");
            SetBindingInternal(Device.Controller, Button.MoveForward, BindingSet.Primary, "ControllerLeftStickUp");
            SetBindingInternal(Device.Controller, Button.MoveBackward, BindingSet.Primary, "ControllerLeftStickDown");
            SetBindingInternal(Device.Controller, Button.MoveLeft, BindingSet.Primary, "ControllerLeftStickLeft");
            SetBindingInternal(Device.Controller, Button.MoveRight, BindingSet.Primary, "ControllerLeftStickRight");
            SetBindingInternal(Device.Controller, Button.MoveUp, BindingSet.Primary, "ControllerButtonLeftBumper");
            SetBindingInternal(Device.Controller, Button.MoveDown, BindingSet.Primary, "ControllerButtonRightBumper");
            SetBindingInternal(Device.Controller, Button.Sprint, BindingSet.Primary, "ControllerButtonLeftStick");
            SetBindingInternal(Device.Controller, Button.LookUp, BindingSet.Primary, "ControllerRightStickUp");
            SetBindingInternal(Device.Controller, Button.LookDown, BindingSet.Primary, "ControllerRightStickDown");
            SetBindingInternal(Device.Controller, Button.LookLeft, BindingSet.Primary, "ControllerRightStickLeft");
            SetBindingInternal(Device.Controller, Button.LookRight, BindingSet.Primary, "ControllerRightStickRight");
            SetBindingInternal(Device.Controller, Button.CycleNext, BindingSet.Primary, "ControllerDPadRight");
            SetBindingInternal(Device.Controller, Button.CyclePrev, BindingSet.Primary, "ControllerDPadLeft");
            SetBindingInternal(Device.Controller, Button.UISubmit, BindingSet.Primary, "ControllerButtonA");
            SetBindingInternal(Device.Controller, Button.UICancel, BindingSet.Primary, "ControllerButtonB");
            SetBindingInternal(Device.Controller, Button.UIClear, BindingSet.Primary, "ControllerButtonX");
            SetBindingInternal(Device.Controller, Button.UIMenu, BindingSet.Primary, "ControllerButtonHome");
            SetBindingInternal(Device.Controller, Button.UILeft, BindingSet.Primary, "ControllerDPadLeft");
            SetBindingInternal(Device.Controller, Button.UIRight, BindingSet.Primary, "ControllerDPadRight");
            SetBindingInternal(Device.Controller, Button.UIDown, BindingSet.Primary, "ControllerDPadDown");
            SetBindingInternal(Device.Controller, Button.UIUp, BindingSet.Primary, "ControllerDPadUp");
            SetBindingInternal(Device.Controller, Button.UILeft, BindingSet.Secondary, "ControllerLeftStickLeftMenu");
            SetBindingInternal(Device.Controller, Button.UIRight, BindingSet.Secondary, "ControllerLeftStickRightMenu");
            SetBindingInternal(Device.Controller, Button.UIDown, BindingSet.Secondary, "ControllerLeftStickDownMenu");
            SetBindingInternal(Device.Controller, Button.UIUp, BindingSet.Secondary, "ControllerLeftStickUpMenu");
            SetBindingInternal(Device.Controller, Button.UIAdjustLeft, BindingSet.Primary, "ControllerLeftStickLeft");
            SetBindingInternal(Device.Controller, Button.UIAdjustRight, BindingSet.Primary, "ControllerLeftStickRight");
            SetBindingInternal(Device.Controller, Button.UIAdjustLeft, BindingSet.Secondary, "ControllerDPadLeft");
            SetBindingInternal(Device.Controller, Button.UIAdjustRight, BindingSet.Secondary, "ControllerDPadRight");
            SetBindingInternal(Device.Controller, Button.UIRightStickAdjustLeft, BindingSet.Primary, "ControllerRightStickLeft");
            SetBindingInternal(Device.Controller, Button.UIRightStickAdjustRight, BindingSet.Primary, "ControllerRightStickRight");
            SetBindingInternal(Device.Controller, Button.UIPrevTab, BindingSet.Primary, "ControllerButtonLeftBumper");
            SetBindingInternal(Device.Controller, Button.UINextTab, BindingSet.Primary, "ControllerButtonRightBumper");
        }

        private static void SetBindingInternal(Device device, Button button, BindingSet bindingSet, int inputIndex)
        {
            buttonBindings[(int)device, (int)button, (int)bindingSet] = inputIndex;
            bindingsChanged = true;
        }

        private static int GetBindingInternal(Device device, Button button, BindingSet bindingSet)
        {
            return buttonBindings[(int)device, (int)button, (int)bindingSet];
        }

        private static void SetBindingInternal(Device device, Button button, BindingSet bindingSet, string input)
        {
            int inputIndex = GetInputIndex(input);
            if (inputIndex == -1 && !string.IsNullOrEmpty(input))
            {
                Debug.LogErrorFormat("GameInput: Input {0} not found", input);
            }
            SetBindingInternal(device, button, bindingSet, inputIndex);
        }

        public static void SetBinding(Device device, Button button, BindingSet bindingSet, string input)
        {
            SetBindingInternal(device, button, bindingSet, input);
        }

        public static string GetBinding(Device device, Button button, BindingSet bindingSet)
        {
            int bindingInternal = GetBindingInternal(device, button, bindingSet);
            if (bindingInternal == -1)
            {
                return null;
            }
            return inputs[bindingInternal].name;
        }

        public static bool IsScanningInput()
        {
            return scanningInput;
        }

        public static string GetPressedInput(Device device)
        {
            scanningInput = true;
            int num = lastInputPressed[(int)device];
            if (num != -1)
            {
                return inputs[num].name;
            }
            return null;
        }

        public static void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = sensitivity;
        }

        public static float GetMouseSensitivity()
        {
            return mouseSensitivity;
        }

        public static void SetControllerHorizontalSensitivity(float sensitivity)
        {
            controllerSensitivity.x = sensitivity;
        }

        public static float GetControllerHorizontalSensitivity()
        {
            return controllerSensitivity.x;
        }

        public static void SetControllerVerticalSensitivity(float sensitivity)
        {
            controllerSensitivity.y = sensitivity;
        }

        public static float GetControllerVerticalSensitivity()
        {
            return controllerSensitivity.y;
        }

        public static void SetInvertMouse(bool _invertMouse)
        {
            invertMouse = _invertMouse;
        }

        public static bool GetInvertMouse()
        {
            return invertMouse;
        }

        public static void SetInvertController(bool _invertController)
        {
            invertController = _invertController;
        }

        public static bool GetInvertController()
        {
            return invertController;
        }

        public static void SetControllerEnabled(bool _controllerEnabled)
        {
            controllerEnabled = _controllerEnabled;
            bindingsChanged = true;
        }

        public static bool GetControllerEnabled()
        {
            return controllerEnabled;
        }

        public static string GetInputName(string bindingName)
        {
            if (GetControllerLayout() == ControllerLayout.PS4)
            {
                switch (bindingName)
                {
                    case "ControllerButtonA":
                        return "ControllerButtonPs4Cross";
                    case "ControllerButtonB":
                        return "ControllerButtonPs4Circle";
                    case "ControllerButtonY":
                        return "ControllerButtonPs4Triangle";
                    case "ControllerButtonX":
                        return "ControllerButtonPs4Square";
                    case "ControllerButtonBack":
                        return "ControllerButtonPs4TouchPad";
                    case "ControllerButtonHome":
                        return "ControllerButtonPs4Options";
                    case "ControllerButtonLeftBumper":
                        return "ControllerPs4L1";
                    case "ControllerButtonRightBumper":
                        return "ControllerPs4R1";
                    case "ControllerLeftTrigger":
                        return "ControllerPs4L2";
                    case "ControllerRightTrigger":
                        return "ControllerPs4R2";
                    case "ControllerButtonLeftStick":
                        return "ControllerButtonPs4LeftStick";
                    case "ControllerButtonRightStick":
                        return "ControllerButtonPs4RightStick";
                    case "ControllerDPadRight":
                        return "ControllerPs4DPadRight";
                    case "ControllerDPadLeft":
                        return "ControllerPs4DPadLeft";
                    case "ControllerDPadUp":
                        return "ControllerPs4DPadUp";
                    case "ControllerDPadDown":
                        return "ControllerPs4DPadDown";
                }
            }
            return bindingName;
        }

        public static string GetBindingName(Button button, BindingSet bindingSet, bool gamepadOnly = false)
        {
            Device device = lastDevice;
            if (gamepadOnly || PlatformUtils.isConsolePlatform)
            {
                device = Device.Controller;
            }
            return GetInputName(GetBinding(device, button, bindingSet));
        }

        public static float GetUIScrollDelta()
        {
            float num = 0f;
            if (controllerEnabled)
            {
                num += 0f - axisValues[1];
            }
            return num;
        }

        public static Vector2 GetUIAdjustDelta()
        {
            Vector2 result = Vector3.zero;
            if (controllerEnabled)
            {
                result.x += axisValues[0];
                result.y -= axisValues[1];
            }
            return result;
        }

        public static Vector2 GetUIDirection(out bool buttonDown)
        {
            Vector2 zero = Vector2.zero;
            buttonDown = false;
            if (GetButtonDown(Button.UIDown))
            {
                buttonDown = true;
                zero.y = 1f;
                return zero;
            }
            if (GetButtonHeld(Button.UIDown))
            {
                zero.y = 1f;
                return zero;
            }
            if (GetButtonDown(Button.UIUp))
            {
                buttonDown = true;
                zero.y = -1f;
                return zero;
            }
            if (GetButtonHeld(Button.UIUp))
            {
                zero.y = -1f;
                return zero;
            }
            if (GetButtonDown(Button.UIRight))
            {
                buttonDown = true;
                zero.x = 1f;
                return zero;
            }
            if (GetButtonHeld(Button.UIRight))
            {
                zero.x = 1f;
                return zero;
            }
            if (GetButtonDown(Button.UILeft))
            {
                buttonDown = true;
                zero.x = -1f;
                return zero;
            }
            if (GetButtonHeld(Button.UILeft))
            {
                zero.x = -1f;
                return zero;
            }
            return zero;
        }

        public static Vector2 GetLookDelta()
        {
            Vector2 zero = Vector2.zero;
            if (!scanningInput && !clearInput)
            {
                if (controllerEnabled)
                {
                    Vector2 zero2 = Vector2.zero;
                    float f = GetAnalogValueForButton(Button.LookRight) - GetAnalogValueForButton(Button.LookLeft);
                    float f2 = GetAnalogValueForButton(Button.LookUp) - GetAnalogValueForButton(Button.LookDown);
                    zero2.x = Mathf.Sign(f) * Mathf.Pow(Mathf.Abs(f), 2f) * 500f * controllerSensitivity.x * Time.deltaTime;
                    zero2.y = Mathf.Sign(f2) * Mathf.Pow(Mathf.Abs(f2), 2f) * 500f * controllerSensitivity.y * Time.deltaTime;
                    if (invertController)
                    {
                        zero2.y = 0f - zero2.y;
                    }
                    zero += zero2;
                }
                if (IsKeyboardAvailable())
                {
                    float num = mouseSensitivity;
                    float num2 = mouseSensitivity;
                    Vector2 zero3 = Vector2.zero;
                    zero3.x += axisValues[8] * num2;
                    zero3.y += axisValues[9] * num;
                    if (invertMouse)
                    {
                        zero3.y = 0f - zero3.y;
                    }
                    zero += zero3;
                }
            }
            return zero;
        }

        public static Vector3 GetMoveDirection()
        {
            float num = 0f;
            num += GetAnalogValueForButton(Button.MoveForward);
            num -= GetAnalogValueForButton(Button.MoveBackward);
            float x = 0f - GetAnalogValueForButton(Button.MoveLeft) + GetAnalogValueForButton(Button.MoveRight);
            float num2 = 0f;
            num2 += GetAnalogValueForButton(Button.MoveUp);
            num2 -= GetAnalogValueForButton(Button.MoveDown);
            return new Vector3(x, num2, num);
        }

        public static bool GetButtonDown(Button button)
        {
            return (GetInputStateForButton(button).flags & InputStateFlags.Down) != 0;
        }

        public static bool GetButtonHeld(Button button)
        {
            return (GetInputStateForButton(button).flags & InputStateFlags.Held) != 0;
        }

        public static float GetButtonHeldTime(Button button)
        {
            InputState inputStateForButton = GetInputStateForButton(button);
            if ((inputStateForButton.flags & InputStateFlags.Held) == 0)
            {
                return 0f;
            }
            return Time.unscaledTime - inputStateForButton.timeDown;
        }

        public static bool GetButtonUp(Button button)
        {
            return (GetInputStateForButton(button).flags & InputStateFlags.Up) != 0;
        }

        public static float GetAnalogValueForButton(Button button)
        {
            float num = 0f;
            if (!clearInput && !scanningInput)
            {
                for (int i = 0; i < numDevices; i++)
                {
                    for (int j = 0; j < numBindingSets; j++)
                    {
                        int bindingInternal = GetBindingInternal((Device)i, button, (BindingSet)j);
                        if (bindingInternal != -1)
                        {
                            if (inputs[bindingInternal].keyCode == KeyCode.None)
                            {
                                num = Mathf.Max(num, axisValues[(int)inputs[bindingInternal].axis] * (inputs[bindingInternal].axisPositive ? 1f : (-1f)));
                            }
                            else if ((inputStates[bindingInternal].flags & InputStateFlags.Held) != 0)
                            {
                                num = 1f;
                            }
                        }
                    }
                }
            }
            return num;
        }

        private static int GetMaximumEnumValue(Type enumType)
        {
            int[] array = (int[])Enum.GetValues(enumType);
            int num = array[0];
            for (int i = 1; i < array.Length; i++)
            {
                int num2 = array[i];
                if (num2 > num)
                {
                    num = num2;
                }
            }
            return num;
        }

        private void Initialize()
        {
            int num = GetMaximumEnumValue(typeof(AnalogAxis)) + 1;
            axisValues = new float[num];
            lastAxisValues = new float[num];
            numButtons = GetMaximumEnumValue(typeof(Button)) + 1;
            numDevices = GetMaximumEnumValue(typeof(Device)) + 1;
            numBindingSets = GetMaximumEnumValue(typeof(BindingSet)) + 1;
            buttonBindings = new Array3<int>(numDevices, numButtons, numBindingSets);
            lastInputPressed = new int[numDevices];
            ClearLastInputPressed();
            foreach (KeyCode value in Enum.GetValues(typeof(KeyCode)))
            {
                if (value != 0 && (value < KeyCode.Joystick1Button0 || value > KeyCode.Joystick8Button19))
                {
                    AddKeyInput(GetKeyCodeAsInputName(value), value, GetKeyCodeDevice(value));
                }
            }
            AddAxisInput("MouseWheelUp", AnalogAxis.MouseWheel, axisPositive: true, Device.Keyboard);
            AddAxisInput("MouseWheelDown", AnalogAxis.MouseWheel, axisPositive: false, Device.Keyboard);
            AddAxisInput("ControllerRightStickRight", AnalogAxis.ControllerRightStickX, axisPositive: true, Device.Controller);
            AddAxisInput("ControllerRightStickLeft", AnalogAxis.ControllerRightStickX, axisPositive: false, Device.Controller);
            AddAxisInput("ControllerRightStickUp", AnalogAxis.ControllerRightStickY, axisPositive: false, Device.Controller);
            AddAxisInput("ControllerRightStickDown", AnalogAxis.ControllerRightStickY, axisPositive: true, Device.Controller);
            AddAxisInput("ControllerLeftStickRight", AnalogAxis.ControllerLeftStickX, axisPositive: true, Device.Controller);
            AddAxisInput("ControllerLeftStickLeft", AnalogAxis.ControllerLeftStickX, axisPositive: false, Device.Controller);
            AddAxisInput("ControllerLeftStickUp", AnalogAxis.ControllerLeftStickY, axisPositive: false, Device.Controller);
            AddAxisInput("ControllerLeftStickDown", AnalogAxis.ControllerLeftStickY, axisPositive: true, Device.Controller);
            AddAxisInput("ControllerLeftStickRightMenu", AnalogAxis.ControllerLeftStickX, axisPositive: true, Device.Controller, 0.75f);
            AddAxisInput("ControllerLeftStickLeftMenu", AnalogAxis.ControllerLeftStickX, axisPositive: false, Device.Controller, 0.75f);
            AddAxisInput("ControllerLeftStickUpMenu", AnalogAxis.ControllerLeftStickY, axisPositive: false, Device.Controller, 0.75f);
            AddAxisInput("ControllerLeftStickDownMenu", AnalogAxis.ControllerLeftStickY, axisPositive: true, Device.Controller, 0.75f);
            AddAxisInput("ControllerLeftTrigger", AnalogAxis.ControllerLeftTrigger, axisPositive: true, Device.Controller);
            AddAxisInput("ControllerRightTrigger", AnalogAxis.ControllerRightTrigger, axisPositive: true, Device.Controller);
            AddAxisInput("ControllerDPadRight", AnalogAxis.ControllerDPadX, axisPositive: true, Device.Controller);
            AddAxisInput("ControllerDPadLeft", AnalogAxis.ControllerDPadX, axisPositive: false, Device.Controller);
            AddAxisInput("ControllerDPadUp", AnalogAxis.ControllerDPadY, axisPositive: true, Device.Controller);
            AddAxisInput("ControllerDPadDown", AnalogAxis.ControllerDPadY, axisPositive: false, Device.Controller);
            inputStates = new InputState[inputs.Count];
        }

        private static void AddKeyInput(string name, KeyCode keyCode, Device device)
        {
            Input item = default(Input);
            item.name = name;
            item.keyCode = keyCode;
            item.device = device;
            inputs.Add(item);
        }

        private static void AddAxisInput(string name, AnalogAxis axis, bool axisPositive, Device device, float deadzone = 0f)
        {
            Input item = default(Input);
            item.name = name;
            item.keyCode = KeyCode.None;
            item.axis = axis;
            item.axisPositive = axisPositive;
            item.axisDeadZone = deadzone;
            item.device = device;
            inputs.Add(item);
        }

        private static void ClearBindings(Device device)
        {
            int inputIndex = -1;
            for (int i = 0; i < numButtons; i++)
            {
                for (int j = 0; j < numBindingSets; j++)
                {
                    SetBindingInternal(device, (Button)i, (BindingSet)j, inputIndex);
                }
            }
        }

        private static InputState GetInputStateForButton(Button button)
        {
            InputState result = default(InputState);
            if (!clearInput && !scanningInput)
            {
                for (int i = 0; i < numDevices; i++)
                {
                    for (int j = 0; j < numBindingSets; j++)
                    {
                        int bindingInternal = GetBindingInternal((Device)i, button, (BindingSet)j);
                        if (bindingInternal != -1)
                        {
                            result.flags |= inputStates[bindingInternal].flags;
                            result.timeDown = Mathf.Max(result.timeDown, inputStates[bindingInternal].timeDown);
                        }
                    }
                }
            }
            return result;
        }

        public static void ClearInput()
        {
            clearInput = true;
        }

        private void Awake()
        {
            if (instance != null)
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
                return;
            }
            instance = this;
            instance.Initialize();
            for (int i = 0; i < numDevices; i++)
            {
                SetupDefaultBindings((Device)i);
            }
        }

        private void OnEnable()
        {
            float repeatRate = 1f;
            InvokeRepeating("UpdateAvailableDevices", 0f, repeatRate);
        }

        private void OnDisable()
        {
            CancelInvoke("UpdateAvailableDevices");
        }

        private void Update()
        {
            Device num = lastDevice;
            ScanInputs();
            if (num != lastDevice)
            {
                bindingsChanged = true;
                if (GameInput.OnPrimaryDeviceChanged != null)
                {
                    GameInput.OnPrimaryDeviceChanged();
                }
            }
            if (bindingsChanged && GameInput.OnBindingsChanged != null)
            {
                GameInput.OnBindingsChanged();
                bindingsChanged = false;
            }
            scanningInput = false;
            clearInput = false;
        }

        private void UpdateAvailableDevices()
        {
            UpdateControllerAvailable();
            UpdateKeyboardAvailable();
            if (PlatformUtils.isConsolePlatform)
            {
                lastDevice = Device.Controller;
                return;
            }
            if (lastDevice == Device.Keyboard && !keyboardAvailable)
            {
                lastDevice = Device.Controller;
            }
            if (lastDevice == Device.Controller && !controllerAvailable)
            {
                lastDevice = Device.Keyboard;
            }
        }

        private void ClearLastInputPressed()
        {
            for (int i = 0; i < numDevices; i++)
            {
                lastInputPressed[i] = -1;
            }
        }

        private Device GetDeviceForAxis(AnalogAxis axis)
        {
            if (axis == AnalogAxis.MouseX || axis == AnalogAxis.MouseY || axis == AnalogAxis.MouseWheel)
            {
                return Device.Keyboard;
            }
            return Device.Controller;
        }

        private void UpdateAxisValues(bool useKeyboard, bool useController)
        {
            for (int i = 0; i < axisValues.Length; i++)
            {
                axisValues[i] = 0f;
            }
            if (useController)
            {
                if (GetUseOculusInputManager())
                {
                    Vector2 vector = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
                    axisValues[2] = vector.x;
                    axisValues[3] = 0f - vector.y;
                    Vector2 vector2 = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
                    axisValues[0] = vector2.x;
                    axisValues[1] = 0f - vector2.y;
                    axisValues[4] = OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger);
                    axisValues[5] = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger);
                    axisValues[6] = 0f;
                    if (OVRInput.Get(OVRInput.RawButton.DpadLeft))
                    {
                        axisValues[6] -= 1f;
                    }
                    if (OVRInput.Get(OVRInput.RawButton.DpadRight))
                    {
                        axisValues[6] += 1f;
                    }
                    axisValues[7] = 0f;
                    if (OVRInput.Get(OVRInput.RawButton.DpadUp))
                    {
                        axisValues[7] += 1f;
                    }
                    if (OVRInput.Get(OVRInput.RawButton.DpadDown))
                    {
                        axisValues[7] -= 1f;
                    }
                }
                else
                {
                    ControllerLayout controllerLayout = GetControllerLayout();
                    if (controllerLayout == ControllerLayout.Xbox360 || controllerLayout == ControllerLayout.XboxOne || Application.platform == RuntimePlatform.PS4)
                    {
                        axisValues[2] = global::UnityEngine.Input.GetAxis("ControllerAxis1");
                        axisValues[3] = global::UnityEngine.Input.GetAxis("ControllerAxis2");
                        axisValues[0] = global::UnityEngine.Input.GetAxis("ControllerAxis4");
                        axisValues[1] = global::UnityEngine.Input.GetAxis("ControllerAxis5");
                        if (Application.platform == RuntimePlatform.PS4)
                        {
                            axisValues[4] = Mathf.Max(global::UnityEngine.Input.GetAxis("ControllerAxis8"), 0f);
                        }
                        else
                        {
                            axisValues[4] = Mathf.Max(global::UnityEngine.Input.GetAxis("ControllerAxis3"), 0f);
                        }
                        axisValues[5] = Mathf.Max(0f - global::UnityEngine.Input.GetAxis("ControllerAxis3"), 0f);
                        axisValues[6] = global::UnityEngine.Input.GetAxis("ControllerAxis6");
                        axisValues[7] = global::UnityEngine.Input.GetAxis("ControllerAxis7");
                    }
                    else if (controllerLayout == ControllerLayout.PS4)
                    {
                        axisValues[2] = global::UnityEngine.Input.GetAxis("ControllerAxis1");
                        axisValues[3] = global::UnityEngine.Input.GetAxis("ControllerAxis2");
                        axisValues[0] = global::UnityEngine.Input.GetAxis("ControllerAxis3");
                        axisValues[1] = global::UnityEngine.Input.GetAxis("ControllerAxis6");
                        axisValues[4] = (global::UnityEngine.Input.GetAxisRaw("ControllerAxis4") + 1f) * 0.5f;
                        axisValues[5] = (global::UnityEngine.Input.GetAxisRaw("ControllerAxis5") + 1f) * 0.5f;
                        axisValues[6] = global::UnityEngine.Input.GetAxis("ControllerAxis7");
                        axisValues[7] = global::UnityEngine.Input.GetAxis("ControllerAxis8");
                    }
                }
            }
            if (useKeyboard)
            {
                axisValues[10] = global::UnityEngine.Input.GetAxis("Mouse ScrollWheel");
                axisValues[8] = global::UnityEngine.Input.GetAxisRaw("Mouse X");
                axisValues[9] = global::UnityEngine.Input.GetAxisRaw("Mouse Y");
            }
            for (int j = 0; j < axisValues.Length; j++)
            {
                AnalogAxis axis = (AnalogAxis)j;
                Device deviceForAxis = GetDeviceForAxis(axis);
                float f = lastAxisValues[j] - axisValues[j];
                lastAxisValues[j] = axisValues[j];
                if (deviceForAxis == lastDevice)
                {
                    continue;
                }
                float num = 0.1f;
                if (Mathf.Abs(f) > num)
                {
                    if (!PlatformUtils.isConsolePlatform)
                    {
                        lastDevice = deviceForAxis;
                    }
                }
                else
                {
                    axisValues[j] = 0f;
                }
            }
        }

        private void UpdateKeyInputs(bool useKeyboard, bool useController)
        {
            ControllerLayout controllerLayout = GetControllerLayout();
            float unscaledTime = Time.unscaledTime;
            int num = -1;
            PlatformServices services = PlatformUtils.main.GetServices();
            if (services != null)
            {
                num = services.GetActiveController();
            }
            for (int i = 0; i < inputs.Count; i++)
            {
                InputState inputState = default(InputState);
                inputState.timeDown = inputStates[i].timeDown;
                Device device = inputs[i].device;
                KeyCode keyCodeForControllerLayout = GetKeyCodeForControllerLayout(inputs[i].keyCode, controllerLayout);
                if (keyCodeForControllerLayout != 0)
                {
                    KeyCode key = keyCodeForControllerLayout;
                    if (num >= 1)
                    {
                        key = keyCodeForControllerLayout + num * 20;
                    }
                    if (global::UnityEngine.Input.GetKey(key))
                    {
                        inputState.flags |= InputStateFlags.Held;
                    }
                    if (global::UnityEngine.Input.GetKeyDown(key))
                    {
                        inputState.flags |= InputStateFlags.Down;
                    }
                    if (global::UnityEngine.Input.GetKeyUp(key))
                    {
                        inputState.flags |= InputStateFlags.Up;
                    }
                    if (inputState.flags != 0 && !PlatformUtils.isConsolePlatform && (controllerEnabled || device != Device.Controller))
                    {
                        lastDevice = device;
                    }
                }
                else
                {
                    bool flag = (inputStates[i].flags & InputStateFlags.Held) != 0;
                    float num2 = axisValues[(int)inputs[i].axis];
                    bool flag2 = ((!inputs[i].axisPositive) ? (num2 < 0f - inputs[i].axisDeadZone) : (num2 > inputs[i].axisDeadZone));
                    if (flag2)
                    {
                        inputState.flags |= InputStateFlags.Held;
                    }
                    if (flag2 && !flag)
                    {
                        inputState.flags |= InputStateFlags.Down;
                    }
                    if (!flag2 && flag)
                    {
                        inputState.flags |= InputStateFlags.Up;
                    }
                }
                if ((inputState.flags & InputStateFlags.Down) != 0)
                {
                    lastInputPressed[(int)device] = i;
                    inputState.timeDown = unscaledTime;
                }
                if ((device == Device.Controller && !useController) || (device == Device.Keyboard && !useKeyboard))
                {
                    inputState.flags = (InputStateFlags)0u;
                    if ((inputStates[i].flags & InputStateFlags.Held) != 0)
                    {
                        inputState.flags |= InputStateFlags.Up;
                    }
                }
                inputStates[i] = inputState;
            }
        }

        private KeyCode GetKeyCodeForControllerLayout(KeyCode keyCode, ControllerLayout controllerLayout)
        {
            if (controllerLayout == ControllerLayout.PS4 && Application.platform != RuntimePlatform.PS4 && keyCode >= KeyCode.JoystickButton0 && keyCode <= KeyCode.Joystick8Button19)
            {
                return keyCode switch
                {
                    KeyCode.JoystickButton0 => KeyCode.JoystickButton1, 
                    KeyCode.JoystickButton1 => KeyCode.JoystickButton2, 
                    KeyCode.JoystickButton2 => KeyCode.JoystickButton0, 
                    KeyCode.JoystickButton3 => KeyCode.JoystickButton3, 
                    KeyCode.JoystickButton4 => KeyCode.JoystickButton4, 
                    KeyCode.JoystickButton5 => KeyCode.JoystickButton5, 
                    KeyCode.JoystickButton6 => KeyCode.JoystickButton13, 
                    KeyCode.JoystickButton7 => KeyCode.JoystickButton9, 
                    KeyCode.JoystickButton8 => KeyCode.JoystickButton10, 
                    KeyCode.JoystickButton9 => KeyCode.JoystickButton11, 
                    KeyCode.JoystickButton10 => KeyCode.JoystickButton8, 
                    KeyCode.JoystickButton11 => KeyCode.JoystickButton15, 
                    KeyCode.JoystickButton12 => KeyCode.JoystickButton12, 
                    KeyCode.JoystickButton13 => KeyCode.JoystickButton6, 
                    KeyCode.JoystickButton14 => KeyCode.JoystickButton7, 
                    KeyCode.JoystickButton15 => KeyCode.JoystickButton14, 
                    _ => keyCode, 
                };
            }
            return keyCode;
        }

        private void ScanInputs()
        {
            bool useKeyboard = IsKeyboardAvailable();
            bool useController = IsControllerAvailable() && controllerEnabled;
            ClearLastInputPressed();
            UpdateAxisValues(useKeyboard, useController);
            UpdateKeyInputs(useKeyboard, useController);
        }

        private static string GetKeyCodeAsInputName(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.Mouse0 => "MouseButtonLeft", 
                KeyCode.Mouse1 => "MouseButtonRight", 
                KeyCode.Mouse2 => "MouseButtonMiddle", 
                KeyCode.JoystickButton0 => "ControllerButtonA", 
                KeyCode.JoystickButton1 => "ControllerButtonB", 
                KeyCode.JoystickButton2 => "ControllerButtonX", 
                KeyCode.JoystickButton3 => "ControllerButtonY", 
                KeyCode.JoystickButton4 => "ControllerButtonLeftBumper", 
                KeyCode.JoystickButton5 => "ControllerButtonRightBumper", 
                KeyCode.JoystickButton6 => "ControllerButtonBack", 
                KeyCode.JoystickButton7 => "ControllerButtonHome", 
                KeyCode.JoystickButton8 => "ControllerButtonLeftStick", 
                KeyCode.JoystickButton9 => "ControllerButtonRightStick", 
                KeyCode.Alpha0 => "0", 
                KeyCode.Alpha1 => "1", 
                KeyCode.Alpha2 => "2", 
                KeyCode.Alpha3 => "3", 
                KeyCode.Alpha4 => "4", 
                KeyCode.Alpha5 => "5", 
                KeyCode.Alpha6 => "6", 
                KeyCode.Alpha7 => "7", 
                KeyCode.Alpha8 => "8", 
                KeyCode.Alpha9 => "9", 
                _ => keyCode.ToString(), 
            };
        }

        private static Device GetKeyCodeDevice(KeyCode keyCode)
        {
            if (keyCode >= KeyCode.JoystickButton0 && keyCode <= KeyCode.Joystick8Button19)
            {
                return Device.Controller;
            }
            return Device.Keyboard;
        }

        private static int GetInputIndex(string name)
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                if (inputs[i].name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        public static void UpdateKeyboardAvailable()
        {
            bool flag = !PlatformUtils.isConsolePlatform;
            if (flag != keyboardAvailable)
            {
                keyboardAvailable = flag;
                bindingsChanged = true;
            }
        }

        public static ControllerLayout GetChosenControllerLayout()
        {
            return chosenControllerLayout;
        }

        public static void SetChosenControllerLayout(ControllerLayout controllerLayout)
        {
            chosenControllerLayout = controllerLayout;
            bindingsChanged = true;
        }

        private static ControllerLayout GetControllerLayout()
        {
            if (chosenControllerLayout == ControllerLayout.Automatic)
            {
                return automaticControllerLayout;
            }
            return chosenControllerLayout;
        }

        private static ControllerLayout GetControllerLayoutFromName(string controllerName)
        {
            return controllerName switch
            {
                "Controller (Xbox One For Windows)" => ControllerLayout.XboxOne, 
                "XBOX 360 For Windows (Controller)" => ControllerLayout.Xbox360, 
                "Wireless Controller" => ControllerLayout.PS4, 
                _ => ControllerLayout.Xbox360, 
            };
        }

        private static bool GetUseOculusInputManager()
        {
            if (!XRSettings.enabled || OVRManager.instance == null)
            {
                return false;
            }
            return (OVRInput.GetConnectedControllers() & OVRInput.Controller.Gamepad) != 0;
        }

        public static void UpdateControllerAvailable()
        {
            ProfilingUtils.BeginSample("GameInput.UpdateControllerAvailable");
            bool flag = false;
            if (GetUseOculusInputManager())
            {
                flag = true;
                automaticControllerLayout = ControllerLayout.XboxOne;
            }
            else if (Application.platform == RuntimePlatform.XboxOne)
            {
                flag = true;
                automaticControllerLayout = ControllerLayout.XboxOne;
            }
            else if (Application.platform == RuntimePlatform.PS4)
            {
                flag = true;
                automaticControllerLayout = ControllerLayout.PS4;
            }
            else
            {
                string[] joystickNames = global::UnityEngine.Input.GetJoystickNames();
                foreach (string text in joystickNames)
                {
                    if (text != string.Empty)
                    {
                        flag = true;
                        automaticControllerLayout = GetControllerLayoutFromName(text);
                        break;
                    }
                }
            }
            if (flag != controllerAvailable)
            {
                controllerAvailable = flag;
                bindingsChanged = true;
            }
            ProfilingUtils.EndSample();
        }

        public static bool GetEnableDeveloperMode()
        {
            if (Application.isEditor)
            {
                return true;
            }
            if (controllerEnabled && global::UnityEngine.Input.GetKey(KeyCode.JoystickButton4))
            {
                if (controllerEnabled)
                {
                    return global::UnityEngine.Input.GetKey(KeyCode.JoystickButton5);
                }
                return false;
            }
            return false;
        }

        public static bool IsBindable(string str)
        {
            switch (str)
            {
                case "ControllerLeftStickRightMenu":
                case "ControllerLeftStickLeftMenu":
                case "ControllerLeftStickUpMenu":
                case "ControllerLeftStickDownMenu":
                    return false;
                default:
                    return true;
            }
        }
    }
}
