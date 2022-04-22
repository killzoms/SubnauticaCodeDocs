using UnityEngine;

namespace AssemblyCSharp
{
    public class ConstructionObstacle : MonoBehaviour, IObstacle
    {
        [AssertLocalization(AssertLocalizationAttribute.Options.AllowEmptyString)]
        public string reason;

        public bool CanDeconstruct(out string r)
        {
            r = (string.IsNullOrEmpty(reason) ? null : Language.main.Get(reason));
            return false;
        }
    }
}
