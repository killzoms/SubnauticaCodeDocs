using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class InputHandlerStack : MonoBehaviour
    {
        private class Wrapper
        {
            private GameObject legacyHandler;

            private IInputHandler handler;

            public string name
            {
                get
                {
                    if (legacyHandler != null)
                    {
                        return legacyHandler.name;
                    }
                    if (handler != null)
                    {
                        return "[IInputHandler] " + handler.GetHashCode();
                    }
                    return "None";
                }
            }

            public Wrapper(GameObject handler)
            {
                legacyHandler = handler;
            }

            public Wrapper(IInputHandler handler)
            {
                this.handler = handler;
            }

            public void SetActive(InputFocusMode mode)
            {
                if (legacyHandler != null)
                {
                    legacyHandler.SetActive(mode == InputFocusMode.Add || mode == InputFocusMode.Restore);
                }
                else
                {
                    handler.OnFocusChanged(mode);
                }
            }

            public bool Equals(GameObject handler)
            {
                return object.Equals(legacyHandler, handler);
            }

            public bool Equals(IInputHandler handler)
            {
                return object.Equals(this.handler, handler);
            }

            public bool HandleInput()
            {
                if (legacyHandler != null)
                {
                    return true;
                }
                if (handler != null)
                {
                    return handler.HandleInput();
                }
                return false;
            }

            public bool HandleLateInput()
            {
                if (legacyHandler != null)
                {
                    return true;
                }
                if (handler != null)
                {
                    return handler.HandleLateInput();
                }
                return false;
            }
        }

        public static InputHandlerStack main;

        private static bool debug;

        public GameObject defaultHandler;

        private int lastPopFrame = -1;

        private Stack<Wrapper> stack = new Stack<Wrapper>();

        private void Awake()
        {
            if (main != null)
            {
                Debug.LogError("More than one InputHandlerStack instance!");
                Object.Destroy(this);
            }
            else
            {
                main = this;
            }
        }

        private void Start()
        {
            Push(defaultHandler);
        }

        private void Update()
        {
            if (Time.frameCount == lastPopFrame + 1 && stack.Count > 0)
            {
                stack.Peek().SetActive(InputFocusMode.Restore);
            }
            if (lastPopFrame != Time.frameCount && stack.Count > 0 && !stack.Peek().HandleInput())
            {
                Pop();
            }
        }

        private void LateUpdate()
        {
            if (lastPopFrame != Time.frameCount && stack.Count > 0 && !stack.Peek().HandleLateInput())
            {
                Pop();
            }
        }

        public void Push(IInputHandler handler)
        {
            Push(new Wrapper(handler));
        }

        public bool IsFocused(IInputHandler handler)
        {
            return stack.Peek()?.Equals(handler) ?? false;
        }

        public void Push(GameObject handler)
        {
            Push(new Wrapper(handler));
        }

        public void Pop(GameObject handler)
        {
            if (stack.Count > 0)
            {
                if (stack.Peek().Equals(handler))
                {
                    Pop();
                }
                else
                {
                    Debug.LogError($"InputHandlerStack push/pop mismatch! GameObject named {handler.name} tried to pop when it wasn't on top.");
                }
            }
            else
            {
                Debug.LogError("Nothing to Pop(). Input handler stack is empty.");
            }
        }

        private void Push(Wrapper wrapper)
        {
            if (debug)
            {
                Debug.Log("pushing input handler " + wrapper.name);
            }
            if (stack.Count > 0)
            {
                stack.Peek().SetActive(InputFocusMode.Suspend);
            }
            stack.Push(wrapper);
            wrapper.SetActive(InputFocusMode.Add);
        }

        private void Pop()
        {
            Wrapper wrapper = stack.Pop();
            if (debug)
            {
                Debug.Log("popping input handler " + wrapper.name);
            }
            wrapper.SetActive(InputFocusMode.Remove);
            lastPopFrame = Time.frameCount;
            if (stack.Count == 0)
            {
                Debug.LogError("Warning: Just popped the very last input handler!");
            }
        }
    }
}
