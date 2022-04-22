using UnityEngine;

namespace AssemblyCSharp
{
    [SkipProtoContractCheck]
    public class UseableDiveHatch : HandTarget, IHandTarget
    {
        public GameObject outsideExit;

        public GameObject insideSpawn;

        public GameObject obstacleCheck;

        public float exitSearchDistance = 2f;

        [AssertNotNull]
        public string enterCustomText;

        [AssertNotNull]
        public string exitCustomText;

        public string enterCustomGoalText;

        public bool customGoalWithLootOnly;

        public bool enterOnly;

        public bool isForEscapePod;

        public bool isForWaterPark;

        public bool secureInventory = true;

        public PlayerCinematicController enterCinematicController;

        public PlayerCinematicController exitCinematicController;

        private int quickSlot = -1;

        public Vector3 GetDiverSpawnPosition()
        {
            if (!IsInside() || enterOnly)
            {
                return insideSpawn.transform.position;
            }
            return outsideExit.transform.position;
        }

        public Vector3 GetInsideSpawnPosition()
        {
            return insideSpawn.transform.position;
        }

        private void Start()
        {
        }

        private bool GetOnLand()
        {
            if (!(Player.main.GetCurrentSub() != null))
            {
                return Player.main.motorMode == Player.MotorMode.Walk;
            }
            return true;
        }

        private bool IsInside()
        {
            if (Player.main.IsInsideWalkable())
            {
                return Player.main.currentWaterPark == null;
            }
            return false;
        }

        private Vector3 GetExitPosition()
        {
            Vector3 vector = outsideExit.transform.position;
            if (CanExit(vector) || isForWaterPark)
            {
                return vector;
            }
            for (int i = 0; i < 10; i++)
            {
                float f = global::UnityEngine.Random.value * 2f * (float)System.Math.PI;
                Vector3 position = new Vector3(Mathf.Cos(f) * exitSearchDistance, Mathf.Sin(f) * exitSearchDistance, 0f);
                vector = obstacleCheck.transform.TransformPoint(position);
                if (CanExit(vector))
                {
                    return vector;
                }
            }
            Debug.LogWarning("failed to find exit position for hatch " + base.gameObject.name);
            return vector;
        }

        private bool CanExit(Vector3 exitPosition)
        {
            if (obstacleCheck == null)
            {
                return true;
            }
            Vector3 direction = exitPosition - obstacleCheck.transform.position;
            float magnitude = direction.magnitude;
            Debug.DrawLine(obstacleCheck.transform.position, exitPosition, Color.white);
            if (Physics.Raycast(obstacleCheck.transform.position, direction, magnitude, Voxeland.GetTerrainLayerMask()))
            {
                return false;
            }
            int num = global::UWE.Utils.OverlapSphereIntoSharedBuffer(exitPosition, 0.2f, -1, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < num; i++)
            {
                Collider collider = global::UWE.Utils.sharedColliderBuffer[i];
                if (!collider.gameObject || !collider.gameObject.GetComponent<Living>())
                {
                    return false;
                }
            }
            return true;
        }

        private bool StartCinematicMode(PlayerCinematicController cinematicController, Player player)
        {
            if (PlayerCinematicController.cinematicModeCount > 0)
            {
                return false;
            }
            quickSlot = Inventory.main.quickSlots.activeSlot;
            if (!Inventory.main.ReturnHeld())
            {
                return false;
            }
            cinematicController.informGameObject = base.gameObject;
            cinematicController.StartCinematicMode(player);
            return true;
        }

        public void OnPlayerCinematicModeEnd(PlayerCinematicController cinematicController)
        {
            if (cinematicController == enterCinematicController)
            {
                EnterExitHelper.Enter(base.gameObject, cinematicController.GetPlayer(), isForEscapePod);
            }
            else if (cinematicController == exitCinematicController)
            {
                EnterExitHelper.Exit(base.transform, cinematicController.GetPlayer(), isForEscapePod, isForWaterPark);
            }
            if (quickSlot >= 0)
            {
                Inventory.main.quickSlots.Select(quickSlot);
                quickSlot = -1;
            }
        }

        public void OnHandHover(GUIHand hand)
        {
            if (base.enabled)
            {
                string interactText = (IsInside() ? exitCustomText : enterCustomText);
                if (enterOnly)
                {
                    interactText = enterCustomText;
                }
                HandReticle.main.SetInteractText(interactText);
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            if (!base.enabled)
            {
                return;
            }
            bool flag = false;
            Base componentInParent = GetComponentInParent<Base>();
            if (componentInParent != null && !componentInParent.isReady)
            {
                flag = true;
            }
            Player component = hand.gameObject.GetComponent<Player>();
            if (IsInside() && !enterOnly)
            {
                if (!flag && (bool)exitCinematicController && CanExit(outsideExit.transform.position))
                {
                    StartCinematicMode(exitCinematicController, component);
                }
                else
                {
                    component.SetPosition(GetExitPosition());
                    EnterExitHelper.Exit(base.transform, component, isForEscapePod, isForWaterPark);
                    FMODUWE.PlayOneShot("event:/sub/base/enter_hatch", MainCamera.camera.transform.position);
                }
            }
            else
            {
                if (!flag && (bool)enterCinematicController)
                {
                    StartCinematicMode(enterCinematicController, component);
                }
                else
                {
                    Vector3 diverSpawnPosition = GetDiverSpawnPosition();
                    component.SetPosition(diverSpawnPosition);
                    EnterExitHelper.Enter(base.gameObject, component, isForEscapePod);
                    FMODUWE.PlayOneShot("event:/sub/base/enter_hatch", MainCamera.camera.transform.position);
                }
                if (enterCustomGoalText != "" && (!customGoalWithLootOnly || Inventory.main.GetTotalItemCount() > 0))
                {
                    Debug.Log("OnCustomGoalEvent(" + enterCustomText);
                    GoalManager.main.OnCustomGoalEvent(enterCustomGoalText);
                }
            }
            if (secureInventory)
            {
                Inventory.Get().SecureItems(verbose: true);
            }
        }
    }
}
