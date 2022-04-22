using UnityEngine;

namespace AssemblyCSharp
{
    public class PilotingChair : CinematicModeTriggerBase
    {
        [AssertNotNull]
        public Animator animator;

        public SubRoot subRoot;

        [AssertNotNull]
        public Transform sittingPosition;

        [AssertNotNull]
        public PlayerCinematicController releaseCinematicController;

        public Transform leftHandPlug;

        public Transform rightHandPlug;

        private Player currentPlayer;

        public void Start()
        {
            onCinematicEnd.AddListener(OnSteeringStart);
        }

        public override void OnHandHover(GUIHand hand)
        {
            if (base.enabled)
            {
                base.isValidHandTarget = IsValidHandTarget(hand);
                if (base.isValidHandTarget)
                {
                    HandReticle.main.SetInteractText("PilotSub");
                    HandReticle.main.SetIcon(HandReticle.IconType.Hand);
                }
            }
        }

        public override void OnHandClick(GUIHand hand)
        {
            if (base.enabled)
            {
                base.isValidHandTarget = IsValidHandTarget(hand);
                base.OnHandClick(hand);
            }
        }

        private bool IsValidHandTarget(GUIHand hand)
        {
            if (hand.IsFreeToInteract() && (bool)hand.player && hand.player.GetCurrentSub() == subRoot)
            {
                return hand.player.GetMode() == Player.Mode.Normal;
            }
            return false;
        }

        public void ReleaseBy(Player player)
        {
            Subscribe(player, state: false);
            if (currentPlayer == player)
            {
                releaseCinematicController.StartCinematicMode(player);
                currentPlayer = null;
            }
            Player.main.armsController.SetWorldIKTarget(null, null);
            global::UWE.Utils.GetEntityRoot(base.gameObject).BroadcastMessage("StopPiloting");
        }

        private void OnSteeringStart(CinematicModeEventData eventData)
        {
            if (base.enabled && eventData.cinematicController == cinematicController && eventData.player.GetCurrentSub() == subRoot)
            {
                currentPlayer = eventData.player;
                currentPlayer.EnterPilotingMode(this);
                Subscribe(currentPlayer, state: true);
                if ((bool)leftHandPlug && (bool)rightHandPlug)
                {
                    Player.main.armsController.SetWorldIKTarget(leftHandPlug, rightHandPlug);
                }
                global::UWE.Utils.GetEntityRoot(base.gameObject).BroadcastMessage("StartPiloting");
            }
        }

        private void Update()
        {
            if (currentPlayer != null && AvatarInputHandler.main.IsEnabled())
            {
                string buttonFormat = LanguageCache.GetButtonFormat("PressToExit", GameInput.Button.Exit);
                HandReticle.main.SetUseTextRaw(buttonFormat, null);
            }
        }

        private void Subscribe(Player player, bool state)
        {
            if (!(player == null))
            {
                if (state)
                {
                    player.playerDeathEvent.AddHandler(base.gameObject, OnPlayerDeath);
                }
                else
                {
                    player.playerDeathEvent.RemoveHandler(base.gameObject, OnPlayerDeath);
                }
            }
        }

        private void OnPlayerDeath(Player player)
        {
            if (!(currentPlayer != player))
            {
                animator.Rebind();
                player.cinematicModeActive = false;
                currentPlayer = null;
            }
        }

        private void CyclopsDeathEvent()
        {
            base.enabled = false;
        }
    }
}
