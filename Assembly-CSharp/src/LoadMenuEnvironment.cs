using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssemblyCSharp
{
    public class LoadMenuEnvironment : MonoBehaviour
    {
        public string sceneName;

        private void Awake()
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }
}
