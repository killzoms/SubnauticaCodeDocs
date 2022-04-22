using UnityEngine;

namespace AssemblyCSharp
{
    public class RocketAtmoTrigger : MonoBehaviour
    {
        public FMOD_CustomEmitter atmoSFX;

        private void OnTriggerEnter(Collider other)
        {
            if ((bool)other.gameObject.GetComponent<Player>() && (bool)atmoSFX && !atmoSFX.playing)
            {
                Player.main.precursorOutOfWater = true;
                atmoSFX.Play();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if ((bool)other.gameObject.GetComponent<Player>() && (bool)atmoSFX)
            {
                Player.main.precursorOutOfWater = false;
                atmoSFX.Stop();
            }
        }
    }
}
