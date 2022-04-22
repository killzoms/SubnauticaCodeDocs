using UnityEngine;

namespace AssemblyCSharp
{
    public class GenericPlaySound : MonoBehaviour
    {
        [AssertNotNull]
        public FMODAsset sound;

        public void PlaySound()
        {
            Utils.PlayFMODAsset(sound, base.transform);
        }
    }
}
