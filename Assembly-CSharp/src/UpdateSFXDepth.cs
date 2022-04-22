using UnityEngine;

namespace AssemblyCSharp
{
    public class UpdateSFXDepth : MonoBehaviour
    {
        [AssertNotNull]
        public FMOD_CustomEmitter sfx;

        private int fmodIndexDepth = -1;

        private void Start()
        {
            sfx.Play();
            InvokeRepeating("UpdateDepth", 0f, 1f);
            InvokeRepeating("SelfDestroy", 5f, 5f);
        }

        private void UpdateDepth()
        {
            if (fmodIndexDepth < 0)
            {
                fmodIndexDepth = sfx.GetParameterIndex("depth");
            }
            sfx.SetParameterValue(fmodIndexDepth, Player.main.transform.position.y);
        }

        private void SelfDestroy()
        {
            if (!sfx.playing)
            {
                Object.Destroy(base.gameObject);
            }
        }
    }
}
