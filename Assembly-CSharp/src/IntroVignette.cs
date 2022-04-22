using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    public class IntroVignette : MonoBehaviour
    {
        public static IntroVignette main;

        public static bool isIntroActive;

        public bool disableInEditor;

        private IEnumerator Start()
        {
            main = this;
            while (!LightmappedPrefabs.main || LightmappedPrefabs.main.IsWaitingOnLoads() || uGUI.main.loading.IsLoading || PAXTerrainController.main.isWorking)
            {
                yield return null;
            }
            Debug.Log("LightmappedPrefabs all loaded, maybe starting intro vignette frame " + Time.frameCount);
            if (ShouldPlayIntro())
            {
                uGUI.main.intro.Play();
                yield break;
            }
            while (!LargeWorldStreamer.main || !LargeWorldStreamer.main.IsWorldSettled())
            {
                yield return new WaitForSecondsRealtime(1f);
            }
            bool fade = !XRSettings.enabled;
            uGUI.main.loading.End(fade);
            MainMenuMusic.Stop();
            VRLoadingOverlay.Hide();
        }

        private void OnDestroy()
        {
            isIntroActive = false;
        }

        public bool ShouldPlayIntro()
        {
            if (GameModeUtils.SpawnsInitialItems())
            {
                return false;
            }
            if (disableInEditor && Application.isEditor)
            {
                return false;
            }
            if (SNUtils.IsSmokeTesting())
            {
                return false;
            }
            if (Utils.GetContinueMode())
            {
                return false;
            }
            return true;
        }
    }
}
