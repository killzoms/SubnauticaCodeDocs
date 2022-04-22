using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class PrecursorTeleporter : MonoBehaviour
    {
        public delegate void TeleportAction();

        public string teleporterIdentifier;

        public Vector3 warpToPos;

        public float warpToAngle;

        [AssertNotNull]
        public PlayerCinematicController cinematicTriggerIn;

        [AssertNotNull]
        public GameObject cinematicEndController;

        [AssertNotNull]
        public GameObject portalFxPrefab;

        [AssertNotNull]
        public Transform portalFxSpawnPoint;

        public Transform registerPrisonExitPoint;

        public bool alwaysOn;

        public bool hasFirstUse = true;

        private GameObject warpObject;

        private VFXPrecursorTeleporter portalFxControl;

        [NonSerialized]
        [ProtoMember(1)]
        public bool isOpen;

        [AssertNotNull]
        public FMODAsset powerUpSound;

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter activeLoopSound;

        public static event TeleportAction TeleportEventStart;

        public static event TeleportAction TeleportEventEnd;

        private void OnEnable()
        {
            TeleporterManager.TeleporterActivateEvent += OnActivateTeleporter;
        }

        private void OnDisable()
        {
            TeleporterManager.TeleporterActivateEvent -= OnActivateTeleporter;
        }

        private void Start()
        {
            GameObject gameObject = Utils.SpawnZeroedAt(portalFxPrefab, portalFxSpawnPoint);
            portalFxControl = gameObject.GetComponent<VFXPrecursorTeleporter>();
            if (alwaysOn)
            {
                InitializeDoor(open: true);
            }
            else
            {
                bool teleporterActive = TeleporterManager.GetTeleporterActive(teleporterIdentifier);
                InitializeDoor(teleporterActive);
            }
            if (registerPrisonExitPoint != null)
            {
                PrisonManager.main.RegisterExitPoint(registerPrisonExitPoint.position);
            }
        }

        private void SetWarpPosition()
        {
            if (!(warpObject == null))
            {
                Quaternion quaternion = Quaternion.Euler(new Vector3(0f, warpToAngle, 0f));
                if ((bool)warpObject.GetComponentInChildren<Vehicle>())
                {
                    warpObject.GetComponentInChildren<Vehicle>().TeleportVehicle(warpToPos, quaternion);
                }
                else
                {
                    warpObject.transform.position = warpToPos;
                    warpObject.transform.rotation = quaternion;
                }
                Player.main.WaitForTeleportation();
                warpObject = null;
            }
        }

        public static void TeleportationComplete()
        {
            if (PrecursorTeleporter.TeleportEventEnd != null)
            {
                PrecursorTeleporter.TeleportEventEnd();
            }
        }

        public void OnPlayerCinematicModeEnd(PlayerCinematicController controller)
        {
            warpObject = null;
            BeginTeleportPlayer(Player.main.gameObject);
        }

        public void BeginTeleportPlayer(GameObject teleportObject)
        {
            if (!alwaysOn && (!TeleporterManager.GetTeleporterActive(teleporterIdentifier) || warpObject != null))
            {
                return;
            }
            warpObject = teleportObject;
            bool flag = teleportObject.Equals(Player.main.gameObject);
            bool flag2 = Player.main.AddUsedTool(TechType.PrecursorTeleporter);
            if (hasFirstUse && flag2 && flag)
            {
                cinematicTriggerIn.cinematicModeActive = false;
                cinematicTriggerIn.StartCinematicMode(Player.main);
                return;
            }
            Player.main.playerController.inputEnabled = false;
            Inventory.main.quickSlots.SetIgnoreHotkeyInput(ignore: true);
            Player.main.GetPDA().SetIgnorePDAInput(ignore: true);
            Player.main.playerController.SetEnabled(enabled: false);
            Player.main.teleportingLoopSound.Play();
            if (PrecursorTeleporter.TeleportEventStart != null)
            {
                PrecursorTeleporter.TeleportEventStart();
            }
            Invoke("SetWarpPosition", 1f);
            if (flag)
            {
                Quaternion identity = Quaternion.identity;
                identity = Quaternion.Euler(0f, warpToAngle, 0f);
                global::UnityEngine.Object.Instantiate(cinematicEndController, warpToPos, identity);
            }
        }

        private void OnActivateTeleporter(string identifier)
        {
            if (!(identifier != teleporterIdentifier))
            {
                ToggleDoor(open: true);
            }
        }

        private void InitializeDoor(bool open)
        {
            if (portalFxControl != null)
            {
                portalFxControl.Toggle(open);
            }
            if (open && !isOpen)
            {
                isOpen = true;
                TeleporterManager.ActivateTeleporter(teleporterIdentifier);
                activeLoopSound.Play();
            }
        }

        public void ToggleDoor(bool open)
        {
            if (portalFxControl != null)
            {
                if (open)
                {
                    portalFxControl.FadeIn();
                }
                else
                {
                    portalFxControl.FadeOut();
                }
            }
            if (open && !isOpen)
            {
                isOpen = true;
                Utils.PlayFMODAsset(powerUpSound, base.transform);
                TeleporterManager.ActivateTeleporter(teleporterIdentifier);
                activeLoopSound.Play();
            }
        }
    }
}
