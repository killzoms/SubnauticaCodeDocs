using UnityEngine;

namespace AssemblyCSharp
{
    public class LoadLavaOnClick : MonoBehaviour
    {
        public GameMode gameMode;

        private void Update()
        {
            if (!PlatformUtils.isConsolePlatform)
            {
                Cursor.visible = true;
            }
        }

        private void OnMouseDown()
        {
            Utils.SetLegacyGameMode(gameMode);
            Application.LoadLevel("LavaTest");
        }
    }
}
