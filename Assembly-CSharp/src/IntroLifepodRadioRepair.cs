using UnityEngine;

namespace AssemblyCSharp
{
    public class IntroLifepodRadioRepair : MonoBehaviour
    {
        public FMOD_CustomEmitter radioPowerUp;

        public void PlayClip()
        {
            if (!(radioPowerUp == null))
            {
                radioPowerUp.Play();
            }
        }
    }
}
