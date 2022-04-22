using System;
using UnityEngine;

namespace AssemblyCSharp.Story
{
    [Serializable]
    public class UnlockSignalData
    {
        public Vector3 targetPosition;

        public string targetDescription;

        public void Trigger(OnGoalUnlockTracker tracker)
        {
            SignalPing component = global::UnityEngine.Object.Instantiate(tracker.signalPrefab).GetComponent<SignalPing>();
            component.pos = targetPosition;
            component.descriptionKey = targetDescription;
            component.PlayVO();
        }

        public override string ToString()
        {
            return $"{targetDescription} {targetPosition}";
        }
    }
}
