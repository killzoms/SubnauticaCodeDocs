using UnityEngine;

namespace AssemblyCSharp.Story
{
    [CreateAssetMenu(fileName = "CompoundGoalData.asset", menuName = "Subnautica/Create CompoundGoalData Asset")]
    public class CompoundGoalData : ScriptableObject
    {
        public CompoundGoal[] goals;
    }
}
