using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class RocketBuilderTooltip : MonoBehaviour, ITooltip
    {
        private TechType rocketTechType = TechType.RocketBaseLadder;

        public void SetTooltipTech(int stage)
        {
            stage--;
            switch (stage)
            {
                case 1:
                    rocketTechType = TechType.RocketStage1;
                    break;
                case 2:
                    rocketTechType = TechType.RocketStage2;
                    break;
                case 3:
                    rocketTechType = TechType.RocketStage3;
                    break;
            }
        }

        public void GetTooltip(out string tooltipText, List<TooltipIcon> tooltipIcons)
        {
            HandReticle.main.SetIcon(HandReticle.IconType.Interact);
            bool locked = !CrafterLogic.IsCraftRecipeUnlocked(rocketTechType);
            TooltipFactory.BuildTech(rocketTechType, locked, out tooltipText, tooltipIcons);
        }
    }
}
