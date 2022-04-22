using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UWE;

namespace AssemblyCSharp
{
    public class SceneCleaner : MonoBehaviour
    {
        public string loadScene;

        public static void Open()
        {
            SceneManager.LoadScene("Cleaner");
        }

        private IEnumerator Start()
        {
            if (!PlatformUtils.main.IsUserLoggedIn())
            {
                SaveLoadManager.main.Deinitialize();
            }
            GameObject[] array = Object.FindObjectsOfType<GameObject>();
            foreach (GameObject gameObject in array)
            {
                if (!(gameObject.transform.parent != null) && !(gameObject == base.gameObject) && !(gameObject.GetComponent<SceneCleanerPreserve>() != null) && !gameObject.GetComponent<SystemsSpawner>() && !gameObject.GetComponent<RuntimeManager>())
                {
                    Object.Destroy(gameObject);
                }
            }
            Base.Deinitialize();
            StreamTiming.Deinitialize();
            VoxelandData.OctNode.ClearPool();
            EcoRegionManager.Deinitialize();
            uGUI.Deinitialize();
            PDAData.Deinitialize();
            TimeCapsuleContentProvider.Deinitialize();
            PDASounds.Deinitialize();
            AssetBundleManager.Deinitialize();
            PingManager.Deinitialize();
            ItemDragManager.Deinitialize();
            GameInfoIcon.Deinitialize();
            if (DeferredSpawner.instance != null)
            {
                DeferredSpawner.instance.Reset();
            }
            GameObjectPool.ClearPools();
            yield return null;
            Resources.UnloadUnusedAssets();
            if (PlatformUtils.isConsolePlatform)
            {
                Time.timeScale = 1f;
            }
            yield return SceneManager.LoadSceneAsync(loadScene, LoadSceneMode.Single);
        }
    }
}
