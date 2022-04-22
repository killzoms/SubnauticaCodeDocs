using UnityEngine;

namespace AssemblyCSharp
{
    public class DockedVehicleHandTarget : CinematicModeTriggerBase
    {
        [AssertNotNull]
        public VehicleDockingBay dockingBay;

        [AssertNotNull]
        public PlayerCinematicController seamothCinematicController;

        [AssertNotNull]
        public PlayerCinematicController exosuitCinematicController;

        public void Start()
        {
            InvokeRepeating("UpdateValid", Random.value, 1f);
            base.isValidHandTarget = false;
        }

        protected override void OnStartCinematicMode()
        {
            dockingBay.OnUndockingStart();
        }

        public override void OnHandHover(GUIHand hand)
        {
            Vehicle dockedVehicle = dockingBay.GetDockedVehicle();
            if (dockedVehicle != null)
            {
                bool flag = false;
                CrushDamage crushDamage = dockedVehicle.crushDamage;
                if (crushDamage != null)
                {
                    float crushDepth = crushDamage.crushDepth;
                    if (Ocean.main.GetDepthOf(Player.main.gameObject) > crushDepth)
                    {
                        flag = true;
                    }
                }
                string text = ((dockedVehicle is Exosuit) ? "EnterExosuit" : "EnterSeamoth");
                if (flag)
                {
                    HandReticle.main.SetInteractText(text, "DockedVehicleDepthWarning");
                    return;
                }
                EnergyMixin component = dockedVehicle.GetComponent<EnergyMixin>();
                LiveMixin liveMixin = dockedVehicle.liveMixin;
                if (component.charge < component.capacity)
                {
                    string format = Language.main.GetFormat("VehicleStatusFormat", liveMixin.GetHealthFraction(), component.GetEnergyScalar());
                    HandReticle.main.SetInteractText(text, format, translate1: true, translate2: false, HandReticle.Hand.Left);
                }
                else
                {
                    string format2 = Language.main.GetFormat("VehicleStatusChargedFormat", liveMixin.GetHealthFraction());
                    HandReticle.main.SetInteractText(text, format2, translate1: true, translate2: false, HandReticle.Hand.Left);
                }
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
            }
            else
            {
                HandReticle.main.SetInteractInfo("NoVehicleDocked");
            }
        }

        private void UpdateValid()
        {
            base.isValidHandTarget = dockingBay.GetDockedVehicle() != null;
        }

        private void OnPlayerCinematicModeStart(PlayerCinematicController cinematicController)
        {
            dockingBay.subRoot.BroadcastMessage("OnLaunchBayOpening", SendMessageOptions.DontRequireReceiver);
        }

        public new void OnPlayerCinematicModeEnd(PlayerCinematicController cinematicController)
        {
            dockingBay.OnUndockingComplete(cinematicController.GetPlayer());
        }

        public override void OnHandClick(GUIHand hand)
        {
            if (dockingBay.GetDockedVehicle() != null)
            {
                cinematicController = ((dockingBay.GetDockedVehicle() is Exosuit) ? exosuitCinematicController : seamothCinematicController);
            }
            base.OnHandClick(hand);
        }
    }
}
