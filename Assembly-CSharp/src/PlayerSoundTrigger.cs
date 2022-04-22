using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class PlayerSoundTrigger : MonoBehaviour
    {
        public enum TriggerType
        {
            OnEnter,
            OnExit,
            OnLookAt
        }

        public FMODAsset asset;

        public bool onlyOnce = true;

        public TriggerType triggerType;

        public bool onlyOutside;

        public bool onlyAboveWater;

        [NonSerialized]
        [ProtoMember(1)]
        public bool triggered;

        private void Start()
        {
            if (onlyOnce && triggered)
            {
                DisableTriggers();
            }
            else if (triggerType == TriggerType.OnLookAt)
            {
                InvokeRepeating("CheckLookAt", 1f, 1f);
            }
        }

        private void DisableTriggers()
        {
            Collider component = GetComponent<Collider>();
            if ((bool)component && component.isTrigger)
            {
                component.enabled = false;
            }
        }

        private bool CheckPlayerIsLookingAt()
        {
            return Vector3.Dot(Vector3.Normalize(base.transform.position - MainCamera.camera.transform.position), MainCamera.camera.transform.forward) > 0.8f;
        }

        private bool CheckTriggeringAllowed()
        {
            if ((!onlyOnce || !triggered) && (!onlyOutside || !Player.main.IsInsideWalkable()) && (!onlyAboveWater || Player.main.transform.position.y >= 0.1f))
            {
                if (triggerType == TriggerType.OnLookAt)
                {
                    return CheckPlayerIsLookingAt();
                }
                return true;
            }
            return false;
        }

        private void CheckLookAt()
        {
            if (CheckTriggeringAllowed())
            {
                if (onlyOnce)
                {
                    CancelInvoke("CheckLookAt");
                }
                PlaySound();
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (triggerType == TriggerType.OnEnter && CheckTriggeringAllowed() && global::UWE.Utils.GetComponentInHierarchy<Player>(collider.gameObject) != null)
            {
                PlaySound();
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (triggerType == TriggerType.OnExit && CheckTriggeringAllowed() && global::UWE.Utils.GetComponentInHierarchy<Player>(collider.gameObject) != null)
            {
                PlaySound();
            }
        }

        private void PlaySound()
        {
            if (onlyOnce)
            {
                DisableTriggers();
            }
            Utils.PlayFMODAsset(asset, base.transform, 0f);
            triggered = true;
        }
    }
}
