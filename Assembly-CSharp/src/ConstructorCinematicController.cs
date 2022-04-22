using UnityEngine;

namespace AssemblyCSharp
{
    public class ConstructorCinematicController : MonoBehaviour
    {
        [AssertNotNull]
        public PlayerCinematicController engageCinematicController;

        [AssertNotNull]
        public PlayerCinematicController disengageCinematicController;

        [AssertNotNull]
        public ConstructorInput constructorInput;

        [AssertNotNull]
        public Animator animator;

        [AssertNotNull]
        public Transform playerAttach;

        private Player usingPlayer;

        private int quickSlot = -1;

        public bool inUse => usingPlayer != null;

        private void Update()
        {
            if (inUse)
            {
                bool flag = !usingPlayer.liveMixin.IsAlive();
                if (flag || uGUI.main.craftingMenu.client != constructorInput)
                {
                    DisengageConstructor(usingPlayer, flag);
                }
            }
        }

        private void LateUpdate()
        {
            if (inUse)
            {
                usingPlayer.SetPosition(playerAttach.position, playerAttach.rotation);
            }
        }

        public void OnPlayerCinematicModeEnd(PlayerCinematicController cinematicController)
        {
            if (cinematicController == engageCinematicController)
            {
                Player player = (usingPlayer = cinematicController.GetPlayer());
                if (GameOptions.GetVrAnimationMode())
                {
                    ForcedSetAnimParams(engageParamValue: true, disengageParamValue: false, null);
                }
                player.playerController.SetEnabled(enabled: false);
                MainCameraControl.main.cinematicMode = true;
                MainCameraControl.main.lookAroundMode = true;
                MainCameraControl.main.viewModel.localRotation = Quaternion.identity;
                constructorInput.StartUse();
            }
            else if (quickSlot != -1)
            {
                Inventory.main.quickSlots.Select(quickSlot);
            }
        }

        public void EngageConstructor(Player player)
        {
            quickSlot = Inventory.main.quickSlots.activeSlot;
            if (Inventory.main.ReturnHeld())
            {
                ResetAnimParams(player.playerAnimator);
                engageCinematicController.StartCinematicMode(player);
            }
        }

        public void DisengageConstructor()
        {
            if (inUse)
            {
                DisengageConstructor(usingPlayer);
            }
        }

        public void DisengageConstructor(Player player, bool skipCinematics = false)
        {
            if (usingPlayer != null && player == usingPlayer)
            {
                if (!skipCinematics)
                {
                    ResetAnimParams(player.playerAnimator);
                    disengageCinematicController.StartCinematicMode(player);
                }
                else
                {
                    animator.SetTrigger("reset");
                    player.cinematicModeActive = false;
                }
                if (GameOptions.GetVrAnimationMode())
                {
                    ForcedSetAnimParams(engageParamValue: false, disengageParamValue: true, null);
                }
            }
            usingPlayer = null;
            MainCameraControl.main.lookAroundMode = false;
            constructorInput.EndUse();
        }

        private void ResetAnimParams(Animator playerAnimator)
        {
            bool vrAnimationMode = GameOptions.GetVrAnimationMode();
            SafeAnimator.SetBool(animator, "cinematics", !vrAnimationMode);
            ForcedSetAnimParams(engageParamValue: false, disengageParamValue: false, vrAnimationMode ? null : playerAnimator);
        }

        private void ForcedSetAnimParams(bool engageParamValue, bool disengageParamValue, Animator playerAnimator)
        {
            SafeAnimator.SetBool(animator, engageCinematicController.animParam, engageParamValue);
            SafeAnimator.SetBool(animator, disengageCinematicController.animParam, disengageParamValue);
            if (playerAnimator != null)
            {
                SafeAnimator.SetBool(playerAnimator, engageCinematicController.playerViewAnimationName, engageParamValue);
                SafeAnimator.SetBool(playerAnimator, disengageCinematicController.playerViewAnimationName, disengageParamValue);
            }
        }
    }
}
