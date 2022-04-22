using UnityEngine;

namespace AssemblyCSharp.Story
{
    [CreateAssetMenu(fileName = "OnGoalUnlockData.asset", menuName = "Subnautica/Create OnGoalUnlockData Asset")]
    public class OnGoalUnlockData : ScriptableObject
    {
        public OnGoalUnlock[] onGoalUnlocks;
    }
}
