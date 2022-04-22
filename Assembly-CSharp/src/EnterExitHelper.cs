using UnityEngine;

namespace AssemblyCSharp
{
    public class EnterExitHelper : MonoBehaviour
    {
        public bool isForEscapePod;

        public bool isForWaterPark;

        public void CinematicEnter(CinematicModeEventData eventData)
        {
            Enter(base.gameObject, eventData.player, isForEscapePod);
        }

        public void CinematicExit(CinematicModeEventData eventData)
        {
            Exit(base.transform, eventData.player, isForEscapePod, isForWaterPark);
        }

        public static void Enter(GameObject gameObject, Player player, bool isForEscapePod)
        {
            if (!(player == null))
            {
                if (isForEscapePod)
                {
                    player.escapePod.Update(newValue: true);
                    player.currentEscapePod = Utils.FindAncestorWithComponent<EscapePod>(gameObject);
                }
                SubRoot subRoot = Utils.FindAncestorWithComponent<SubRoot>(gameObject);
                if ((bool)subRoot)
                {
                    player.SetCurrentSub(subRoot);
                }
                player.currentWaterPark = null;
            }
        }

        public static void Exit(Transform transform, Player player, bool isForEscapePod, bool isForWaterPark)
        {
            if (!(player == null))
            {
                if (isForEscapePod)
                {
                    player.escapePod.Update(newValue: false);
                    player.currentEscapePod = null;
                }
                if (isForWaterPark)
                {
                    WaterParkPiece componentInChildren = transform.parent.GetComponentInChildren<WaterParkPiece>();
                    player.currentWaterPark = componentInChildren.GetWaterParkModule();
                }
                else
                {
                    player.SetCurrentSub(null);
                }
            }
        }
    }
}
