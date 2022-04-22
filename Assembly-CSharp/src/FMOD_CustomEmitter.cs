using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace AssemblyCSharp
{
    public class FMOD_CustomEmitter : MonoBehaviour, IManagedUpdateBehaviour, IManagedBehaviour
    {
        [AssertNotNull]
        public FMODAsset asset;

        public bool playOnAwake;

        public bool followParent;

        public bool restartOnPlay;

        public bool debug;

        private EventInstance evt;

        private ATTRIBUTES_3D attributes;

        private bool _playing;

        private float length;

        private bool addedToUpdateManager;

        public bool playing => _playing;

        public int managedUpdateIndex { get; set; }

        public string GetProfileTag()
        {
            return "FMOD_CustomEmitter";
        }

        protected virtual void OnPlay()
        {
            BehaviourUpdateUtils.Register(this);
        }

        protected virtual void OnStop()
        {
            BehaviourUpdateUtils.Deregister(this);
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnSetEvent(EventInstance eventInstance)
        {
        }

        public void Play()
        {
            CacheEventInstance();
            if (debug && !evt.hasHandle())
            {
                global::UnityEngine.Debug.Log("FMOD: tried to start sound without any event assigned");
            }
            if ((!_playing || restartOnPlay) && evt.hasHandle())
            {
                ProfilingUtils.BeginSample("GetPlaybackState");
                bool flag = false;
                if (evt.getPlaybackState(out var state) == RESULT.OK)
                {
                    _playing = state != PLAYBACK_STATE.STOPPED;
                    flag = state == PLAYBACK_STATE.STOPPING;
                }
                ProfilingUtils.EndSample();
                if (_playing)
                {
                    ProfilingUtils.BeginSample("RestartOnPlay");
                    if (!flag)
                    {
                        Stop();
                    }
                    ReleaseEvent();
                    CacheEventInstance();
                    ProfilingUtils.EndSample();
                }
                UpdateEventAttributes();
                evt.start();
                if (debug)
                {
                    global::UnityEngine.Debug.Log("FMOD: starting sound");
                }
                _playing = true;
                OnPlay();
            }
            if (debug && !evt.hasHandle())
            {
                global::UnityEngine.Debug.Log("FMOD: tried to play but evt is null");
            }
        }

        public void Stop()
        {
            if (_playing && evt.hasHandle())
            {
                evt.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                if (debug)
                {
                    global::UnityEngine.Debug.Log("FMOD: stopping sound");
                }
                _playing = false;
                OnStop();
            }
        }

        private bool ReleaseEvent()
        {
            if (evt.hasHandle())
            {
                Stop();
                evt.release();
                evt.clearHandle();
                return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            ReleaseEvent();
            BehaviourUpdateUtils.Deregister(this);
        }

        private void Awake()
        {
            SetAsset(asset);
        }

        private void CacheEventInstance()
        {
            if (!evt.hasHandle())
            {
                SetAsset(asset);
            }
        }

        public void SetAsset(FMODAsset newAsset)
        {
            ReleaseEvent();
            asset = newAsset;
            if (newAsset != null)
            {
                evt = FMODUWE.GetEvent(asset);
                OnSetEvent(evt);
                if (!evt.hasHandle())
                {
                    global::UnityEngine.Debug.LogError("FMOD: " + base.gameObject.name + ".FMOD_CustomEmitter: could not load fmod event: " + asset.path + " id: " + asset.id);
                }
                attributes = default(ATTRIBUTES_3D);
                attributes.velocity = Vector3.zero.ToFMODVector();
            }
        }

        protected virtual void Start()
        {
            if (playOnAwake)
            {
                Play();
            }
        }

        private void UpdateEventAttributes()
        {
            attributes = base.transform.To3DAttributes();
            evt.set3DAttributes(attributes);
        }

        public void SetParameterValue(string paramname, float value)
        {
            CacheEventInstance();
            ProfilingUtils.BeginSample("SetParameterValue");
            if (evt.hasHandle())
            {
                evt.setParameterValue(paramname, value);
            }
            ProfilingUtils.EndSample();
        }

        public void SetParameterValue(int paramIndex, float value)
        {
            CacheEventInstance();
            ProfilingUtils.BeginSample("SetParameterValue");
            if (evt.hasHandle())
            {
                evt.setParameterValueByIndex(paramIndex, value);
            }
            ProfilingUtils.EndSample();
        }

        public int GetParameterIndex(string paramName)
        {
            CacheEventInstance();
            if (evt.hasHandle())
            {
                return FMODUWE.GetEventInstanceParameterIndex(evt, paramName);
            }
            return -1;
        }

        public void ManagedUpdate()
        {
            if (followParent && evt.hasHandle() && _playing)
            {
                UpdateEventAttributes();
            }
            OnUpdate();
        }

        private void OnEnable()
        {
            if (debug)
            {
                global::UnityEngine.Debug.Log("FMOD: enable event " + asset.path);
            }
            if (playOnAwake)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            if (debug)
            {
                global::UnityEngine.Debug.Log("FMOD: disable event " + asset.path);
            }
            Stop();
        }

        public EventInstance GetEventInstance()
        {
            return evt;
        }
    }
}
