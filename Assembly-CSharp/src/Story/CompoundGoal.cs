using System;
using System.Collections.Generic;

namespace AssemblyCSharp.Story
{
    [Serializable]
    public class CompoundGoal : StoryGoal
    {
        public string[] preconditions;

        public bool Trigger(HashSet<string> completedGoals)
        {
            for (int i = 0; i < preconditions.Length; i++)
            {
                string item = preconditions[i];
                if (!completedGoals.Contains(item))
                {
                    return false;
                }
            }
            Trigger();
            return true;
        }
    }
}
