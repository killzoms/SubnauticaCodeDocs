using UnityEngine;

namespace AssemblyCSharp
{
    public class uGUI_SceneRespawning : uGUI_Scene
    {
        [AssertNotNull]
        public uGUI_TextFade loadingText;

        [AssertNotNull]
        public uGUI_Fader loadingBackground;

        [ContextMenu("Show")]
        public void Show()
        {
            loadingText.SetText(Language.main.Get("Loading"));
            loadingBackground.SetState(enabled: true);
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            loadingBackground.FadeOut();
        }
    }
}
