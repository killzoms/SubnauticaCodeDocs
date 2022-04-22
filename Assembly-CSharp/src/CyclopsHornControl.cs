using UnityEngine;

namespace AssemblyCSharp
{
    public class CyclopsHornControl : MonoBehaviour, IHandTarget
    {
        [AssertNotNull]
        public FMOD_CustomEmitter hornSound;

        [AssertNotNull]
        public BoxCollider hornCollider;

        private bool hornCD;

        private void Start()
        {
            hornCollider.enabled = false;
        }

        private void ResetCD()
        {
            hornCD = false;
        }

        public void StopPiloting()
        {
            hornCollider.enabled = false;
        }

        public void StartPiloting()
        {
            hornCollider.enabled = true;
        }

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractText("CyclopsHorn");
            HandReticle.main.SetIcon(HandReticle.IconType.Interact);
        }

        public void OnHandClick(GUIHand hand)
        {
            if (!hornCD)
            {
                Utils.PlayEnvSound(hornSound, hornSound.gameObject.transform.position);
                hornCD = true;
                Invoke("ResetCD", 1f);
            }
        }
    }
}
