using UnityEngine;

namespace AssemblyCSharp
{
    public class PowerCable : HandTarget, IHandTarget, IRopeProperties
    {
        public GameObject physics;

        private PowerGenerator _generator;

        private PowerRelay _relay;

        private Player player;

        private Vector3 startPosition;

        private Vector3 endPosition;

        private float length;

        public void StartDragging(Player p)
        {
            player = p;
            physics.SetActive(value: false);
        }

        public void StopDragging()
        {
            endPosition = player.GetComponent<Inventory>().toolSocket.transform.position;
            physics.transform.position = endPosition;
            physics.SetActive(value: true);
            player = null;
        }

        private void TryAttach()
        {
            global::UWE.Utils.TraceForFPSTarget(player.gameObject, 4f, 0.15f, out var closestObj, out var _);
            if (closestObj != null)
            {
                PowerPlug component = closestObj.GetComponent<PowerPlug>();
                if (component != null && !component.occupied)
                {
                    component.occupied = true;
                    player = null;
                    endPosition = component.transform.position;
                }
            }
        }

        private void Update()
        {
            if (player != null && Player.main == player)
            {
                HandReticle.main.SetInteractText("PowerCableInstructions");
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
                if (GameInput.GetButtonHeld(GameInput.Button.LeftHand))
                {
                    TryAttach();
                }
                if (GameInput.GetButtonHeld(GameInput.Button.RightHand))
                {
                    StopDragging();
                }
            }
        }

        public void OnHandHover(GUIHand hand)
        {
            if (player == null)
            {
                HandReticle.main.SetInteractText("PickupCable");
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            if (player == null)
            {
                StartDragging(hand.player);
            }
        }

        private void Start()
        {
            startPosition = base.transform.position;
            endPosition = startPosition;
            length = 0f;
        }

        public Vector3 GetStartPosition()
        {
            return startPosition;
        }

        public Vector3 GetEndPosition()
        {
            if (player != null)
            {
                endPosition = player.GetComponent<Inventory>().toolSocket.transform.position;
            }
            return endPosition;
        }

        public float GetLength()
        {
            return (endPosition - startPosition).magnitude;
        }
    }
}
