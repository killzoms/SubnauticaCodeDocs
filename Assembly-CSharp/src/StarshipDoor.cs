using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class StarshipDoor : HandTarget, IHandTarget
    {
        public enum OpenMethodEnum
        {
            Manual,
            Sealed,
            Keycard,
            Powercell
        }

        public OpenMethodEnum doorOpenMethod;

        [AssertNotNull]
        public string openText = "OpenDoor";

        [AssertNotNull]
        public string closeText = "CloseDoor";

        [NonSerialized]
        [ProtoMember(1)]
        public bool doorOpen;

        [NonSerialized]
        [ProtoMember(2)]
        public bool doorLocked = true;

        public bool startDoorOpen;

        public bool requirePlayerInFrontToOpen;

        public GameObject doorObject;

        public FMOD_CustomEmitter openSound;

        private Sealed sealedComponent;

        private Vector3 closedPos;

        private Vector3 openPos;

        private void OnEnable()
        {
            NoCostConsoleCommand.main.UnlockDoorsEvent += OnUnlockDoorsCheat;
        }

        private void OnDisable()
        {
            NoCostConsoleCommand.main.UnlockDoorsEvent -= OnUnlockDoorsCheat;
        }

        private void OnUnlockDoorsCheat()
        {
            if (NoCostConsoleCommand.main.unlockDoors)
            {
                UnlockDoor();
            }
            else
            {
                LockDoor();
            }
        }

        private void Start()
        {
            if (!doorObject)
            {
                doorObject = base.gameObject;
            }
            sealedComponent = GetComponent<Sealed>();
            if (sealedComponent != null)
            {
                sealedComponent.openedEvent.AddHandler(base.gameObject, OnSealedDoorOpen);
            }
            closedPos = doorObject.transform.position;
            openPos = doorObject.transform.TransformPoint(new Vector3(0f, 1.6f, 0f));
            if (startDoorOpen || doorOpen)
            {
                doorLocked = false;
                doorOpen = true;
                doorObject.transform.position = openPos;
            }
            if (!doorLocked)
            {
                UnlockDoor();
            }
            if (NoCostConsoleCommand.main.unlockDoors)
            {
                UnlockDoor();
            }
        }

        private void Update()
        {
            if (doorOpenMethod == OpenMethodEnum.Manual)
            {
                Vector3 position = doorObject.transform.position;
                position = ((!doorOpen) ? Vector3.Lerp(position, closedPos, Time.deltaTime * 2f) : Vector3.Lerp(position, openPos, Time.deltaTime * 2f));
                doorObject.transform.position = position;
            }
        }

        public void OnHandHover(GUIHand hand)
        {
            switch (doorOpenMethod)
            {
                case OpenMethodEnum.Manual:
                    if (!doorLocked)
                    {
                        HandReticle.main.SetInteractText(doorOpen ? closeText : openText);
                        HandReticle.main.SetIcon(HandReticle.IconType.Hand);
                    }
                    break;
                case OpenMethodEnum.Sealed:
                    HandReticle.main.SetInteractInfo("Sealed_Door", "SealedInstructions");
                    HandReticle.main.SetProgress(sealedComponent.GetSealedPercentNormalized());
                    HandReticle.main.SetIcon(HandReticle.IconType.Progress);
                    break;
                case OpenMethodEnum.Keycard:
                    HandReticle.main.SetInteractInfo("Locked_Door", "DoorInstructions_Keycard");
                    HandReticle.main.SetIcon(HandReticle.IconType.Hand);
                    break;
                case OpenMethodEnum.Powercell:
                    HandReticle.main.SetInteractInfo("Locked_Door", "DoorInstructions_Powercell");
                    HandReticle.main.SetIcon(HandReticle.IconType.Hand);
                    break;
            }
        }

        public void OnHandClick(GUIHand guiHand)
        {
            if ((!requirePlayerInFrontToOpen || Utils.CheckObjectInFront(base.transform, Player.main.transform)) && !doorLocked)
            {
                OnDoorToggle();
            }
        }

        public void UnlockDoor()
        {
            doorLocked = false;
            StarshipDoorLocked component = GetComponent<StarshipDoorLocked>();
            if (component != null)
            {
                component.SetDoorLockState(locked: false);
            }
        }

        public void LockDoor()
        {
            doorLocked = true;
            StarshipDoorLocked component = GetComponent<StarshipDoorLocked>();
            if (component != null)
            {
                component.SetDoorLockState(locked: true);
            }
        }

        private void OnDoorToggle()
        {
            if (doorOpenMethod == OpenMethodEnum.Manual)
            {
                doorOpen = !doorOpen;
            }
            if ((bool)openSound)
            {
                openSound.Play();
            }
        }

        private void OnSealedDoorOpen(Sealed sealedComponent)
        {
            OnDoorToggle();
        }
    }
}
