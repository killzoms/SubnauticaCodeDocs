using UnityEngine;

namespace AssemblyCSharp
{
    public class Bench : CinematicModeTrigger
    {
        private enum BenchSide
        {
            None,
            Front,
            Back
        }

        private bool isSitting;

        private Player currentPlayer;

        [AssertNotNull]
        public Animator animator;

        public Transform playerTarget;

        public Vector3 frontAnimRotation = new Vector3(0f, 90f, 0f);

        public Vector3 backAnimRotation = new Vector3(0f, 270f, 0f);

        public PlayerCinematicController standUpCinematicController;

        public GameObject frontObstacleCheck;

        public GameObject backObstacleCheck;

        public float checkDistance;

        private LayerMask checkLayerMask;

        public void Start()
        {
            onCinematicEnd.AddListener(OnCinematicEnd);
            checkLayerMask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Trigger")));
        }

        private void Update()
        {
            if (!(currentPlayer != null))
            {
                return;
            }
            if (isSitting)
            {
                HandReticle.main.SetUseText("StandUp");
                if (GameInput.GetButtonDown(GameInput.Button.Exit) && !currentPlayer.GetPDA().isInUse)
                {
                    ExitSittingMode(currentPlayer);
                }
            }
            else
            {
                Subscribe(currentPlayer, state: false);
                currentPlayer = null;
            }
        }

        public void OnCinematicEnd(CinematicModeEventData eventData)
        {
            if (eventData.cinematicController == cinematicController)
            {
                currentPlayer = eventData.player;
                Subscribe(currentPlayer, state: true);
                EnterSittingMode(currentPlayer);
            }
        }

        private void LateUpdate()
        {
            if (isSitting && currentPlayer != null)
            {
                currentPlayer.SetPosition(playerTarget.position, playerTarget.rotation);
            }
        }

        public override void OnHandClick(GUIHand hand)
        {
            base.isValidHandTarget = GetCanBeUsedBy(hand.player);
            if (base.isValidHandTarget)
            {
                BenchSide side = GetSide(hand.player);
                if (side == BenchSide.None)
                {
                    ErrorMessage.AddWarning(Language.main.Get("NotEnoughSpaceToSit"));
                    return;
                }
                animator.transform.localEulerAngles = ((side == BenchSide.Front) ? frontAnimRotation : backAnimRotation);
                ResetAnimParams(hand.player.playerAnimator);
                base.OnHandClick(hand);
            }
        }

        public override void OnHandHover(GUIHand hand)
        {
            base.isValidHandTarget = GetCanBeUsedBy(hand.player);
            if (base.isValidHandTarget && hand.IsFreeToInteract())
            {
                HandReticle.main.SetInteractText(handText);
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
            }
        }

        private bool GetCanBeUsedBy(Player player)
        {
            if (base.enabled && player.GetMode() == Player.Mode.Normal)
            {
                return !player.IsUnderwater();
            }
            return false;
        }

        private void SwitchSittingEffects(Player player, bool switchOn)
        {
            player.GetComponent<Survival>().freezeStats = switchOn;
        }

        private BenchSide GetSide(Player player)
        {
            bool flag = CanSit(frontObstacleCheck);
            bool flag2 = CanSit(backObstacleCheck);
            if (!flag && !flag2)
            {
                return BenchSide.None;
            }
            if ((base.transform.InverseTransformPoint(player.transform.position).z >= 0f && flag) || !flag2)
            {
                return BenchSide.Front;
            }
            return BenchSide.Back;
        }

        private bool CanSit(GameObject obstacleCheckObj)
        {
            return !Physics.Raycast(new Ray(obstacleCheckObj.transform.position, obstacleCheckObj.transform.forward), checkDistance, checkLayerMask);
        }

        private void EnterSittingMode(Player player)
        {
            isSitting = true;
            SwitchSittingEffects(player, switchOn: true);
            if (GameOptions.GetVrAnimationMode())
            {
                ForcedSetAnimParams(sitParamValue: true, standUpParamValue: false, player.playerAnimator);
            }
            player.playerController.SetEnabled(enabled: false);
            MainCameraControl.main.cinematicMode = true;
            MainCameraControl.main.lookAroundMode = true;
            MainCameraControl.main.viewModel.localRotation = Quaternion.identity;
            player.EnterSittingMode();
        }

        private void ExitSittingMode(Player player, bool skipCinematics = false)
        {
            isSitting = false;
            SwitchSittingEffects(player, switchOn: false);
            if (player == currentPlayer)
            {
                if (!skipCinematics)
                {
                    ResetAnimParams(player.playerAnimator);
                    standUpCinematicController.StartCinematicMode(player);
                }
                if (GameOptions.GetVrAnimationMode() || skipCinematics)
                {
                    ForcedSetAnimParams(sitParamValue: false, standUpParamValue: true, player.playerAnimator);
                }
            }
            MainCameraControl.main.lookAroundMode = false;
            player.ExitSittingMode();
        }

        private void Subscribe(Player player, bool state)
        {
            if (!(player == null))
            {
                if (state)
                {
                    currentPlayer.playerDeathEvent.AddHandler(base.gameObject, OnPlayerDeath);
                    currentPlayer.isUnderwater.changedEvent.AddHandler(base.gameObject, CheckIfUnderwater);
                }
                else
                {
                    currentPlayer.playerDeathEvent.RemoveHandler(base.gameObject, OnPlayerDeath);
                    currentPlayer.isUnderwater.changedEvent.RemoveHandler(base.gameObject, CheckIfUnderwater);
                }
            }
        }

        private void OnPlayerDeath(Player player)
        {
            if (!(currentPlayer != player))
            {
                animator.Rebind();
                ExitSittingMode(player, skipCinematics: true);
                player.cinematicModeActive = false;
            }
        }

        private void CheckIfUnderwater(Utils.MonitoredValue<bool> isUnderwater)
        {
            if (currentPlayer != null && currentPlayer.isUnderwater.value)
            {
                ExitSittingMode(currentPlayer);
            }
        }

        private void ResetAnimParams(Animator playerAnimator)
        {
            SafeAnimator.SetBool(animator, "cinematics", !GameOptions.GetVrAnimationMode());
            ForcedSetAnimParams(sitParamValue: false, standUpParamValue: false, playerAnimator);
        }

        private void ForcedSetAnimParams(bool sitParamValue, bool standUpParamValue, Animator playerAnimator)
        {
            SafeAnimator.SetBool(animator, cinematicController.animParam, sitParamValue);
            SafeAnimator.SetBool(animator, standUpCinematicController.animParam, standUpParamValue);
            SafeAnimator.SetBool(playerAnimator, cinematicController.playerViewAnimationName, sitParamValue);
            SafeAnimator.SetBool(playerAnimator, standUpCinematicController.playerViewAnimationName, standUpParamValue);
        }
    }
}
