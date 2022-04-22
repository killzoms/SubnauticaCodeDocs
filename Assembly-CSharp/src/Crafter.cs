using System;
using ProtoBuf;

namespace AssemblyCSharp
{
    [ProtoContract]
    [ProtoInclude(5100, typeof(PowerCrafter))]
    [ProtoInclude(5200, typeof(ConstructorInput))]
    [ProtoInclude(5500, typeof(GhostCrafter))]
    [ProtoInclude(5600, typeof(RocketConstructorInput))]
    [ProtoInclude(5700, typeof(Incubator))]
    public abstract class Crafter : HandTarget, ITreeActionReceiver
    {
        protected const float craftingTimeAutoCloseThreshold = 5.9f;

        public CrafterLogic crafterLogic;

        protected bool _initialized;

        protected bool _state;

        private CrafterLogic _logic;

        private ITreeActionSender _sender;

        public CrafterLogic logic
        {
            get
            {
                return _logic;
            }
            set
            {
                if (_logic != value)
                {
                    if (base.enabled)
                    {
                        Deinitialize();
                    }
                    _logic = value;
                    if (base.enabled)
                    {
                        Initialize();
                    }
                }
            }
        }

        protected bool state
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged(_state);
                }
            }
        }

        bool ITreeActionReceiver.inProgress
        {
            get
            {
                if (_logic != null)
                {
                    return _logic.inProgress;
                }
                return false;
            }
        }

        protected virtual void OnEnable()
        {
            Initialize();
        }

        public override void Awake()
        {
            base.Awake();
            logic = crafterLogic;
        }

        protected virtual void Start()
        {
        }

        protected virtual void OnDisable()
        {
            Deinitialize();
        }

        protected virtual void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;
                if (_logic != null)
                {
                    CrafterLogic obj = _logic;
                    obj.onItemChanged = (CrafterLogic.OnItemChanged)Delegate.Combine(obj.onItemChanged, new CrafterLogic.OnItemChanged(CrafterOnItemChanged));
                    CrafterLogic obj2 = _logic;
                    obj2.onProgress = (CrafterLogic.OnProgress)Delegate.Combine(obj2.onProgress, new CrafterLogic.OnProgress(CrafterOnProgress));
                    CrafterLogic obj3 = _logic;
                    obj3.onDone = (CrafterLogic.OnDone)Delegate.Combine(obj3.onDone, new CrafterLogic.OnDone(CrafterOnDone));
                    state = _logic.inProgress;
                }
            }
        }

        protected virtual void Deinitialize()
        {
            if (_initialized)
            {
                _initialized = false;
                if (_logic != null)
                {
                    CrafterLogic obj = _logic;
                    obj.onItemChanged = (CrafterLogic.OnItemChanged)Delegate.Remove(obj.onItemChanged, new CrafterLogic.OnItemChanged(CrafterOnItemChanged));
                    CrafterLogic obj2 = _logic;
                    obj2.onProgress = (CrafterLogic.OnProgress)Delegate.Remove(obj2.onProgress, new CrafterLogic.OnProgress(CrafterOnProgress));
                    CrafterLogic obj3 = _logic;
                    obj3.onDone = (CrafterLogic.OnDone)Delegate.Remove(obj3.onDone, new CrafterLogic.OnDone(CrafterOnDone));
                }
                state = false;
            }
        }

        protected virtual void Craft(TechType techType, float duration)
        {
            if (_logic != null && _logic.Craft(techType, duration))
            {
                state = true;
                OnCraftingBegin(techType, duration);
            }
        }

        protected virtual void OnCraftingBegin(TechType techType, float duration)
        {
            if (!GameInput.GetButtonHeld(GameInput.Button.Sprint))
            {
                if (duration > 5.9f)
                {
                    uGUI.main.craftingMenu.Close(this);
                }
                else
                {
                    uGUI.main.craftingMenu.Lock(this);
                }
            }
        }

        protected virtual void OnCraftingEnd()
        {
        }

        protected virtual void OnStateChanged(bool crafting)
        {
        }

        protected virtual void OnItemChanged(TechType techType)
        {
        }

        protected virtual void OnProgress(float progress)
        {
        }

        protected bool HasCraftedItem()
        {
            if (_logic != null)
            {
                return _logic.craftingTechType != TechType.None;
            }
            return false;
        }

        private void CrafterOnItemChanged(TechType techType)
        {
            OnItemChanged(techType);
        }

        private void CrafterOnProgress(float progress)
        {
            if (_sender != null)
            {
                _sender.Progress(progress);
            }
            OnProgress(progress);
        }

        private void CrafterOnDone()
        {
            if (_sender != null)
            {
                _sender.Done();
                _sender = null;
            }
            state = false;
            _ = logic.craftingTechType;
            OnCraftingEnd();
        }

        bool ITreeActionReceiver.PerformAction(ITreeActionSender sender, TechType techType)
        {
            if (_logic == null || HasCraftedItem())
            {
                return false;
            }
            if (techType == TechType.None)
            {
                return false;
            }
            _sender = sender;
            Craft(techType, 3f);
            return _logic.craftingTechType != TechType.None;
        }
    }
}
